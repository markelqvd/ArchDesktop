using UnityEngine;
using Archaeo.Activity;
using Archaeo.Data;

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

        [Header("Hallazgo anticipado")]
        [Tooltip("Probabilidad por cada PA. 0,0003 = 0,03 %")]
        [Range(0f, 0.05f)]
        [SerializeField] float earlyFindChancePerPoint = 0.0003f;

        public ExcavationProgress Progress { get; } = new ExcavationProgress();

        ActivityCounter counter;

        float CurrentThreshold
        {
            get
            {
                if (thresholdOverride > 0) return thresholdOverride;
                return currentZone != null ? currentZone.activityPointsPerFind : 1000;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

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
        void Update() => ApplySettings();

        void ApplySettings()
        {
            Progress.Threshold = CurrentThreshold;
            Progress.EarlyFindChancePerPoint = earlyFindChancePerPoint;
        }

        void OnPointsAdded(float points) => Progress.AddPoints(points);
    }
}