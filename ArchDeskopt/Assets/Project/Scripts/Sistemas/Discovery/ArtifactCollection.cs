using System;
using System.Collections.Generic;
using Archaeo.Data;

namespace Archaeo.Discovery
{
    /// Los artefactos que el jugador ha encontrado, con su nivel.
    /// Un artefacto entra en la colección al ser encontrado (Restored = false)
    /// y pasa a Restored = true cuando completa el minijuego de restauración;
    /// solo entonces forma parte del museo.
    public class ArtifactCollection
    {
        public class Entry
        {
            public ArtifactData Artifact;
            public int Level = 1;
            public bool Restored;
        }

        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        public int Count => entries.Count;
        public IEnumerable<Entry> Entries => entries.Values;
        public event Action Changed;

        public bool Has(string id) => entries.ContainsKey(id);

        public bool TryGet(string id, out Entry entry) => entries.TryGetValue(id, out entry);

        public Entry AddNew(ArtifactData artifact)
        {
            var entry = new Entry { Artifact = artifact, Level = 1, Restored = false };
            entries[artifact.id] = entry;
            Changed?.Invoke();
            return entry;
        }

        public int LevelUp(string id)
        {
            var entry = entries[id];
            entry.Level++;
            Changed?.Invoke();
            return entry.Level;
        }

        public void MarkRestored(string id)
        {
            if (!entries.TryGetValue(id, out var entry)) return;
            entry.Restored = true;
            Changed?.Invoke();
        }
    }
}