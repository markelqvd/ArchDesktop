using System.Collections.Generic;
using UnityEngine;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Da forma redondeada a la ventana: recorta la ventana de Windows siguiendo
    ///   - la ventana principal,
    ///   - la zona de paneles, cuando está abierta (con un pequeño hueco debajo),
    ///   - los elementos flotantes que sobresalen de la ventana principal (p. ej. los iconos de arriba).
    /// Todo lo que queda fuera de esa forma se ve cortado y deja pasar los clics.
    /// Colócalo en el objeto Canvas.
    public class WindowShapeView : MonoBehaviour
    {
        [Header("Qué recortar")]
        [SerializeField] RectTransform mainWindow;   // UI_MainWindow
        [SerializeField] RectTransform panelArea;    // PanelArea (solo cuenta cuando está activa)

        [Tooltip("Elementos que sobresalen de la ventana principal. Cada uno se recorta con su propio rectángulo redondeado.")]
        [SerializeField] RectTransform[] floatingElements;

        [Header("Aspecto (en unidades de referencia, se escalan con la ventana)")]
        [SerializeField] float cornerRadius = 14f;
        [SerializeField] float floatingCornerRadius = 8f;
        [SerializeField] float panelGap = 6f;

        readonly Vector3[] corners = new Vector3[4];
        readonly List<WindowShapePart> parts = new List<WindowShapePart>();
        readonly List<WindowShapePart> applied = new List<WindowShapePart>();

        void LateUpdate()
        {
            var window = WindowManager.Instance;
            if (window == null || mainWindow == null) return;

            float scale = window.Scale;
            int radius = Mathf.RoundToInt(cornerRadius * scale);
            int floatingRadius = Mathf.RoundToInt(floatingCornerRadius * scale);
            int gap = Mathf.RoundToInt(panelGap * scale);

            parts.Clear();
            Add(mainWindow, radius, 0);

            if (panelArea != null && panelArea.gameObject.activeInHierarchy)
                Add(panelArea, radius, gap);

            if (floatingElements != null)
            {
                foreach (var element in floatingElements)
                    if (element != null && element.gameObject.activeInHierarchy)
                        Add(element, floatingRadius, 0);
            }

            if (SameAsApplied()) return;

            // Solo se da por aplicada si la ventana estaba lista; si no, se reintenta en el siguiente frame
            if (window.SetShape(parts))
            {
                applied.Clear();
                applied.AddRange(parts);
            }
        }

        /// Convierte un RectTransform a píxeles de ventana (origen arriba a la izquierda).
        void Add(RectTransform rt, int radius, int gapBottom)
        {
            rt.GetWorldCorners(corners);   // 0 = abajo-izq, 2 = arriba-der; en píxeles, con Y hacia arriba
            int h = Screen.height;

            int xMin = Mathf.RoundToInt(corners[0].x);
            int xMax = Mathf.RoundToInt(corners[2].x);
            int yMin = h - Mathf.RoundToInt(corners[2].y);
            int yMax = h - Mathf.RoundToInt(corners[0].y) - gapBottom;

            var rect = new RectInt(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
            parts.Add(new WindowShapePart(rect, radius));
        }

        bool SameAsApplied()
        {
            if (parts.Count != applied.Count) return false;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Radius != applied[i].Radius) return false;
                if (!parts[i].Rect.Equals(applied[i].Rect)) return false;
            }
            return true;
        }
    }
}