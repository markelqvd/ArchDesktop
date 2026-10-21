using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Opciones de ventana en el panel de Ajustes: "Siempre visible" y "Anclar a los bordes".
    public class WindowOptionsView : MonoBehaviour
    {
        [SerializeField] Toggle alwaysOnTopToggle;
        [SerializeField] Toggle snapToggle;          // opcional

        void Start()
        {
            var wm = WindowManager.Instance;
            if (wm == null) return; // p.ej. al probar Main directamente en el Editor

            // Muestra el valor guardado sin disparar el evento
            alwaysOnTopToggle.SetIsOnWithoutNotify(wm.AlwaysOnTop);
            alwaysOnTopToggle.onValueChanged.AddListener(wm.SetAlwaysOnTop);

            if (snapToggle != null)
            {
                snapToggle.SetIsOnWithoutNotify(wm.SnapToEdges);
                snapToggle.onValueChanged.AddListener(wm.SetSnapToEdges);
            }
        }
    }
}