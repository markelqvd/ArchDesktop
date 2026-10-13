using UnityEngine;
using UnityEngine.EventSystems;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Barra superior: al hacer clic sobre ella (fuera de los botones) se arrastra la ventana.
    /// El objeto debe tener una Image con Raycast Target activado.
    public class TitleBarView : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                WindowManager.Instance?.BeginDrag();
        }
    }
}