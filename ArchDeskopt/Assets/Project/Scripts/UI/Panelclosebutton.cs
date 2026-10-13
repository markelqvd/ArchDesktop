using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.UI
{
    /// Botón "X" de un panel: lo cierra a través del PanelHostView del Canvas.
    [RequireComponent(typeof(Button))]
    public class PanelCloseButton : MonoBehaviour
    {
        void Awake()
        {
            var host = GetComponentInParent<PanelHostView>(true);
            GetComponent<Button>().onClick.AddListener(() => host?.Close());
        }
    }
}