using TMPro;
using UnityEngine;
using Archaeo.Activity;

namespace Archaeo.UI
{
    /// TEMPORAL: muestra los contadores de actividad para probar. Borrar después.
    public class ActivityDebugText : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

        void Update()
        {
            var manager = ActivityManager.Instance;
            if (manager == null) return;

            var c = manager.Counter;
            label.text = $"Ratón: {c.MouseEvents}   Teclado: {c.KeyboardEvents}\n" +
                         $"Puntos: {c.TotalPoints:0}   Descartados: {c.RejectedEvents}";
        }
    }
}