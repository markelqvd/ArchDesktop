using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Barra de título propia: arrastrar la ventana, minimizar y cerrar.
    /// El objeto debe tener una Image con Raycast Target activado.
    public class TitleBarView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] Button minimizeButton;
        [SerializeField] Button closeButton;

        void Awake()
        {
            minimizeButton.onClick.AddListener(() => WindowManager.Instance?.Minimize());
            closeButton.onClick.AddListener(() => WindowManager.Instance?.Quit());
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                WindowManager.Instance?.BeginDrag();
        }
    }
}