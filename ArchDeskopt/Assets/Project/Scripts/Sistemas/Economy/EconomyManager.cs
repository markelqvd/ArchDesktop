using System;
using UnityEngine;
using Archaeo.Data;
using Archaeo.Discovery;
using Archaeo.Upgrades;

namespace Archaeo.Economy
{
    /// Monedas del jugador e ingresos pasivos del museo.
    /// Cada artefacto RESTAURADO genera monedas por segundo según su nivel y su rareza.
    /// Colócalo en un objeto de la escena Boot.
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Tooltip("Multiplicador de ingresos por nivel cuando el artefacto no define sus ingresos por nivel (GDD: 10, 15, 22 ...).")]
        [SerializeField] float levelGrowth = 1.5f;

        [Header("Diseño de economía")]
        [Tooltip("Asigna aquí el asset EconomyConfig. Si está vacío se usa la economía antigua.")]
        [SerializeField] EconomyConfig config;

        /// Objetos que hay que tener en una zona para el bonus de colección completa.
        const int ItemsPerZone = 6;

        public EconomyConfig Config => config;
        /// Fórmulas del diseño (null si no hay config).
        public EconomyMath Formulas { get; private set; }

        /// Bonus global de ingresos (0.1 = +10 %): Vitrinas iluminadas (+10 % por nivel)
        /// y Folleto del museo (+1 % por cada objeto distinto expuesto).
        public double GlobalBonus
        {
            get
            {
                var up = UpgradeManager.Instance;
                if (up == null) return 0;

                double bonus = 0.10 * up.LevelOf(Archaeo.Data.UpgradeType.Showcase);
                if (up.LevelOf(Archaeo.Data.UpgradeType.Pamphlet) > 0) bonus += 0.01 * restoredCount;
                return bonus;
            }
        }

        int restoredCount;

        public double Coins { get; private set; }
        public double IncomePerSecond { get; private set; }

        /// Cambia el número entero de monedas.
        public event Action CoinsChanged;
        /// Cambian los ingresos por segundo.
        public event Action IncomeChanged;

        DiscoveryManager discovery;
        double lastTime;
        long lastShown = -1;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (config != null) Formulas = new EconomyMath(config);
        }

        void Start()
        {
            if (Instance != this) return;

            lastTime = Time.realtimeSinceStartupAsDouble;

            discovery = DiscoveryManager.Instance;
            if (discovery == null)
            {
                Debug.LogWarning("[Economía] No hay DiscoveryManager en la escena.");
                return;
            }

            discovery.Collection.Changed += RecomputeIncome;
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.Changed += RecomputeIncome;
            discovery.RestorationCompleted += OnRestored;
            RecomputeIncome();
        }

        void OnDestroy()
        {
            if (discovery == null) return;
            discovery.Collection.Changed -= RecomputeIncome;
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.Changed -= RecomputeIncome;
            discovery.RestorationCompleted -= OnRestored;
        }

        void Update()
        {
            // Reloj real: sigue siendo correcto aunque la ventana esté minimizada y los frames vayan lentos
            double now = Time.realtimeSinceStartupAsDouble;
            double elapsed = now - lastTime;
            lastTime = now;

            if (IncomePerSecond <= 0 || elapsed <= 0) return;

            Coins += IncomePerSecond * elapsed;
            NotifyCoins();
        }

        void OnRestored(ArtifactData artifact)
        {
            if (Formulas != null) return;   // con el diseño nuevo no hay recompensa fija por restaurar
            int reward = artifact.rarity != null ? artifact.rarity.initialReward : 0;
            if (reward <= 0) return;

            Coins += reward;
            NotifyCoins();
            Debug.Log($"[Economía] Recompensa por restaurar {artifact.displayName}: +{reward} monedas");
        }

        void RecomputeIncome()
        {
            restoredCount = 0;
            foreach (var entry in discovery.Collection.Entries)
                if (entry.Restored) restoredCount++;

            double perMinute = 0;
            foreach (var entry in discovery.Collection.Entries)
            {
                if (entry.Restored)
                    perMinute += IncomePerMinute(entry.Artifact, entry.Level);
            }

            IncomePerSecond = perMinute / 60.0;
            IncomeChanged?.Invoke();
        }

        /// Ingresos por minuto de un artefacto a un nivel dado, incluida la rareza.
        public float IncomePerMinute(ArtifactData artifact, int level)
        {
            level = Mathf.Max(1, level);

            // Economía del diseño: el nivel del objeto = copias acumuladas
            if (Formulas != null)
                return (float)Formulas.IncomePerMinute(artifact.zoneIndex, artifact.economyRarity, level, GlobalBonus + ZoneBonus(artifact.zoneIndex));

            float value;

            var table = artifact.levelIncomes;
            if (table != null && table.Length > 0)
            {
                // Valores por nivel definidos; más allá del último, sigue creciendo
                int index = Mathf.Min(level - 1, table.Length - 1);
                value = table[index] * Mathf.Pow(levelGrowth, Mathf.Max(0, level - table.Length));
            }
            else
            {
                value = artifact.baseIncomePerMinute * Mathf.Pow(levelGrowth, level - 1);
            }

            float rarity = artifact.rarity != null ? artifact.rarity.incomeMultiplier : 1f;
            return value * rarity;
        }

        /// +25 % a los ingresos de una sala si tienes sus 6 objetos restaurados.
        public double ZoneBonus(int zone)
        {
            if (config == null || discovery == null) return 0;
            int count = 0;
            foreach (var e in discovery.Collection.Entries)
                if (e.Restored && e.Artifact != null && e.Artifact.zoneIndex == zone) count++;
            return count >= ItemsPerZone ? config.zoneCompleteBonus : 0;
        }

        /// ¿El objeto ya está en rango S con estas copias?
        public bool IsMaxRank(ArtifactData artifact, int copies)
            => Formulas != null && Formulas.RankIndex(artifact.economyRarity, copies) >= config.ranks.Length - 1;

        /// Vende una copia sobrante (30 min de su ingreso base). Devuelve las monedas.
        public double SellExtraCopy(ArtifactData artifact)
        {
            double value = Formulas.SellValue(artifact.zoneIndex, artifact.economyRarity);
            AddCoins(value);
            return value;
        }

        /// Gasta monedas. Devuelve false si no hay suficientes.
        public bool TrySpend(double amount)
        {
            if (amount < 0 || Coins < amount) return false;
            Coins -= amount;
            NotifyCoins();
            return true;
        }

        /// Suma monedas (p. ej. ingresos con el juego cerrado).
        public void AddCoins(double amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            NotifyCoins();
        }

        /// Fija las monedas (al cargar una partida).
        public void SetCoins(double value)
        {
            Coins = Math.Max(0, value);
            lastShown = -1;   // fuerza el aviso aunque coincida el valor entero
            NotifyCoins();
        }

        /// Atajo de pruebas: menú de los tres puntos del componente, en Play.
        [ContextMenu("Añadir 1000 monedas (pruebas)")]
        void DebugAddCoins() => AddCoins(1000);

        void NotifyCoins()
        {
            long shown = (long)Math.Floor(Coins);
            if (shown == lastShown) return;
            lastShown = shown;
            CoinsChanged?.Invoke();
        }
    }
}