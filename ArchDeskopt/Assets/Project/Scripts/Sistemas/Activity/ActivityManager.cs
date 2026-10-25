using System;
using System.Collections.Concurrent;
using UnityEngine;
using Archaeo.Platform;

namespace Archaeo.Activity
{
    /// Punto central de la actividad del usuario.
    /// Recibe eventos del lector global, los filtra según los ajustes, aplica el
    /// límite por segundo y los cuenta. Colócalo en un objeto de la escena Boot.
    public class ActivityManager : MonoBehaviour
    {
        public static ActivityManager Instance { get; private set; }

        [Header("Detección (se conectarán al panel de Ajustes)")]
        [SerializeField] bool globalDetectionEnabled = true;
        [SerializeField] bool countMouse = true;
        [SerializeField] bool countKeyboard = true;

        [Header("Balance")]
        [SerializeField] int maxEventsPerSecond = 8;
        [SerializeField] float pointsPerEvent = 1f;

        public ActivityCounter Counter { get; } = new ActivityCounter();
        public ActivityRateLimiter RateLimiter { get; } = new ActivityRateLimiter();

        public bool Paused { get; set; }

        /// Multiplicador de las mejoras (Potencia de excavación). Puntos por evento = base × multiplicador.
        public float PointsMultiplier { get; set; } = 1f;
        public bool GlobalDetectionEnabled { get => globalDetectionEnabled; set => globalDetectionEnabled = value; }
        public bool CountMouse { get => countMouse; set => countMouse = value; }
        public bool CountKeyboard { get => countKeyboard; set => countKeyboard = value; }
        public int MaxEventsPerSecond { get => maxEventsPerSecond; set => maxEventsPerSecond = Mathf.Max(0, value); }

        /// Se dispara por cada evento de actividad aceptado.
        public event Action<ActivityEvent> ActivityRegistered;

        readonly ConcurrentQueue<ActivityEvent> queue = new ConcurrentQueue<ActivityEvent>();
        GlobalInputPoller poller;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDisable() => StopPoller();
        void OnApplicationQuit() => StopPoller();

        void Update()
        {
            if (Instance != this) return;

            // Se aplican cada frame para que los cambios del Inspector surtan efecto en Play
            RateLimiter.MaxPerSecond = maxEventsPerSecond;
            Counter.PointsPerEvent = pointsPerEvent * PointsMultiplier;
            RefreshPoller();

            while (queue.TryDequeue(out ActivityEvent e))
            {
                if (Paused || !globalDetectionEnabled) continue;
                if (e.Kind == ActivityKind.Mouse && !countMouse) continue;
                if (e.Kind == ActivityKind.Keyboard && !countKeyboard) continue;

                if (!RateLimiter.TryAccept(e.Timestamp))
                {
                    Counter.RegisterRejected();
                    continue;
                }

                Counter.Register(e);
                ActivityRegistered?.Invoke(e);
            }
        }

        void RefreshPoller()
        {
            bool shouldRun = globalDetectionEnabled;
            if (shouldRun && poller == null)
            {
                poller = new GlobalInputPoller(queue);
                poller.Start();
            }
            else if (!shouldRun && poller != null)
            {
                StopPoller();
            }
        }

        void StopPoller()
        {
            poller?.Dispose();
            poller = null;
            while (queue.TryDequeue(out _)) { }
        }
    }
}