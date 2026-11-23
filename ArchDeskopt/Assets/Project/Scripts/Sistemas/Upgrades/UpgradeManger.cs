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
    /// Además de las asignadas en "Upgrades", carga automáticamente todas las que estén en
    /// una carpeta Resources/Upgrades (las crea el menú Archaeo → Economía → Generar catálogo).
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] UpgradeData[] upgrades;

        // Solo se guardan los niveles que difieren del inicial
        readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        List<UpgradeData> everything;   // todas (para cargar partidas)
        List<UpgradeData> visible;      // las que se enseñan en la tienda

        /// Mejoras que se enseñan en la tienda. Con el diseño nuevo se ocultan las antiguas.
        public IReadOnlyList<UpgradeData> All
        {
            get
            {
                EnsureLists();
                bool newDesign = EconomyManager.Instance != null && EconomyManager.Instance.Config != null;
                if (visible == null || visible.Count == 0 || visibleForNew != newDesign) BuildVisible(newDesign);
                return visible;
            }
        }

        bool visibleForNew;

        public IEnumerable<KeyValuePair<string, int>> Levels => levels;

        /// Cambian los niveles (compra o carga de partida).
        public event Action Changed;
        /// Se compra una mejora del mapa (nivel de mapa nuevo). Solo durante el juego, no al cargar.
        public event Action<int> MapPurchased;

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

        // ───────────── Listas ─────────────
        void EnsureLists()
        {
            if (everything != null) return;

            everything = new List<UpgradeData>();
            var ids = new HashSet<string>();

            foreach (var u in upgrades)
                if (u != null && ids.Add(u.id)) everything.Add(u);

            foreach (var u in Resources.LoadAll<UpgradeData>("Upgrades"))
                if (u != null && ids.Add(u.id)) everything.Add(u);
        }

        void BuildVisible(bool newDesign)
        {
            visibleForNew = newDesign;
            visible = new List<UpgradeData>();

            foreach (var u in everything)
            {
                bool legacy = u.type <= UpgradeType.Museum;
                if (newDesign && legacy) continue;
                if (!newDesign && !legacy) continue;
                visible.Add(u);
            }

            // Mejoras primero, mapa después; dentro de cada grupo por 'order'
            visible.Sort((a, b) =>
            {
                int ga = a.type == UpgradeType.Map ? 1 : 0, gb = b.type == UpgradeType.Map ? 1 : 0;
                return ga != gb ? ga.CompareTo(gb) : a.order.CompareTo(b.order);
            });
        }

        // ───────────── Consulta ─────────────
        public int GetLevel(UpgradeData upgrade) =>
            levels.TryGetValue(upgrade.id, out int level) ? level : upgrade.startLevel;

        public bool IsMaxed(UpgradeData upgrade) => upgrade.IsMaxed(GetLevel(upgrade));

        public int NextCost(UpgradeData upgrade) => upgrade.CostToUpgrade(GetLevel(upgrade));

        /// ¿Se ha comprado ya la mejora previa que exige esta?
        public bool IsUnlocked(UpgradeData upgrade) =>
            upgrade.requires == null || GetLevel(upgrade.requires) > upgrade.requires.startLevel;

        public bool CanAfford(UpgradeData upgrade)
        {
            var economy = EconomyManager.Instance;
            return economy != null && !IsMaxed(upgrade) && IsUnlocked(upgrade) && economy.Coins >= NextCost(upgrade);
        }

        /// Nivel de la mejora de este tipo (0 si no está comprada). Para las de varios niveles, el nivel actual.
        public int LevelOf(UpgradeType type)
        {
            EnsureLists();
            int best = 0;
            foreach (var u in everything)
                if (u.type == type) best = Mathf.Max(best, GetLevel(u));
            return best;
        }

        /// Mejoras de mapa compradas = capa del mapa que se ve (0 = la primera).
        public int MapLevel
        {
            get
            {
                EnsureLists();
                int count = 0;
                foreach (var u in everything)
                    if (u.type == UpgradeType.Map && GetLevel(u) > u.startLevel) count++;
                return count;
            }
        }

        // ───────────── Compra ─────────────
        public bool TryBuy(UpgradeData upgrade)
        {
            int level = GetLevel(upgrade);
            if (upgrade.IsMaxed(level) || !IsUnlocked(upgrade)) return false;

            var economy = EconomyManager.Instance;
            if (economy == null || !economy.TrySpend(upgrade.CostToUpgrade(level))) return false;

            levels[upgrade.id] = level + 1;
            Debug.Log($"[Mejoras] {upgrade.displayName}: nivel {level + 1}");

            ApplyEffects();
            Changed?.Invoke();
            if (upgrade.type == UpgradeType.Map) MapPurchased?.Invoke(MapLevel);
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
            EnsureLists();
            foreach (var u in everything)
                if (u.id == id) return u;
            return null;
        }

        // ───────────── Efectos ─────────────
        // Los que afectan a QTE, economía y suerte los consultan directamente con LevelOf(...).
        void ApplyEffects()
        {
            EnsureLists();
            var activity = ActivityManager.Instance;
            if (activity == null) return;

            // Antiguo: Potencia de excavación multiplica los PA por evento
            float digPower = 1f;
            foreach (var u in everything)
            {
                if (u.type != UpgradeType.DigPower) continue;
                digPower = Mathf.Max(digPower, u.ValueAt(GetLevel(u)));
            }

            var cfg = EconomyManager.Instance != null ? EconomyManager.Instance.Config : null;
            float pickPerLevel = cfg != null ? cfg.pickBonusPerLevel : 0.2f;

            // Pico reforzado: +20 % de progreso por interacción y nivel
            activity.PointsMultiplier = digPower * (1f + pickPerLevel * LevelOf(UpgradeType.Pickaxe));
            // Guantes de trabajo / Teclado del escriba: cada clic / tecla cuenta por 2
            activity.MouseMultiplier = LevelOf(UpgradeType.Gloves) > 0 ? 2f : 1f;
            activity.KeyboardMultiplier = LevelOf(UpgradeType.Keyboard) > 0 ? 2f : 1f;
        }
    }
}
