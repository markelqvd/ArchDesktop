using System;
using System.Collections.Generic;
using System.Linq;
using Archaeo.Qte.Games;

namespace Archaeo.Qte
{
    /// Qué QTEs hay y cuáles toca en cada zona (según el documento de diseño).
    /// Para añadir un QTE nuevo: crear su clase y registrarla en 'Games'.
    /// Los nombres de ByZone que aún no estén registrados se ignoran.
    public static class QteCatalog
    {
        static readonly Dictionary<string, Func<QteGame>> Games = new Dictionary<string, Func<QteGame>>
        {
            { "Golpe de cincel", () => new ChiselQte() },
            { "Brújula", () => new CompassQte() },
            { "Mosaico", () => new MosaicQte() },
            { "Capas de tierra", () => new LayersQte() },
            { "Escorpión", () => new ScorpionQte() },
            { "Jeroglíficos", () => new HieroglyphQte() },
            { "Péndulo trampa", () => new PendulumQte() },
            { "Rayo de sol", () => new SunRayQte() },
        };

        static readonly string[][] ByZone =
        {
            /* 1 Huerto del abuelo */ new[] { "Pincelada", "Golpe de cincel", "Capas de tierra", "Mosaico", "Brújula", "Vasija rota" },
            /* 2 Desierto de Kemet */ new[] { "Escorpión", "Arena movediza", "Jeroglíficos", "Soplar el polvo", "Cubo del pozo", "Dial de la cámara" },
            /* 3 Selva de Itzamná  */ new[] { "Raíces", "Linterna", "Péndulo trampa", "Ídolo en equilibrio", "Frotado", "Mosaico" },
            /* 4 Tundra glaciar    */ new[] { "Golpe de cincel", "Soplar el polvo", "Linterna", "Capas de tierra", "Pincelada", "Brújula" },
            /* 5 Ruinas sumergidas */ new[] { "Fósil en el río", "Cubo del pozo", "Dial de la cámara", "Vasija rota", "Rayo de sol", "Constelación" },
            /* 6 Templo de la Luna */ new[] { "Constelación", "Rayo de sol", "Jeroglíficos", "Péndulo trampa", "Frotado", "Ídolo en equilibrio" },
        };

        /// Nombres de los QTE ya implementados.
        public static IEnumerable<string> Implemented => Games.Keys;

        public static QteGame Create(int zoneIndex, Random rng)
        {
            var zone = ByZone[Math.Max(0, Math.Min(ByZone.Length - 1, zoneIndex))];
            var available = zone.Where(Games.ContainsKey).ToList();

            if (available.Count == 0) return Games["Golpe de cincel"]();
            return Games[available[rng.Next(available.Count)]]();
        }

        /// Crea un QTE concreto por nombre (pruebas).
        public static QteGame CreateByName(string name) => Games.TryGetValue(name, out var f) ? f() : null;
    }
}
