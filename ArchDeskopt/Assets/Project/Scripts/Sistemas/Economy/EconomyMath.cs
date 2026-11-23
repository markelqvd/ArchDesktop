using System;
using Archaeo.Data;

namespace Archaeo.Economy
{
    /// Las fórmulas de la economía, sin estado ni Unity: solo cuentas con los números de EconomyConfig.
    public class EconomyMath
    {
        readonly EconomyConfig cfg;

        /// true cuando se compra "Restaurador maestro" (rango S pasa de x15 a x20).
        public bool MasterRestorer { get; set; }

        public EconomyMath(EconomyConfig config) { cfg = config; }

        /// Rango según copias acumuladas: -1 = no lo tienes, 0 = E ... 6 = S
        public int RankIndex(EconomyRarity rarity, int copies)
        {
            if (copies < 1) return -1;
            var need = cfg.rarities[(int)rarity].copiesForRank;
            int rank = 0;
            for (int i = 0; i < need.Length; i++)
                if (copies >= need[i]) rank = i;
            return rank;
        }

        public float RankMultiplier(int rankIndex)
        {
            if (rankIndex < 0) return 0f;
            if (rankIndex >= cfg.ranks.Length - 1 && MasterRestorer) return cfg.masterRankSMultiplier;
            return cfg.ranks[Math.Min(rankIndex, cfg.ranks.Length - 1)].multiplier;
        }

        /// m/min = B_zona · M_rareza · M_rango · (1 + bonos)
        /// bonuses = suma de Vitrinas + Folleto + colección completa (0.25 = +25 %).
        public double IncomePerMinute(int zone, EconomyRarity rarity, int copies, double bonuses = 0)
        {
            int rank = RankIndex(rarity, copies);
            if (rank < 0) return 0;
            return cfg.zoneBase[zone] * cfg.rarities[(int)rarity].multiplier * RankMultiplier(rank) * (1 + bonuses);
        }

        /// Ingreso base (rango E, sin bonos): sirve para vender copias sobrantes.
        public double BaseIncome(int zone, EconomyRarity rarity)
            => cfg.zoneBase[zone] * cfg.rarities[(int)rarity].multiplier;

        /// Monedas al vender una copia sobrante (objeto ya en S).
        public double SellValue(int zone, EconomyRarity rarity, bool souvenirShop = false)
            => BaseIncome(zone, rarity) * cfg.autoSellMinutes * (souvenirShop ? 2 : 1);

        /// I = 400 / ((1 + 0,2·Pico) · Guantes/Teclado)
        /// inputMultiplier = 1 (nada), 2 (Guantes o Teclado en ese tipo de entrada).
        public double InteractionsToFill(int pickLevel, double inputMultiplier)
            => cfg.barThreshold / ((1 + cfg.pickBonusPerLevel * pickLevel) * inputMultiplier);

        /// QTE/h = interacciones por minuto · 60 / I
        public double QtesPerHour(double interactionsPerMinute, double interactionsToFill)
            => interactionsPerMinute * 60.0 / interactionsToFill;

        /// Coste de una habilidad en su nivel n (1 = primer nivel): base · g^(n-1)
        public static double SkillCost(double baseCost, double growth, int level)
            => baseCost * Math.Pow(growth <= 0 ? 1 : growth, Math.Max(0, level - 1));
    }
}
