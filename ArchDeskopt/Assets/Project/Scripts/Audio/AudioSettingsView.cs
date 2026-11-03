using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.Sound
{
    /// Deslizadores de volumen del panel de Ajustes: Maestro, Música y Efectos
    /// (los efectos incluyen también los sonidos de la interfaz). El ambiente es opcional.
    /// Los valores se guardan entre sesiones. Colócalo en el panel de Ajustes (OptionsPanel).
    /// Cualquier deslizador que se deje vacío se ignora.
    public class AudioSettingsView : MonoBehaviour
    {
        [SerializeField] Slider masterSlider;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;
        [SerializeField] Slider ambienceSlider;     // opcional

        [Tooltip("Al mover el de Efectos suena un clic de muestra para oír el nivel.")]
        [SerializeField] bool previewSfx = true;

        float lastPreview;

        void Awake()
        {
            Setup(masterSlider, v => { var m = AudioManager.Instance; if (m != null) m.SetMasterVolume(v); });
            Setup(musicSlider, v => { var m = AudioManager.Instance; if (m != null) m.SetBusVolume(AudioBus.Music, v); });
            Setup(ambienceSlider, v => { var m = AudioManager.Instance; if (m != null) m.SetBusVolume(AudioBus.Ambience, v); });
            Setup(sfxSlider, v =>
            {
                var m = AudioManager.Instance;
                if (m == null) return;

                m.SetBusVolume(AudioBus.Sfx, v);
                m.SetBusVolume(AudioBus.Ui, v);

                if (previewSfx && Time.unscaledTime - lastPreview > 0.15f)
                {
                    lastPreview = Time.unscaledTime;
                    GameAudio.Play(AudioIds.UI_Click);
                }
            });
        }

        // Al abrirse el panel, los deslizadores muestran los valores actuales (sin disparar sonidos)
        void OnEnable()
        {
            var m = AudioManager.Instance;
            if (m == null) return;

            Show(masterSlider, m.MasterVolume);
            Show(musicSlider, m.GetBusVolume(AudioBus.Music));
            Show(sfxSlider, m.GetBusVolume(AudioBus.Sfx));
            Show(ambienceSlider, m.GetBusVolume(AudioBus.Ambience));
        }

        static void Setup(Slider slider, UnityEngine.Events.UnityAction<float> onChanged)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.onValueChanged.AddListener(onChanged);
        }

        static void Show(Slider slider, float value)
        {
            if (slider != null) slider.SetValueWithoutNotify(value);
        }
    }
}