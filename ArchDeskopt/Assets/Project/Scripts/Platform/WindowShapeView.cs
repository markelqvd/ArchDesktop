using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Da forma a la ventana de Windows: recorta todo lo que no sea parte de la interfaz.
    ///   - La ventana principal y los elementos flotantes toman la silueta de su sprite
    ///     (los píxeles transparentes no forman parte de la ventana: no se ve ningún color y los clics pasan).
    ///   - La zona de paneles, cuando está abierta, es un rectángulo redondeado (con un hueco debajo).
    ///
    /// Cuando la forma CRECE (se abre un panel) espera unos frames antes de aplicarla, para que el
    /// contenido ya esté dibujado en la zona nueva. Cuando se ENCOGE se aplica al instante.
    /// Colócalo en el objeto Canvas.
    ///
    /// Los avisos y confirmaciones ([Forma] ...) salen en la Console también al jugar en el Editor.
    public class WindowShapeView : MonoBehaviour
    {
        class MaskData
        {
            public Sprite Sprite;
            public int Width, Height, Key;
            public bool PreserveAspect;
            public List<RectInt> Rects;
        }

        [Header("Qué recortar")]
        [SerializeField] RectTransform mainWindow;   // UI_MainWindow

        [Tooltip("Imagen que define la forma de la ventana principal (tu mapa). " +
                 "Si se deja vacío, se busca sola: la imagen más grande con sprite dentro de UI_MainWindow.")]
        [SerializeField] Image mainShapeImage;

        [SerializeField] RectTransform panelArea;    // PanelArea (solo cuenta cuando está activa)

        [Tooltip("Elementos que sobresalen de la ventana principal (los iconos de arriba).")]
        [SerializeField] RectTransform[] floatingElements;

        [Header("Forma según la imagen")]
        [Tooltip("La forma sigue la transparencia del sprite (Image tipo Simple, con Read/Write activado en el sprite). " +
                 "Si un elemento no cumple, usa un rectángulo redondeado.")]
        [SerializeField] bool followSpriteAlpha = true;
        [Tooltip("Opacidad mínima (0-1) para que un píxel cuente como parte de la ventana.")]
        [SerializeField, Range(0.05f, 0.95f)] float alphaThreshold = 0.3f;

        [Header("Esquinas redondeadas (cuando no hay sprite)")]
        [SerializeField] float cornerRadius = 14f;
        [SerializeField] float floatingCornerRadius = 8f;
        [SerializeField] float panelGap = 6f;

        [Header("Ajuste fino")]
        [Tooltip("Frames que se espera antes de ampliar la forma, para evitar destellos al abrir un panel.")]
        [SerializeField] int growDelayFrames = 2;

        readonly Vector3[] corners = new Vector3[4];
        readonly List<WindowShapePart> parts = new List<WindowShapePart>();
        readonly List<WindowShapePart> applied = new List<WindowShapePart>();
        readonly List<WindowShapePart> pending = new List<WindowShapePart>();
        int pendingFrames;

        readonly Dictionary<RectTransform, MaskData> masks = new Dictionary<RectTransform, MaskData>();
        readonly Dictionary<Texture2D, Color32[]> pixelCache = new Dictionary<Texture2D, Color32[]>();
        readonly HashSet<string> warned = new HashSet<string>();
        int nextMaskKey = 1;

        Image autoMainImage;
        float nextSearchTime;

        void LateUpdate()
        {
            var window = WindowManager.Instance;
            if (window == null || mainWindow == null) return;

            float scale = window.Scale;
            int radius = Mathf.RoundToInt(cornerRadius * scale);
            int floatingRadius = Mathf.RoundToInt(floatingCornerRadius * scale);
            int gap = Mathf.RoundToInt(panelGap * scale);

            parts.Clear();

            // Ventana principal: la silueta es la de la imagen del mapa
            RectTransform mainShape = mainWindow;
            Image mainImage = followSpriteAlpha ? ResolveMainImage() : null;
            if (mainImage != null) mainShape = mainImage.rectTransform;
            AddPart(mainShape, radius, 0, true);

            if (panelArea != null && panelArea.gameObject.activeInHierarchy)
                AddPart(panelArea, radius, gap, false);

            if (floatingElements != null)
            {
                foreach (var element in floatingElements)
                    if (element != null && element.gameObject.activeInHierarchy)
                        AddPart(element, floatingRadius, 0, true);
            }

            if (Same(parts, applied))
            {
                pending.Clear();
                pendingFrames = 0;
                return;
            }

            // Si la forma crece, esperar a que el contenido nuevo ya se haya dibujado
            if (TotalArea(parts) > TotalArea(applied))
            {
                if (!Same(parts, pending))
                {
                    pending.Clear();
                    pending.AddRange(parts);
                    pendingFrames = 0;
                }

                pendingFrames++;
                if (pendingFrames < growDelayFrames) return;
            }

            // Solo se da por aplicada si la ventana estaba lista; si no, se reintenta en el siguiente frame
            if (window.SetShape(parts))
            {
                applied.Clear();
                applied.AddRange(parts);
                pending.Clear();
                pendingFrames = 0;
            }
        }

        // ───────────── Qué imagen define la forma de la ventana principal ─────────────
        Image ResolveMainImage()
        {
            if (mainShapeImage != null) return mainShapeImage;
            if (autoMainImage != null) return autoMainImage;

            // Búsqueda automática (como mucho cada medio segundo)
            if (Time.unscaledTime < nextSearchTime) return null;
            nextSearchTime = Time.unscaledTime + 0.5f;

            autoMainImage = FindMainImage();
            if (autoMainImage == null)
                Warn("noimage", "[Forma] No encuentro dentro de UI_MainWindow la imagen que define la forma. " +
                                "Arrastra tu mapa al campo 'Main Shape Image' del Window Shape View.");
            else
                Debug.Log($"[Forma] Imagen que define la forma de la ventana: '{autoMainImage.name}'", autoMainImage);

            return autoMainImage;
        }

        /// La imagen más grande (casi tan grande como la ventana) con sprite dentro de la ventana principal.
        /// Entre varias, la última de la jerarquía, que es la que se dibuja encima.
        Image FindMainImage()
        {
            RectInt mainRect = GetPixelRect(mainWindow, 0);
            long mainArea = (long)mainRect.width * mainRect.height;
            if (mainArea <= 0) return null;

            Image best = null;
            foreach (var image in mainWindow.GetComponentsInChildren<Image>(false))
            {
                if (!image.enabled || image.sprite == null) continue;

                RectInt r = GetPixelRect(image.rectTransform, 0);
                if ((long)r.width * r.height < mainArea * 0.8f) continue;   // botones y demás: no definen la forma

                best = image;
            }

            return best;
        }

        // ───────────── Piezas de la forma ─────────────
        /// Rectángulo de un RectTransform en píxeles de ventana (origen arriba a la izquierda).
        RectInt GetPixelRect(RectTransform rt, int gapBottom)
        {
            rt.GetWorldCorners(corners);   // 0 = abajo-izq, 2 = arriba-der; en píxeles, con Y hacia arriba
            int h = Screen.height;

            int xMin = Mathf.RoundToInt(corners[0].x);
            int xMax = Mathf.RoundToInt(corners[2].x);
            int yMin = h - Mathf.RoundToInt(corners[2].y);
            int yMax = h - Mathf.RoundToInt(corners[0].y) - gapBottom;

            return new RectInt(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
        }

        void AddPart(RectTransform rt, int radius, int gapBottom, bool allowMask)
        {
            RectInt rect = GetPixelRect(rt, gapBottom);

            if (allowMask && TryGetMask(rt, ref rect, out MaskData mask))
                parts.Add(new WindowShapePart(rect, radius, mask.Rects, mask.Key));
            else
                parts.Add(new WindowShapePart(rect, radius));
        }

        // ───────────── Silueta a partir de la transparencia del sprite ─────────────
        bool TryGetMask(RectTransform rt, ref RectInt rect, out MaskData mask)
        {
            mask = null;
            if (!followSpriteAlpha || rect.width <= 0 || rect.height <= 0) return false;

            var image = rt.GetComponent<Image>();
            if (image == null || image.sprite == null) return false;

            if (image.type != Image.Type.Simple)
            {
                Warn(rt.name + "_type", $"[Forma] '{rt.name}': el Image debe ser de tipo Simple (ahora es {image.type}) para seguir la silueta. " +
                                        "Se usa un rectángulo redondeado.");
                return false;
            }

            Sprite sprite = image.sprite;
            Texture2D texture = sprite.texture;
            if (!texture.isReadable)
            {
                Warn(texture.name, $"[Forma] La textura '{texture.name}' no tiene Read/Write activado. " +
                                   "Actívalo en su Import Settings (Advanced) y pulsa Apply. Se usa un rectángulo redondeado.");
                return false;
            }

            // Con Preserve Aspect el sprite no llena el rectángulo: se centra con sus proporciones
            if (image.preserveAspect)
            {
                float spriteAspect = sprite.rect.width / sprite.rect.height;
                float rectAspect = rect.width / (float)rect.height;
                int w = rect.width, h = rect.height;

                if (spriteAspect > rectAspect) h = Mathf.RoundToInt(rect.width / spriteAspect);
                else w = Mathf.RoundToInt(rect.height * spriteAspect);

                rect = new RectInt(rect.xMin + (rect.width - w) / 2, rect.yMin + (rect.height - h) / 2, w, h);
            }

            if (masks.TryGetValue(rt, out mask)
                && mask.Sprite == sprite && mask.Width == rect.width && mask.Height == rect.height
                && mask.PreserveAspect == image.preserveAspect)
                return true;

            List<RectInt> rects = BuildMask(sprite, rect.width, rect.height);
            if (rects.Count == 0)
            {
                Warn(rt.name + "_empty", $"[Forma] El sprite de '{rt.name}' es totalmente transparente. Se usa un rectángulo redondeado.");
                mask = null;
                return false;
            }

            mask = new MaskData
            {
                Sprite = sprite, Width = rect.width, Height = rect.height, PreserveAspect = image.preserveAspect,
                Rects = rects, Key = nextMaskKey++
            };
            masks[rt] = mask;

            Debug.Log($"[Forma] Silueta de '{rt.name}' aplicada: {rects.Count} rectángulos ({rect.width}x{rect.height} px).", rt);
            return true;
        }

        /// Recorre el sprite estirado a width x height y devuelve los tramos opacos como rectángulos
        /// (y = 0 es la fila superior). Las filas iguales seguidas se agrupan en un solo rectángulo.
        List<RectInt> BuildMask(Sprite sprite, int width, int height)
        {
            Texture2D texture = sprite.texture;
            if (!pixelCache.TryGetValue(texture, out Color32[] pixels))
            {
                pixels = texture.GetPixels32();
                pixelCache[texture] = pixels;
            }

            Rect sr = sprite.rect;
            int texWidth = texture.width;
            byte limit = (byte)Mathf.RoundToInt(alphaThreshold * 255f);

            // Columna del sprite que corresponde a cada columna de la ventana
            var column = new int[width];
            for (int x = 0; x < width; x++)
                column[x] = (int)sr.x + Mathf.Min((int)sr.width - 1, Mathf.FloorToInt((x + 0.5f) / width * sr.width));

            var result = new List<RectInt>();
            var runs = new List<int>();        // pares: inicio, fin (exclusivo)
            var previous = new List<int>();
            int bandTop = 0;
            bool hasPrevious = false;

            for (int y = 0; y < height; y++)
            {
                // La textura tiene el origen abajo: la fila superior de la ventana es la última del sprite
                float v = 1f - (y + 0.5f) / height;
                int sy = (int)sr.y + Mathf.Clamp(Mathf.FloorToInt(v * sr.height), 0, (int)sr.height - 1);
                int rowStart = sy * texWidth;

                runs.Clear();
                int start = -1;
                for (int x = 0; x < width; x++)
                {
                    bool solid = pixels[rowStart + column[x]].a >= limit;
                    if (solid && start < 0) start = x;
                    else if (!solid && start >= 0) { runs.Add(start); runs.Add(x); start = -1; }
                }
                if (start >= 0) { runs.Add(start); runs.Add(width); }

                if (hasPrevious && SameRuns(previous, runs)) continue;   // misma fila: se alarga la banda

                FlushBand(result, previous, hasPrevious, bandTop, y);
                previous.Clear();
                previous.AddRange(runs);
                hasPrevious = true;
                bandTop = y;
            }

            FlushBand(result, previous, hasPrevious, bandTop, height);
            return result;
        }

        static void FlushBand(List<RectInt> result, List<int> runs, bool hasRuns, int top, int bottom)
        {
            if (!hasRuns) return;
            for (int i = 0; i < runs.Count; i += 2)
                result.Add(new RectInt(runs[i], top, runs[i + 1] - runs[i], bottom - top));
        }

        static bool SameRuns(List<int> a, List<int> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        void Warn(string key, string message)
        {
            if (warned.Add(key)) Debug.LogWarning(message, this);
        }

        // ───────────── Comparación de formas ─────────────
        static bool Same(List<WindowShapePart> a, List<WindowShapePart> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Radius != b[i].Radius) return false;
                if (a[i].MaskKey != b[i].MaskKey) return false;
                if (!a[i].Rect.Equals(b[i].Rect)) return false;
            }
            return true;
        }

        static long TotalArea(List<WindowShapePart> list)
        {
            long total = 0;
            foreach (var p in list) total += (long)p.Rect.width * p.Rect.height;
            return total;
        }
    }
}