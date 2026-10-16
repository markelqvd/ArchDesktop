using System;
using System.Collections.Generic;
using UnityEngine;
using Archaeo.Data;
using Archaeo.Excavation;

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

        readonly Queue<ArtifactData> pendingRestorations = new Queue<ArtifactData>();

        public int PendingCount => pendingRestorations.Count;
        public ArtifactData NextPending => pendingRestorations.Count > 0 ? pendingRestorations.Peek() : null;

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
            ArtifactData artifact = ChooseArtifact();
            if (artifact == null)
            {
                Debug.LogWarning("[Descubrimiento] No hay artefactos disponibles en la zona.");
                return;
            }

            if (Collection.Has(artifact.id))
            {
                int level = Collection.LevelUp(artifact.id);
                Debug.Log($"[Descubrimiento] Repetido: {artifact.displayName} -> nivel {level}");
                ArtifactFound?.Invoke(artifact, false, level);
            }
            else
            {
                Collection.AddNew(artifact);
                pendingRestorations.Enqueue(artifact);
                Debug.Log($"[Descubrimiento] ¡NUEVO!: {artifact.displayName} ({artifact.restorationType}). Pendientes: {PendingCount}");
                ArtifactFound?.Invoke(artifact, true, 1);
                PendingChanged?.Invoke();
            }
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
    }
}