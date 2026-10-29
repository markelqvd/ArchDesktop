using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Archaeo.UI
{
    /// Herramienta de revisión (no forma parte del juego): comprueba que todos los dibujos pixel art de la interfaz
    /// se dibujan con el tamaño de píxel común (PixelArt.UnitsPerPixel), y puede corregir los que no.
    /// Colócalo en cualquier objeto de la escena, asigna "Root" y usa el menú de los tres puntos del componente:
    ///   - "Comprobar tamaño de píxel": solo informa.
    ///   - "Aplicar tamaño de píxel": corrige los de tamaño fijo (con el juego PARADO).
    public class PixelSizeChecker : MonoBehaviour
    {
        [Tooltip("Objeto cuyos hijos se revisan (UI_MainWindow). Si está vacío, este mismo objeto.")]
        [SerializeField] RectTransform root;

        [Tooltip("Margen de error aceptado, en unidades por píxel del arte.")]
        [SerializeField] float tolerance = 0.02f;

        [ContextMenu("Comprobar tamaño de píxel")]
        void Check() => Run(false);

        [ContextMenu("Aplicar tamaño de píxel")]
        void Apply()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[Píxel] Aplica los cambios con el juego PARADO; en Play se perderían al salir.", this);
                return;
            }
            Run(true);
        }

        void Run(bool apply)
        {
            var target = root != null ? root : (RectTransform)transform;
            float units = PixelArt.UnitsPerPixel;

            var report = new StringBuilder();
            report.AppendLine($"[Píxel] Tamaño de píxel común: {units:0.####} unidades por píxel del arte.");

            int ok = 0, fixedCount = 0, manual = 0;
            foreach (var image in target.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || image.type != Image.Type.Simple) continue;

                Rect sprite = image.sprite.rect;
                float current = UnitsPerPixel(image);

                if (Mathf.Abs(current - units) <= tolerance) { ok++; continue; }

                var size = new Vector2(sprite.width * units, sprite.height * units);
                var rt = image.rectTransform;
                bool fixedSize = rt.anchorMin == rt.anchorMax;   // tamaño fijo, no estirado

                if (apply && fixedSize)
                {
#if UNITY_EDITOR
                    Undo.RecordObject(rt, "Aplicar tamaño de píxel");
#endif
                    rt.sizeDelta = size;
                    fixedCount++;
                    report.AppendLine($"  ✔ '{image.name}': ahora {size.x:0.#} x {size.y:0.#} (antes {current:0.##} unidades por píxel).");
                }
                else
                {
                    manual++;
                    string how = fixedSize ? "usa 'Aplicar tamaño de píxel'" : "tiene las anclas estiradas: ajusta sus márgenes a mano";
                    report.AppendLine($"  ✗ '{image.name}': sprite {sprite.width}x{sprite.height} px, ahora {current:0.##} unidades por píxel " +
                                      $"→ debería medir {size.x:0.#} x {size.y:0.#} ({how}).");
                }
            }

            report.AppendLine($"Correctos: {ok}   Corregidos: {fixedCount}   Pendientes: {manual}");
            report.AppendLine("Los efectos por código (hoguera, humo, pico y brocha) ya usan este tamaño solos.");
            Debug.Log(report.ToString(), this);
        }

        /// Unidades de la UI con las que se dibuja cada píxel del sprite.
        static float UnitsPerPixel(Image image)
        {
            Rect size = image.rectTransform.rect;
            Rect sprite = image.sprite.rect;
            float x = size.width / sprite.width;
            float y = size.height / sprite.height;
            return image.preserveAspect ? Mathf.Min(x, y) : (x + y) * 0.5f;
        }
    }
}