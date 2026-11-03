using System.Collections.Generic;
using UnityEngine;
using Archaeo.Core;

namespace Archaeo.Sound
{
    /// Qué tipo de panel hay abierto (para elegir la música).
    public enum MusicPanel { None, Museum, Shop, Other }

    /// Decide qué música y qué ambiente suenan según lo que pasa en el juego.
    /// Los demás scripts solo le cuentan el estado (capa del mapa, panel abierto); él elige.
    /// Si la pista que tocaría no tiene audio todavía, baja a la siguiente de la lista de prioridad.
    /// Colócalo en un objeto de la escena Boot (junto al AudioManager).
    public class MusicDirector : MonoBehaviour
    {
        public static MusicDirector Instance { get; private set; }

        [Header("Música (por orden de prioridad: la primera que tenga audio)")]
        [SerializeField] string restorationTrack = AudioIds.Music_Restoration;
        [SerializeField] string museumTrack = AudioIds.Music_Museum;
        [SerializeField] string shopTrack = AudioIds.Music_Shop;
        [Tooltip("Pista de cada capa del mapa (el índice es la capa). Si no tiene audio, suena la base.")]
        [SerializeField] string[] layerTracks = { AudioIds.Music_Excavation, AudioIds.Music_Excavation_Layer2 };
        [SerializeField] string baseTrack = AudioIds.Music_Excavation;

        [Header("Ambiente (bucles)")]
        [SerializeField] string zoneAmbience = AudioIds.Ambient_Zone1;
        [SerializeField] string campfireAmbience = AudioIds.Ambient_Campfire;
        [Tooltip("Desde qué capa del mapa suena la hoguera.")]
        [SerializeField] int campfireFromLayer = 1;

        // El estado es estático: no depende de en qué orden arranquen los objetos
        static int mapLayer;
        static MusicPanel openPanel = MusicPanel.None;
        static bool restoring;

        AudioHandle zoneHandle, campfireHandle;
        float nextAmbientCheck;

        public static void SetMapLayer(int layer)
        {
            mapLayer = layer;
            Refresh();
        }

        public static void SetPanel(MusicPanel panel)
        {
            openPanel = panel;
            Refresh();
        }

        static void Refresh()
        {
            if (Instance != null) Instance.ChooseAndPlay();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (Instance != this) return;

            var game = GameManager.Instance;
            if (game != null) game.StateMachine.OnStateChanged += OnStateChanged;

            ChooseAndPlay();
        }

        void OnDestroy()
        {
            var game = GameManager.Instance;
            if (game != null) game.StateMachine.OnStateChanged -= OnStateChanged;
        }

        void OnStateChanged(GameState previous, GameState next)
        {
            restoring = next == GameState.Restoration;
            ChooseAndPlay();
        }

        // ───────────── Música ─────────────
        void ChooseAndPlay()
        {
            string track = ChooseTrack();
            if (track != null) GameAudio.Music(track);
        }

        string ChooseTrack()
        {
            var candidates = new List<string>(5);

            if (restoring) candidates.Add(restorationTrack);
            if (openPanel == MusicPanel.Museum) candidates.Add(museumTrack);
            else if (openPanel == MusicPanel.Shop) candidates.Add(shopTrack);

            if (layerTracks != null && layerTracks.Length > 0)
                candidates.Add(layerTracks[Mathf.Clamp(mapLayer, 0, layerTracks.Length - 1)]);
            candidates.Add(baseTrack);

            foreach (string id in candidates)
                if (!string.IsNullOrEmpty(id) && GameAudio.HasMusic(id)) return id;

            return null;   // ninguna tiene audio todavía: no se toca lo que esté sonando
        }

        // ───────────── Ambiente ─────────────
        void Update()
        {
            if (Time.unscaledTime < nextAmbientCheck) return;
            nextAmbientCheck = Time.unscaledTime + 0.5f;

            EnsureLoop(ref zoneHandle, zoneAmbience, true);
            EnsureLoop(ref campfireHandle, campfireAmbience, mapLayer >= campfireFromLayer);
        }

        /// Mantiene un bucle sonando mientras toque, y lo para cuando no. Se reintenta cada medio segundo,
        /// así que arranca solo en cuanto el evento tiene sonido o la ventana recupera el foco.
        static void EnsureLoop(ref AudioHandle handle, string id, bool shouldPlay)
        {
            if (string.IsNullOrEmpty(id)) return;

            bool playing = handle.IsValid && GameAudio.IsPlaying(handle);

            if (shouldPlay && !playing)
            {
                handle = GameAudio.Play(id);
            }
            else if (!shouldPlay && playing)
            {
                GameAudio.Stop(handle);
                handle = AudioHandle.None;
            }
        }
    }
}