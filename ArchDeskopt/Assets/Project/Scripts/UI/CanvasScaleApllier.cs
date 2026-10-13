using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Aplica la escala de la ventana a toda la UI. Colócalo en el Canvas.
    /// Usa Constant Pixel Size: 1 unidad = 1 píxel × escala.
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasScaleApplier : MonoBehaviour
    {
        CanvasScaler scaler;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var wm = WindowManager.Instance;
            Apply(wm != null ? wm.Scale : 1f);
        }

        void OnEnable()
        {
            if (WindowManager.Instance != null) WindowManager.Instance.ScaleChanged += Apply;
        }

        void OnDisable()
        {
            if (WindowManager.Instance != null) WindowManager.Instance.ScaleChanged -= Apply;
        }

        void Apply(float scale) => scaler.scaleFactor = scale;
    }
}