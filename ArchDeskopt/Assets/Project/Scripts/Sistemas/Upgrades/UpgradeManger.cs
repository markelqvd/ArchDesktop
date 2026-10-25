using System;
using System.Collections.Generic;
using UnityEngine;
using Archaeo.Activity;
using Archaeo.Data;
using Archaeo.Economy;
using Archaeo.Save;

namespace Archaeo.Upgrades
{
    /// Niveles de las mejoras, compra con monedas y aplicación de sus efectos.
    /// Colócalo en un objeto de la escena Boot y asigna en "Upgrades" las mejoras disponibles.
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] UpgradeData[] upgrades;

        // Solo se guardan los niveles que difieren del inicial
        readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public IReadOnlyList<UpgradeData> All => upgrades;
        public IEnumerable<KeyValuePair<string, int>> Levels => levels;

        /// Cambian los niveles (compra o carga de partida).
        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (upgrades == null) upgrades = new UpgradeData[0];
        }

        void Start()
        {
            if (Instance != this) return;
            ApplyEffects();
        }

        // ───────────── Consulta ─────────────
        public int GetLevel(UpgradeData upgrade) =>
            levels.TryGetValue(upgrade.id, out int level) ? level : upgrade.startLevel;

        public bool IsMaxed(UpgradeData upgrade) => upgrade.IsMaxed(GetLevel(upgrade));

        public int NextCost(UpgradeData upgrade) => upgrade.CostToUpgrade(GetLevel(upgrade));

        public bool CanAfford(UpgradeData upgrade)
        {
            var economy = EconomyManager.Instance;
            return economy != null && !IsMaxed(upgrade) && economy.Coins >= NextCost(upgrade);
        }

        // ───────────── Compra ─────────────
        public bool TryBuy(UpgradeData upgrade)
        {
            int level = GetLevel(upgrade);
            if (upgrade.IsMaxed(level)) return false;

            var economy = EconomyManager.Instance;
            if (economy == null || !economy.TrySpend(upgrade.CostToUpgrade(level))) return false;

            levels[upgrade.id] = level + 1;
            Debug.Log($"[Mejoras] {upgrade.displayName}: nivel {level + 1}");

            ApplyEffects();
            Changed?.Invoke();
            SaveManager.Instance?.RequestSave();
            return true;
        }

        // ───────────── Guardado ─────────────
        public void ClearLevels() => levels.Clear();

        public void SetLevel(string id, int level)
        {
            UpgradeData upgrade = Find(id);
            if (upgrade == null)
            {
                Debug.LogWarning($"[Mejoras] Mejora desconocida en el guardado: '{id}'. Se ignora.");
                return;
            }

            levels[id] = Mathf.Clamp(level, upgrade.startLevel, upgrade.maxLevel);
        }

        /// Se llama tras cargar los niveles de una partida.
        public void ApplyLoaded()
        {
            ApplyEffects();
            Changed?.Invoke();
        }

        UpgradeData Find(string id)
        {
            foreach (var u in upgrades)
                if (u != null && u.id == id) return u;
            return null;
        }

        // ───────────── Efectos ─────────────
        void ApplyEffects()
        {
            var activity = ActivityManager.Instance;
            if (activity == null) return;

            // Potencia de excavación: multiplica los PA que da cada clic o pulsación
            float digPower = 1f;
            foreach (var u in upgrades)
            {
                if (u == null || u.type != UpgradeType.DigPower) continue;
                digPower = Mathf.Max(digPower, u.ValueAt(GetLevel(u)));
            }

            activity.PointsMultiplier = digPower;
        }
    }
}