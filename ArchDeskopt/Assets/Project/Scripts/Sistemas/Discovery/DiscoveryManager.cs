using System;
using System.Collections.Generic;
using UnityEngine;
using Archaeo.Data;
using Archaeo.Excavation;
using Archaeo.Economy;
using Archaeo.Qte;

namespace Archaeo.Discovery
{
    /// Convierte cada hallazgo de la excavación en un artefacto.
    ///   - Artefacto NUEVO  -> entra en la colección y queda pendiente de restaurar.
    ///   - Artefacto REPETIDO -> sube de nivel, sin interrumpir al jugador.
    /// Colócalo en un objeto de la escena Boot.
    public class DiscoveryManager : MonoBehaviour
    {
        public static DiscoveryManager Instance { get; private set; }

        [Header("Datos")]
        [SerializeField] ArtifactDatabase database;
        [SerializeField] ZoneData currentZone;

        [Tooltip("Si se asigna, el primer hallazgo del juego siempre será este artefacto.")]
        [SerializeField] ArtifactData firstArtifact;

        [Header("Balance")]
        [Tooltip("Multiplicador de probabilidad para artefactos que aún no se han encontrado.")]
        [SerializeField] float newArtifactBoost = 4f;

        public ArtifactCollection Collection { get; } = new ArtifactCollection();
        public ArtifactSpawner Spawner { get; } = new ArtifactSpawner();

        /// Base de datos de artefactos (el guardado la usa para reconstruir la colección por id).
        public ArtifactDatabase Database => database;

        readonly Queue<ArtifactData> pendingRestorations = new Queue<ArtifactData>();

        public int PendingCount => pendingRestorations.Count;
        public ArtifactData NextPending => pendingRestorations.Count > 0 ? pendingRestorations.Peek() : null;

        /// Pendientes de restaurar, en orden.
        public IEnumerable<ArtifactData> PendingList => pendingRestorations;

        /// (artefacto, ¿es nuevo?, nivel actual)
        public event Action<ArtifactData, bool, int> ArtifactFound;
        public event Action<ArtifactData> RestorationCompleted;
        public event Action PendingChanged;

        ExcavationProgress progress;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (Instance != this) return;

            var system = ExcavationSystem.Instance;
            if (system == null)
            {
                Debug.LogWarning("[Descubrimiento] No hay ExcavationSystem en la escena.");
                return;
            }

            progress = system.Progress;
            progress.FindMade += OnFindMade;
        }

        void OnDestroy()
        {
            if (progress != null) progress.FindMade -= OnFindMade;
        }

        void OnFindMade(int findNumber, bool early)
        {
            // Con el diseño nuevo, el QTE decide cuándo y cómo se consigue el objeto
            if (QteManager.Handles) return;

            ArtifactData artifact = ChooseArtifact();
            if (artifact == null)
            {
                Debug.LogWarning("[Descubrimiento] No hay artefactos disponibles en la zona.");
                return;
            }

            GrantArtifact(artifact, false);
        }

        /// Elige el objeto que saldrá en este hallazgo (sin dárselo todavía al jugador).
        public ArtifactData PrepareFind() => ChooseArtifact();

        /// Da el objeto al jugador.
        ///   restoreNow = true  -> va directo al museo (flujo con QTE).
        ///   restoreNow = false -> queda pendiente de restaurar (flujo antiguo).
        /// Nuevo -> entra en la colección. Repetido -> sube una copia; si ya está en rango S, se vende.
        public GrantResult GrantArtifact(ArtifactData artifact, bool restoreNow)
        {
            if (Collection.Has(artifact.id))
            {
                // Copia sobrante de un objeto que ya está en rango S: se vende automáticamente
                var eco = EconomyManager.Instance;
                if (eco != null && eco.Formulas != null && Collection.TryGet(artifact.id, out var owned)
                    && eco.IsMaxRank(artifact, owned.Level))
                {
                    double coins = eco.SellExtraCopy(artifact);
                    Debug.Log($"[Descubrimiento] Copia sobrante de {artifact.displayName} (rango S): vendida por {coins:0.##} monedas");
                    ArtifactFound?.Invoke(artifact, false, owned.Level);
                    return new GrantResult(GrantOutcome.Sold, owned.Level, coins);
                }

                int level = Collection.LevelUp(artifact.id);
                Debug.Log($"[Descubrimiento] Repetido: {artifact.displayName} -> nivel {level}");
                ArtifactFound?.Invoke(artifact, false, level);
                return new GrantResult(GrantOutcome.Copy, level, 0);
            }

            Collection.AddNew(artifact);
            if (restoreNow) Collection.MarkRestored(artifact.id);
            else pendingRestorations.Enqueue(artifact);

            Debug.Log($"[Descubrimiento] ¡NUEVO!: {artifact.displayName}. Pendientes: {PendingCount}");
            ArtifactFound?.Invoke(artifact, true, 1);

            if (restoreNow) RestorationCompleted?.Invoke(artifact);
            else PendingChanged?.Invoke();

            return new GrantResult(GrantOutcome.New, 1, 0);
        }

        ArtifactData ChooseArtifact()
        {
            // Primer hallazgo del juego: siempre el mismo (tutorial)
            if (firstArtifact != null && Collection.Count == 0)
                return firstArtifact;

            IReadOnlyList<ArtifactData> pool = null;
            if (currentZone != null && currentZone.items != null && currentZone.items.Count > 0)
                pool = currentZone.items;
            else if (database != null)
                pool = database.All;

            Spawner.NewArtifactBoost = newArtifactBoost;
            var cfg = EconomyManager.Instance != null ? EconomyManager.Instance.Config : null;
            Spawner.Config = cfg;
            if (cfg != null)
            {
                var upgrades = Archaeo.Upgrades.UpgradeManager.Instance;
                int mapLevel = upgrades != null ? upgrades.MapLevel : 0;
                Spawner.IsRarityUnlocked = r => mapLevel >= cfg.rarityUnlockMapLevel[(int)r];
                Spawner.RareBoost = 1f + 0.1f * (upgrades != null ? upgrades.LevelOf(UpgradeType.Lupa) : 0);
            }
            return Spawner.Pick(pool, a => Collection.Has(a.id));
        }

        /// Se llama cuando el jugador completa el minijuego de restauración.
        public void CompleteRestoration(ArtifactData artifact)
        {
            if (artifact == null) return;

            if (pendingRestorations.Count > 0 && pendingRestorations.Peek() == artifact)
                pendingRestorations.Dequeue();

            Collection.MarkRestored(artifact.id);
            RestorationCompleted?.Invoke(artifact);
            PendingChanged?.Invoke();
        }

        /// Restaura la cola de pendientes (al cargar una partida).
        public void RestorePending(IEnumerable<ArtifactData> pending)
        {
            pendingRestorations.Clear();
            foreach (var a in pending)
                if (a != null) pendingRestorations.Enqueue(a);

            PendingChanged?.Invoke();
        }
    }

    public enum GrantOutcome { New, Copy, Sold }

    public readonly struct GrantResult
    {
        public readonly GrantOutcome Outcome;
        public readonly int Level;      // copias del objeto tras el hallazgo
        public readonly double Coins;   // monedas obtenidas al vender una copia
        public GrantResult(GrantOutcome outcome, int level, double coins) { Outcome = outcome; Level = level; Coins = coins; }
    }
}
