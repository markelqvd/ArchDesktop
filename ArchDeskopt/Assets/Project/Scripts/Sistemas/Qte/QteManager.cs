using System;
using UnityEngine;
using Archaeo.Data;
using Archaeo.Discovery;
using Archaeo.Economy;
using Archaeo.Excavation;
using Archaeo.Upgrades;

namespace Archaeo.Qte
{
    public enum QteState { Idle, Waiting, Playing, Result }

    /// Cerebro del QTE: barra llena -> aviso (espera 20 s) -> juego -> resultado -> recompensa.
    ///   Perfecto = objeto + bono (2 min de ingresos).   Bien = objeto.
    ///   Fallo = polvo de excavación (10 % del bono) y la barra queda al 25 %.
    ///   Si no lo atiendes a tiempo: sin penalización, la barra sigue llena y el aviso vuelve tras el tiempo mínimo.
    /// Se crea solo (lo pide QtePanelView). Solo actúa si el EconomyManager tiene un EconomyConfig.
    public class QteManager : MonoBehaviour
    {
        public static QteManager Instance { get; private set; }

        /// ¿El QTE se encarga de los hallazgos? (si no, funciona el flujo antiguo)
        public static bool Handles => Instance != null && Instance.UsesQte;

        public static QteManager EnsureExists()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("QteManager");
            DontDestroyOnLoad(go);
            return go.AddComponent<QteManager>();
        }

        // ---- Habilidades (se leen de las mejoras compradas) ----
        static int Lvl(UpgradeType t) => UpgradeManager.Instance != null ? UpgradeManager.Instance.LevelOf(t) : 0;

        public bool FocusMode => Lvl(UpgradeType.Focus) > 0;               // Modo concentración: espera 60 s
        public float PrecisionBonus => 0.10f * Lvl(UpgradeType.Pulse);     // Pulso firme: zonas +10 % por nivel
        public float ExtraSeconds => 0.5f * Lvl(UpgradeType.Hourglass);    // Reloj de arena: +0,5 s por nivel
        public float EasyChance => 0.10f * Lvl(UpgradeType.Instinct);      // Instinto: 10 % de versión fácil por nivel
        public bool ShowRarity => Lvl(UpgradeType.Detector) > 0;           // Detector de metales
        bool ExpertEye => Lvl(UpgradeType.Expert) > 0;                     // Ojo experto: Perfecto = copia extra
        bool ChainSkill => Lvl(UpgradeType.Chain) > 0;                     // Cadena de aciertos
        float CloverChance => 0.05f * Lvl(UpgradeType.Clover);             // Trébol fósil: 2 objetos

        int perfectChain;
        float sessionTimer;
        bool sessionChecked;

        public QteState State { get; private set; } = QteState.Idle;
        public ArtifactData Artifact { get; private set; }
        public float WaitLeft { get; private set; }
        public float WaitTotal { get; private set; }

        // Último resultado (para que el panel lo muestre)
        public QteResult LastResult { get; private set; }
        public GrantResult LastGrant { get; private set; }
        public double LastCoins { get; private set; }
        public float ResultLeft { get; private set; }

        public event Action StateChanged;
        public event Action<QteResult, ArtifactData> Resolved;

        const float ResultSeconds = 2f;

        ExcavationProgress progress;
        DiscoveryManager discovery;
        EconomyManager economy;
        readonly System.Random rng = new System.Random();

