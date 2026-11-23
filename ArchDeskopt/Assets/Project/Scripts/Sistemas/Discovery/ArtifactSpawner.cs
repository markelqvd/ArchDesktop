using System;
using System.Collections.Generic;
using Archaeo.Data;

namespace Archaeo.Discovery
{
    /// Elige qué artefacto sale en un hallazgo.
    ///   Diseño nuevo (Config asignada): primero se elige la RAREZA entre las desbloqueadas
    ///   (según su probabilidad) y luego un objeto de esa rareza al azar.
    ///   Diseño antiguo: probabilidad = peso de la rareza (dropWeight) con boost a los objetos nuevos.
    public class ArtifactSpawner
    {
        /// Multiplicador de peso para artefactos aún no encontrados (solo diseño antiguo).
        public float NewArtifactBoost { get; set; } = 4f;

        public EconomyConfig Config { get; set; }

        /// ¿Está desbloqueada esta rareza? (null = todas). La zona 1 empieza solo con Común.
        public Func<EconomyRarity, bool> IsRarityUnlocked { get; set; }

        /// Multiplicador de probabilidad de Raro, Épico y Legendario (habilidad Lupa).
        public float RareBoost { get; set; } = 1f;

        readonly Random rng = new Random();

        public ArtifactData Pick(IReadOnlyList<ArtifactData> pool, Func<ArtifactData, bool> isOwned)
        {
            if (pool == null || pool.Count == 0) return null;
            return Config != null ? PickByRarity(pool) : PickByWeight(pool, isOwned);
        }

        ArtifactData PickByRarity(IReadOnlyList<ArtifactData> pool)
        {
            int classes = Config.rarities.Length;
            var items = new List<ArtifactData>[classes];
            for (int i = 0; i < classes; i++) items[i] = new List<ArtifactData>();

            foreach (var a in pool)
            {
                if (a == null) continue;
                var r = a.economyRarity;
                if (IsRarityUnlocked != null && !IsRarityUnlocked(r)) continue;
                items[(int)r].Add(a);
            }

            var weights = new double[classes];
            double total = 0;
            for (int i = 0; i < classes; i++)
            {
                if (items[i].Count == 0) continue;
                double w = Math.Max(0f, Config.rarities[i].chancePercent) * (i >= (int)EconomyRarity.Rare ? RareBoost : 1f);
                weights[i] = w;
                total += w;
            }

            // Nada desbloqueado o todo con peso 0: cualquier objeto válido
            if (total <= 0)
            {
                foreach (var a in pool) if (a != null) return a;
                return null;
            }

            double roll = rng.NextDouble() * total;
            for (int i = 0; i < classes; i++)
            {
                roll -= weights[i];
                if (roll < 0 && items[i].Count > 0) return items[i][rng.Next(items[i].Count)];
            }

            for (int i = classes - 1; i >= 0; i--)
                if (items[i].Count > 0) return items[i][rng.Next(items[i].Count)];
            return null;
        }

        ArtifactData PickByWeight(IReadOnlyList<ArtifactData> pool, Func<ArtifactData, bool> isOwned)
        {
            var weights = new double[pool.Count];
            double total = 0;

            for (int i = 0; i < pool.Count; i++)
            {
                var a = pool[i];
                if (a == null) continue;

                double w = a.rarity != null ? Math.Max(0f, a.rarity.dropWeight) : 1.0;
                if (!isOwned(a)) w *= Math.Max(1f, NewArtifactBoost);

                weights[i] = w;
                total += w;
            }

            if (total <= 0)
            {
                foreach (var a in pool) if (a != null) return a;
                return null;
            }

            double roll = rng.NextDouble() * total;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0 && pool[i] != null) return pool[i];
            }

            for (int i = pool.Count - 1; i >= 0; i--)
                if (pool[i] != null) return pool[i];
            return null;
        }
    }
}
