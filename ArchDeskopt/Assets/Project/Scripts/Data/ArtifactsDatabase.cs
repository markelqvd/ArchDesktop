using System.Collections.Generic;
using UnityEngine;

namespace Archaeo.Data
{
    /// Lista maestra de todos los artefactos del juego, con búsqueda por id.
    /// El sistema de guardado usará los ids para reconstruir la colección.
    [CreateAssetMenu(menuName = "Archaeo/Artifact Database", fileName = "ArtifactDatabase")]
    public class ArtifactDatabase : ScriptableObject
    {
        [SerializeField] List<ArtifactData> artifacts = new();

        Dictionary<string, ArtifactData> byId;

        public IReadOnlyList<ArtifactData> All => artifacts;

        public bool TryGet(string id, out ArtifactData artifact)
        {
            BuildIndex();
            return byId.TryGetValue(id, out artifact);
        }

        public List<ArtifactData> GetByZone(ZoneData zone)
        {
            var result = new List<ArtifactData>();
            foreach (var a in artifacts)
                if (a != null && a.zone == zone) result.Add(a);
            return result;
        }

        void BuildIndex()
        {
            if (byId != null) return;
            byId = new Dictionary<string, ArtifactData>();

            foreach (var a in artifacts)
            {
                if (a == null) continue;
                if (string.IsNullOrEmpty(a.id))
                {
                    Debug.LogWarning($"[ArtifactDatabase] '{a.name}' no tiene id.", a);
                    continue;
                }
                if (!byId.TryAdd(a.id, a))
                    Debug.LogError($"[ArtifactDatabase] id duplicado: '{a.id}' ('{a.name}').", a);
            }
        }

        // Avisa en el Editor de ids vacíos o repetidos al modificar la lista
        void OnValidate()
        {
            byId = null;
            BuildIndex();
        }
    }
}