using System.Collections.Generic;
using UnityEngine;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Da forma redondeada a la ventana: recorta la ventana de Windows siguiendo
    /// la ventana principal y, si está abierta, la zona de paneles (con un pequeño hueco entre ambas).
    /// Colócalo en el objeto Canvas.
    public class WindowShapeView : MonoBehaviour
    {
        [Header("Qué recortar")]
        [SerializeField] RectTransform mainWindow;   // UI_MainWindow
        [SerializeField] RectTransform panelArea;    // PanelArea (solo cuenta cuando está activa)

        [Header("Aspecto (en unidades de referencia, se escalan con la ventana)")]
        [SerializeField] float cornerRadius = 14f;
        [SerializeField] float panelGap = 6f;

        readonly Vector3[] corners = new Vector3[4];
        readonly List<RectInt> rects = new List<RectInt>(2);
        readonly List<RectInt> applied = new List<RectInt>(2);
        int appliedRadius = -1;

        void LateUpdate()
        {
            var window = WindowManager.Instance;
            if (window == null || mainWindow == null) return;

            float scale = window.Scale;
            int radius = Mathf.RoundToInt(cornerRadius * scale);
            int gap = Mathf.RoundToInt(panelGap * scale);

            rects.Clear();
            AddRect(mainWindow, 0);
            if (panelArea != null && panelArea.gameObject.activeInHierarchy)
                AddRect(panelArea, gap);

            if (radius == appliedRadius && SameAsApplied()) return;

            // Solo se da por aplicada si la ventana estaba lista; si no, se reintenta en el siguiente frame
            if (window.SetShape(rects, radius))
            {
                appliedRadius = radius;
                applied.Clear();
                applied.AddRange(rects);
            }
        }

        /// Convierte un RectTransform a píxeles de ventana (origen arriba a la izquierda).
        void AddRect(RectTransform rt, int gapBottom)
        {
            rt.GetWorldCorners(corners);   // 0 = abajo-izq, 2 = arriba-der; en píxeles, con Y hacia arriba
            int h = Screen.height;

            int xMin = Mathf.RoundToInt(corners[0].x);
            int xMax = Mathf.RoundToInt(corners[2].x);
            int yMin = h - Mathf.RoundToInt(corners[2].y);
            int yMax = h - Mathf.RoundToInt(corners[0].y) - gapBottom;

            rects.Add(new RectInt(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin)));
        }

        bool SameAsApplied()
        {
            if (rects.Count != applied.Count) return false;
            for (int i = 0; i < rects.Count; i++)
                if (!rects[i].Equals(applied[i])) return false;
            return true;
        }
    }
}