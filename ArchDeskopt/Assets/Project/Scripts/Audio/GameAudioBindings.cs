using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Activity;
using Archaeo.Data;
using Archaeo.Discovery;
using Archaeo.Excavation;
using Archaeo.Save;
using Archaeo.UI;

namespace Archaeo.Sound
{
    /// Conecta los sucesos del juego con sus eventos de sonido. Es el ÚNICO sitio donde se decide
    /// "cuando pasa X, suena Y" para los eventos generales; el resto de scripts no saben nada de audio
    /// salvo los pocos sonidos que dependen de un momento muy concreto de su animación.
    /// Colócalo en un objeto de la escena Boot (junto al AudioManager).
    public class GameAudioBindings : MonoBehaviour
    {
        [Header("Botones y casillas")]
        [Tooltip("Añade solo el sonido de clic (y de ratón encima) a todos los botones y casillas de la interfaz.")]
        [SerializeField] bool autoButtonSounds = true;
        [SerializeField] bool hoverSounds = true;
        [Tooltip("Cada cuánto se buscan botones nuevos, en segundos.")]
        [SerializeField] float scanInterval = 0.5f;

        ActivityManager activity;
        ExcavationProgress excavation;
        DiscoveryManager discovery;
        SaveManager save;

        readonly HashSet<int> wired = new HashSet<int>();
        float nextScan;

        void Awake() => DontDestroyOnLoad(gameObject);

        void Start()
        {
            activity = ActivityManager.Instance;
            if (activity != null) activity.ActivityRegistered += OnActivity;

            var system = ExcavationSystem.Instance;
            if (system != null)
            {
                excavation = system.Progress;
                excavation.FindMade += OnFindMade;
            }

            discovery = DiscoveryManager.Instance;
            if (discovery != null)
            {
                discovery.ArtifactFound += OnArtifactFound;
                discovery.RestorationCompleted += OnRestored;
            }

            save = SaveManager.Instance;
            if (save != null) save.OfflineIncomeEarned += OnOfflineIncome;

            GameAudio.Play(AudioIds.Window_Start);
        }

        void OnDestroy()
        {
            if (activity != null) activity.ActivityRegistered -= OnActivity;
            if (excavation != null) excavation.FindMade -= OnFindMade;
            if (discovery != null)
            {
                discovery.ArtifactFound -= OnArtifactFound;
                discovery.RestorationCompleted -= OnRestored;
            }
            if (save != null) save.OfflineIncomeEarned -= OnOfflineIncome;
        }

        // ───────────── Sucesos del juego ─────────────
        void OnActivity(ActivityEvent e) =>
            GameAudio.Play(e.Kind == ActivityKind.Mouse ? AudioIds.Activity_Click : AudioIds.Activity_Key);

        void OnFindMade(int number, bool early) =>
            GameAudio.Play(early ? AudioIds.Excavation_EarlyFind : AudioIds.Excavation_Find);

        void OnArtifactFound(ArtifactData artifact, bool isNew, int level)
        {
            if (!isNew)
            {
                GameAudio.Play(AudioIds.Artifact_Duplicate);
                return;
            }

            // Sonido propio de su rareza; si no tiene, el general de "objeto nuevo"
            string specific = artifact.rarity != null ? "Artifact_New_" + artifact.rarity.name.Replace("Rarity_", "") : null;
            GameAudio.Play(specific != null && GameAudio.Has(specific) ? specific : AudioIds.Artifact_New);
        }

        void OnRestored(ArtifactData artifact) => GameAudio.Play(AudioIds.Artifact_Restored);

        void OnOfflineIncome(double coins, double seconds) => GameAudio.Play(AudioIds.Economy_OfflineIncome);

        // ───────────── Sonido automático en la interfaz ─────────────
        void Update()
        {
            if (!autoButtonSounds || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + scanInterval;

            // Solo los activos: así, cuando un panel se abre por primera vez, ya han corrido sus Awake
            // (que pueden marcar botones con UISoundIgnore) antes de que se les ponga el sonido.
            foreach (var selectable in FindObjectsByType<Selectable>(FindObjectsSortMode.None))
            {
                int id = selectable.GetInstanceID();
                if (wired.Contains(id)) continue;

                bool isButton = selectable is Button;
                bool isToggle = selectable is Toggle;
                if (!isButton && !isToggle) continue;   // deslizadores, barras de scroll...

                wired.Add(id);

                // Los que tienen su propio sonido, o los que arrastran la ventana, no llevan el genérico
                if (selectable.GetComponent<UISoundIgnore>() != null) continue;
                if (selectable.GetComponent<TitleBarView>() != null) continue;
                if (selectable.name == "ExaminePoint") continue;

                if (selectable is Button button) button.onClick.AddListener(PlayClick);
                else ((Toggle)selectable).onValueChanged.AddListener(PlayToggle);

                if (hoverSounds && selectable.GetComponent<UIHoverSound>() == null)
                    selectable.gameObject.AddComponent<UIHoverSound>();
            }
        }

        static void PlayClick() => GameAudio.Play(AudioIds.UI_Click);
        static void PlayToggle(bool value) => GameAudio.Play(AudioIds.UI_Toggle);
    }
}