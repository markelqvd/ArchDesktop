using System;
using System.Collections.Generic;
using Archaeo.Data;

namespace Archaeo.Discovery
{
    /// Elige qué artefacto sale en un hallazgo.
    /// Probabilidad = peso de su rareza (dropWeight). Los que el jugador aún no tiene
    /// reciben un multiplicador extra, así los primeros hallazgos tienden a ser nuevos.
    /// Cuando ya los tiene todos, el multiplicador no influye y salen sobre todo repetidos.
    public class ArtifactSpawner
    {
        /// Multiplicador de peso para artefactos aún no encontrados.
        public float NewArtifactBoost { get; set; } = 4f;

        readonly Random rng = new Random();

        public ArtifactData Pick(IReadOnlyList<ArtifactData> pool, Func<ArtifactData, bool> isOwned)
        {
            if (pool == null || pool.Count == 0) return null;

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

            // Si todos los pesos son 0, se elige uno cualquiera válido
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

            // Por redondeo, devolver el último válido
            for (int i = pool.Count - 1; i >= 0; i--)
                if (pool[i] != null) return pool[i];
            return null;
        }
    }
}