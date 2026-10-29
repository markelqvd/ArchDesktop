using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Control de tamaño de ventana:  −  Tamaño ×2  +
    /// El tamaño es cuántos píxeles de pantalla mide cada píxel del arte (×1, ×2, ×3...),
    /// así todo el pixel art se ve nítido y con los píxeles del mismo tamaño.
    /// Se puede poner en el desplegable de ajustes y/o en el panel de Ajustes.
    public class WindowSizeView : MonoBehaviour
    {
        [SerializeField] Button smallerButton;
        [SerializeField] Button largerButton;
        [SerializeField] TMP_Text valueText;

        [Tooltip("Formato del texto. {0} es el tamaño de píxel (1, 2, 3...). Ej.: \"Tamaño ×{0}\"")]
        [SerializeField] string format = "Tamaño ×{0}";

        void Awake()
        {
            smallerButton.onClick.AddListener(() => Change(-1));
            largerButton.onClick.AddListener(() => Change(+1));
        }

        void OnEnable() => Refresh();

        void Change(int delta)
        {
            var wm = WindowManager.Instance;
            if (wm == null) return;
            wm.SetPixelSize(wm.PixelSize + delta);
            Refresh();
        }

        void Refresh()
        {
            var wm = WindowManager.Instance;
            int size = wm != null ? wm.PixelSize : 2;
            valueText.text = string.Format(format, size);
            smallerButton.interactable = size > 1;
            largerButton.interactable = wm != null && size < wm.MaxPixelSize;
        }
    }
}