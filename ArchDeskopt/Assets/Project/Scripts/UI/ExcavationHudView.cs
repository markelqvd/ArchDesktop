using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Excavation;

namespace Archaeo.UI
{
    /// Pinta la barra de excavación, el progreso numérico y el número de hallazgos.
    /// Colócalo en la raíz de UI_MainWindow.
    public class ExcavationHudView : MonoBehaviour
    {
        [SerializeField] Slider bar;
        [SerializeField] TMP_Text progressText;   // "312 / 1000"
        [SerializeField] TMP_Text findsText;      // "Hallazgos: 3"

        ExcavationProgress progress;

        void Start()
        {
            var system = ExcavationSystem.Instance;
            if (system == null) return;   // p.ej. al probar Main sin pasar por Boot

            progress = system.Progress;
            progress.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (progress != null) progress.Changed -= Refresh;
        }

        void Refresh()
        {
            bar.value = progress.Normalized;
            progressText.text = $"{progress.Points:0} / {progress.Threshold:0}";
            findsText.text = $"Hallazgos: {progress.TotalFinds}";
        }
    }
}