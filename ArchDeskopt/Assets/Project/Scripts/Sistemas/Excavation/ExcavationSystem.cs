using UnityEngine;
using Archaeo.Activity;
using Archaeo.Data;
using Archaeo.Economy;
using Archaeo.Upgrades;

namespace Archaeo.Excavation
{
    /// Conecta la actividad del usuario con la barra de excavación.
    /// Colócalo en un objeto de la escena Boot (junto a ActivityManager).
    public class ExcavationSystem : MonoBehaviour
    {
        public static ExcavationSystem Instance { get; private set; }

        [Header("Zona actual")]
        [SerializeField] ZoneData currentZone;

        [Tooltip("0 = usar los PA por hallazgo de la zona. Pon un valor bajo (p.ej. 40) para probar.")]
        [SerializeField] int thresholdOverride = 0;

        [Tooltip("-1 = usar el tiempo mínimo entre QTEs del EconomyConfig (60 s). 0 = sin límite (para pruebas).")]
        [SerializeField] float minSecondsOverride = -1f;

        [Header("Hallazgo anticipado")]
        [Tooltip("Probabilidad por cada PA. 0,0003 = 0,03 %")]
        [Range(0f, 0.05f)]
        [SerializeField] float earlyFindChancePerPoint = 0.0003f;

        public ExcavationProgress Progress { get; } = new ExcavationProgress();

        ActivityCounter counter;

        static EconomyConfig Config => EconomyManager.Instance != null ? EconomyManager.Instance.Config : null;

        float CurrentThreshold
        {
            get
            {
                if (thresholdOverride > 0) return thresholdOverride;
                var cfg = Config;
                if (cfg != null) return cfg.barThreshold;
                return currentZone != null ? currentZone.activityPointsPerFind : 1000;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Progress.Clock = () => Time.realtimeSinceStartupAsDouble;
            ApplySettings();
            Progress.FindMade += (number, early) =>
                Debug.Log($"[Excavación] Hallazgo nº {number}{(early ? " (anticipado)" : "")}");
        }

        void Start()
        {
            if (Instance != this) return;

            var manager = ActivityManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[Excavación] No hay ActivityManager en la escena.");
                return;
            }

            counter = manager.Counter;
            counter.PointsAdded += OnPointsAdded;
        }

        void OnDestroy()
        {
            if (counter != null) counter.PointsAdded -= OnPointsAdded;
        }

        // Se aplica cada frame para que los cambios del Inspector surtan efecto en Play
        float autoAccumulated;

        void Update()
        {
            ApplySettings();

            // Excavadora automática: +1 interacción por segundo y nivel, aunque no toques nada
            var up = UpgradeManager.Instance;
            int auto = up != null ? up.LevelOf(UpgradeType.AutoDig) : 0;
            if (auto > 0 && Config != null && !Progress.HeldByQte)
            {
                autoAccumulated += auto * Time.deltaTime;
                if (autoAccumulated >= 1f)
                {
                    int whole = Mathf.FloorToInt(autoAccumulated);
                    autoAccumulated -= whole;
                    Progress.AddPoints(whole);
                }
            }

            Progress.Tick();
        }

        void ApplySettings()
        {
            Progress.Threshold = CurrentThreshold;
            var cfg = Config;
            // Con el diseño nuevo no hay hallazgos anticipados: el QTE sale al llenarse la barra
            Progress.EarlyFindChancePerPoint = cfg != null ? 0f : earlyFindChancePerPoint;
            Progress.MinSecondsBetweenFinds = minSecondsOverride >= 0f ? minSecondsOverride
                : (cfg != null ? cfg.minSecondsBetweenQtes : 0f);
        }

        void OnPointsAdded(float points) => Progress.AddPoints(points);
    }
}