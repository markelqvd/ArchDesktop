using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Opciones de ventana en la UI: check de "siempre visible".
    public class WindowOptionsView : MonoBehaviour
    {
        [SerializeField] Toggle alwaysOnTopToggle;

        void Start()
        {
            var wm = WindowManager.Instance;
            if (wm == null) return; // p.ej. al probar Main directamente en el Editor

            // Muestra el valor guardado sin disparar el evento
            alwaysOnTopToggle.SetIsOnWithoutNotify(wm.AlwaysOnTop);
            alwaysOnTopToggle.onValueChanged.AddListener(wm.SetAlwaysOnTop);
        }
    }
}