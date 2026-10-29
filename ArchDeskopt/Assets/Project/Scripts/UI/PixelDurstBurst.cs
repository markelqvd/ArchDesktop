using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Nube de polvo en pixel art que cubre una zona y se disipa, para esconder un cambio (p. ej. de mapa).
    /// Está hecha de manchas que crecen por etapas y se desvanecen en escalones, con un velo ligero.
    /// Todo va encajado en la rejilla de píxeles del arte y se mueve "a saltos". No necesita imágenes.
    ///
    /// Colócalo en un RectTransform que cubra la zona (estirado sobre el mapa), por encima del mapa y por debajo del HUD.
    /// Uso:  dust.Play(() => /* aquí se hace el cambio, cuando la nube está más densa */);
    public class PixelDustBurst : MonoBehaviour
    {
        class Puff
        {
            public RectTransform Rect;
            public Image Image;
            public float Delay, Life, X, Y;
            public int Variant;
            public Color Tone;
            public int Stage = -1;
            public bool Visible;
        }

        // Tamaños de las manchas, en píxeles del arte, de pequeña a grande
        static readonly Vector2Int[] BlobSizes =
        {
            new Vector2Int(4, 3), new Vector2Int(8, 6), new Vector2Int(12, 9), new Vector2Int(16, 12)
        };
        const int Variants = 2;   // formas distintas por tamaño

        [Header("Duración")]
        [Tooltip("Instante (segundos desde el inicio) en el que la nube está más densa: ahí se hace el cambio.")]
        [SerializeField] float peakSeconds = 0.55f;
        [Tooltip("Cuántas veces por segundo se actualiza. Bajo = más aspecto de animación a pocos fotogramas.")]
        [SerializeField, Range(6f, 30f)] float stepsPerSecond = 14f;

        [Header("Nube")]
        [SerializeField] int puffCount = 150;
        [Tooltip("Píxeles del arte que suben por segundo mientras se disipa.")]
        [SerializeField] float riseSpeed = 8f;

        [Header("Colores")]
        [SerializeField] Color[] tones =
        {
            new Color(0.97f, 0.91f, 0.78f),
            new Color(0.90f, 0.80f, 0.62f),
            new Color(0.82f, 0.70f, 0.50f)
        };
        [SerializeField] Color hazeColor = new Color(0.93f, 0.85f, 0.68f);

        public bool IsPlaying { get; private set; }

        Sprite[] sprites;
        Texture2D[] textures;
        Image haze;

        void Awake()
        {
            BuildSprites();
            EnsureHaze();
        }

        void OnDestroy()
        {
            if (sprites != null) foreach (var s in sprites) if (s != null) Destroy(s);
            if (textures != null) foreach (var t in textures) if (t != null) Destroy(t);
        }

        /// Lanza la nube. onPeak se llama cuando está más densa; onDone, cuando ha desaparecido del todo.
        public void Play(System.Action onPeak, System.Action onDone = null)
        {
            if (IsPlaying)
            {
                // Ya hay una nube en marcha: no se apila, se hace el cambio directamente
                onPeak?.Invoke();
                onDone?.Invoke();
                return;
            }

            StartCoroutine(Run(onPeak, onDone));
        }

        IEnumerator Run(System.Action onPeak, System.Action onDone)
        {
            IsPlaying = true;

            Rect area = ((RectTransform)transform).rect;
            float unit = PixelArt.UnitsPerPixel;
            List<Puff> puffs = SpawnPuffs(area);

            float start = Time.unscaledTime;
            bool peaked = false;

            while (true)
            {
                float t = Time.unscaledTime - start;   // reloj real: funciona aunque la ventana esté en segundo plano

                bool anyLeft = false;
                foreach (var puff in puffs)
                    if (UpdatePuff(puff, t, unit)) anyLeft = true;

                haze.color = HazeColor(t);

                if (!peaked && t >= peakSeconds)
                {
                    peaked = true;
                    onPeak?.Invoke();
                }

                if (peaked && !anyLeft) break;
                yield return new WaitForSecondsRealtime(1f / stepsPerSecond);
            }

            foreach (var puff in puffs)
                if (puff.Rect != null) Destroy(puff.Rect.gameObject);

            haze.color = new Color(hazeColor.r, hazeColor.g, hazeColor.b, 0f);
            IsPlaying = false;
            onDone?.Invoke();
        }

        // ───────────── Manchas de polvo ─────────────
        List<Puff> SpawnPuffs(Rect area)
        {
            var puffs = new List<Puff>(puffCount);

            for (int i = 0; i < puffCount; i++)
            {
                var go = new GameObject("Dust", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = Vector2.zero;   // coordenadas desde la esquina inferior izquierda
                rect.pivot = new Vector2(0.5f, 0.5f);

                var image = go.GetComponent<Image>();
                image.raycastTarget = false;   // el polvo nunca bloquea clics
                go.SetActive(false);

                puffs.Add(new Puff
                {
                    Rect = rect,
                    Image = image,
                    X = Random.Range(0f, area.width),
                    Y = Random.Range(0f, area.height),
                    Delay = Random.Range(0f, peakSeconds * 0.6f),
                    Life = Random.Range(0.8f, 1.1f),
                    Variant = Random.Range(0, Variants),
                    Tone = tones[Random.Range(0, tones.Length)]
                });
            }

            return puffs;
        }

        /// Devuelve true mientras la mancha no haya terminado.
        bool UpdatePuff(Puff puff, float t, float unit)
        {
            float age = t - puff.Delay;

            if (age < 0f) return true;   // aún no ha empezado

            if (age >= puff.Life)
            {
                if (puff.Visible) { puff.Rect.gameObject.SetActive(false); puff.Visible = false; }
                return false;
            }

            if (!puff.Visible) { puff.Rect.gameObject.SetActive(true); puff.Visible = true; }

            float f = age / puff.Life;
            int stage = f < 0.14f ? 0 : f < 0.28f ? 1 : f < 0.42f ? 2 : 3;

            if (stage != puff.Stage)
            {
                puff.Stage = stage;
                Sprite sprite = sprites[stage * Variants + puff.Variant];
                puff.Image.sprite = sprite;
                puff.Rect.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height) * unit;
            }

            // Opacidad en escalones: aparece casi opaca y se disipa en tres pasos
            float alpha = f < 0.6f ? 0.9f : f < 0.72f ? 0.7f : f < 0.84f ? 0.45f : 0.2f;
            Color color = puff.Tone;
            color.a = alpha;
            puff.Image.color = color;

            // Al disiparse sube un poco
            float rise = f > 0.6f ? (f - 0.6f) * puff.Life * riseSpeed : 0f;
            puff.Rect.anchoredPosition = new Vector2(Snap(puff.X, unit), Snap(puff.Y + rise * unit, unit));
            return true;
        }

        static float Snap(float units, float pixel) => Mathf.Round(units / pixel) * pixel;

        // Velo que cubre toda la zona, en escalones: hace que el cambio no se note
        Color HazeColor(float t)
        {
            float alpha = t < 0.15f ? 0f
                        : t < 0.30f ? 0.35f
                        : t < peakSeconds + 0.25f ? 0.7f
                        : t < peakSeconds + 0.50f ? 0.4f
                        : t < peakSeconds + 0.75f ? 0.15f
                        : 0f;

            return new Color(hazeColor.r, hazeColor.g, hazeColor.b, alpha);
        }

        void EnsureHaze()
        {
            var go = new GameObject("Haze", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();   // el velo queda detrás de las manchas

            haze = go.GetComponent<Image>();
            haze.raycastTarget = false;
            haze.color = new Color(hazeColor.r, hazeColor.g, hazeColor.b, 0f);
        }

        // ───────────── Sprites de las manchas, generados por código ─────────────
        void BuildSprites()
        {
            sprites = new Sprite[BlobSizes.Length * Variants];
            textures = new Texture2D[sprites.Length];

            for (int size = 0; size < BlobSizes.Length; size++)
            {
                for (int variant = 0; variant < Variants; variant++)
                {
                    int index = size * Variants + variant;
                    int w = BlobSizes[size].x, h = BlobSizes[size].y;
                    var rng = new System.Random(size * 31 + variant * 7 + 5);

                    var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Point,       // píxeles nítidos
                        wrapMode = TextureWrapMode.Clamp
                    };

                    var pixels = new Color32[w * h];
                    float cx = (w - 1) / 2f, cy = (h - 1) / 2f;

                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            // Elipse con el borde irregular, para que cada mancha tenga su forma
                            float dx = (x - cx) / (w / 2f);
                            float dy = (y - cy) / (h / 2f);
                            float limit = 1f + (float)(rng.NextDouble() * 0.5 - 0.25);
                            bool solid = dx * dx + dy * dy <= limit;

                            pixels[y * w + x] = solid ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                        }
                    }

                    texture.SetPixels32(pixels);
                    texture.Apply(false);

                    textures[index] = texture;
                    sprites[index] = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
                }
            }
        }
    }
}