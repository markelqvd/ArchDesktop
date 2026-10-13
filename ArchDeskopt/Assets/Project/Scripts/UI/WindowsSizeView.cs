using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Control de tamaño de ventana:  −  100 %  +
    public class WindowSizeView : MonoBehaviour
    {
        [SerializeField] Button smallerButton;
        [SerializeField] Button largerButton;
        [SerializeField] TMP_Text valueText;

        void Awake()
        {
            smallerButton.onClick.AddListener(() => Change(-WindowManager.ScaleStep));
            largerButton.onClick.AddListener(() => Change(+WindowManager.ScaleStep));
        }

        void OnEnable() => Refresh();

        void Change(float delta)
        {
            var wm = WindowManager.Instance;
            if (wm == null) return;
            wm.SetScale(wm.Scale + delta);
            Refresh();
        }

        void Refresh()
        {
            var wm = WindowManager.Instance;
            float s = wm != null ? wm.Scale : 1f;
            valueText.text = $"{Mathf.RoundToInt(s * 100f)} %";
            smallerButton.interactable = s > WindowManager.MinScale + 0.01f;
            largerButton.interactable = wm != null && s < wm.MaxScale - 0.01f;
        }
    }
}