        public bool UsesQte => economy != null && economy.Config != null && discovery != null && progress != null;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (Instance != this) return;
            Connect();
        }

        void OnDestroy()
        {
            if (progress != null) progress.FindMade -= OnFindMade;
            if (Instance == this) Instance = null;
        }

        void Connect()
        {
            discovery = DiscoveryManager.Instance;
            economy = EconomyManager.Instance;
            var system = ExcavationSystem.Instance;
            if (discovery == null || economy == null || system == null)
            {
                Debug.LogWarning("[QTE] Falta DiscoveryManager, EconomyManager o ExcavationSystem (¿has pasado por Boot?).");
                return;
            }

            progress = system.Progress;
            progress.FindMade += OnFindMade;
        }

        void Update()
        {
            // Mapa del tesoro: el primer QTE de la sesión sale nada más abrir el juego
            if (!sessionChecked && UsesQte)
            {
                sessionTimer += Time.unscaledDeltaTime;
                if (sessionTimer > 3f)
                {
                    sessionChecked = true;
                    if (Lvl(UpgradeType.TreasureMap) > 0) DebugTrigger();
                }
            }

            if (State == QteState.Waiting)
            {
                WaitLeft -= Time.unscaledDeltaTime;
                if (WaitLeft <= 0f) Expire();
            }
            else if (State == QteState.Result)
            {
                ResultLeft -= Time.unscaledDeltaTime;
                if (ResultLeft <= 0f) SetState(QteState.Idle);
            }
        }

        // ───────── Flujo ─────────
        void OnFindMade(int findNumber, bool early)
        {
            if (!UsesQte) return;
            if (State != QteState.Idle) return;

            Artifact = discovery.PrepareFind();
            if (Artifact == null)
            {
                Debug.LogWarning("[QTE] No hay objetos disponibles en la zona.");
                return;
            }

            progress.HeldByQte = true;
            WaitTotal = FocusMode ? economy.Config.qteWaitSecondsFocus : economy.Config.qteWaitSeconds;
            WaitLeft = WaitTotal;
            SetState(QteState.Waiting);
            Debug.Log($"[QTE] Aviso: {Artifact.displayName} ({Artifact.economyRarity})");
        }

        /// El jugador ha hecho clic en el aviso.
        public void BeginPlaying()
        {
            if (State != QteState.Waiting) return;
            SetState(QteState.Playing);
        }

        /// Crea el contexto de dificultad para el QTE actual.
        public QteContext BuildContext()
        {
            int rarity = Artifact != null ? (int)Artifact.economyRarity : 0;
            return new QteContext
            {
                RarityIndex = rarity,
                ZoneIndex = Artifact != null ? Artifact.zoneIndex : 0,
                Difficulty01 = rarity / 4f,
                Easy = EasyChance > 0f && rng.NextDouble() < EasyChance,
                WindowScale = 1f + PrecisionBonus,
                ExtraSeconds = ExtraSeconds,
                Rng = rng,
            };
        }

        public QteGame CreateGame() => QteCatalog.Create(Artifact != null ? Artifact.zoneIndex : 0, rng);

        /// Cierra el QTE con un resultado y reparte la recompensa.
        public void Resolve(QteResult result)
        {
            if (State != QteState.Playing || Artifact == null) return;

            var cfg = economy.Config;
            double perfectBonus = economy.IncomePerSecond * 60.0 * cfg.perfectBonusMinutes;
            if (ChainSkill) perfectBonus *= 1.0 + 0.10 * Mathf.Min(perfectChain, 10);   // cada Perfecto seguido +10 % (máx. 10)

            LastResult = result;
            LastCoins = 0;
            LastGrant = default;

            if (result == QteResult.Fail)
            {
                LastCoins = perfectBonus * cfg.failDustFraction;          // polvo de excavación
                economy.AddCoins(LastCoins);
                progress.SetState(progress.Threshold * cfg.barAfterFail, progress.TotalFinds);
            }
            else
            {
                if (result == QteResult.Perfect)
                {
                    LastCoins = perfectBonus;
                    economy.AddCoins(LastCoins);
                    if (ExpertEye) discovery.GrantArtifact(Artifact, true);   // Ojo experto: 1 copia extra
                }
                LastGrant = discovery.GrantArtifact(Artifact, true);

                // Trébol fósil: a veces sale un segundo objeto
                if (CloverChance > 0f && rng.NextDouble() < CloverChance)
                {
                    var bonus = discovery.PrepareFind();
                    if (bonus != null) discovery.GrantArtifact(bonus, true);
                }
            }

            perfectChain = result == QteResult.Perfect ? perfectChain + 1 : 0;

            Debug.Log($"[QTE] {result}: {Artifact.displayName} -> {LastGrant.Outcome}, monedas +{LastCoins:0.##}");

            progress.StartCooldown();
            progress.HeldByQte = false;

            ResultLeft = ResultSeconds;
            Resolved?.Invoke(result, Artifact);
            SetState(QteState.Result);
        }

        // Nadie lo atendió: sin penalización
        void Expire()
        {
            Debug.Log("[QTE] Expirado sin atender (sin penalización).");
            Artifact = null;
            progress.SetState(progress.Threshold, progress.TotalFinds);   // la barra sigue llena
            progress.StartCooldown();
            progress.HeldByQte = false;
            SetState(QteState.Idle);
        }

        /// Pruebas: lanza un QTE ahora mismo, sin llenar la barra.
        public void DebugTrigger()
        {
            if (!UsesQte || State != QteState.Idle) return;
            OnFindMade(progress.TotalFinds, false);
        }

        void SetState(QteState s)
        {
            State = s;
            StateChanged?.Invoke();
        }
    }
}
