using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Archaeo.Sound
{
    /// Marca: este botón no lleva el sonido de clic automático (porque tiene el suyo propio).
    public class UISoundIgnore : MonoBehaviour
    {
        public static void Mark(GameObject target)
        {
            if (target.GetComponent<UISoundIgnore>() == null)
                target.AddComponent<UISoundIgnore>();
        }
    }

    /// Sonido al pasar el ratón por encima de un botón. Lo añade solo GameAudioBindings.
    public class UIHoverSound : MonoBehaviour, IPointerEnterHandler
    {
        Selectable selectable;

        void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable == null || selectable.IsInteractable())
                GameAudio.Play(AudioIds.UI_Hover);
        }
    }
}