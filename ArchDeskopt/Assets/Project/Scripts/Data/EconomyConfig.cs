using System;
using UnityEngine;

namespace Archaeo.Data
{
    public enum EconomyRarity { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4 }

    /// Todas las constantes de la economía en un solo sitio (documento de diseño).
    /// Para reequilibrar: cambia los números aquí, sin tocar código.
    [CreateAssetMenu(menuName = "Archaeo/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Serializable]
        public class RarityRow
        {
            public string name;
            [Tooltip("Probabilidad (en %) de que el objeto sea de esta rareza entre las ya desbloqueadas. Se reparte a partes iguales entre los objetos de la rareza.")]
            public float chancePercent;
            [Tooltip("Multiplicador de ingresos")]
            public float multiplier;
            [Tooltip("Copias TOTALES acumuladas necesarias para cada rango E,F,D,C,B,A,S")]
            public int[] copiesForRank = new int[7];
        }

        [Serializable]
        public class RankRow
        {
            public string name;
            public float multiplier;
        }

        [Header("Zonas")]
        [Tooltip("B_zona: ingreso base (m/min) del objeto más básico de cada zona")]
        public float[] zoneBase = { 1f, 5f, 25f, 120f, 600f, 3000f };
        [Tooltip("Coste en monedas de desbloquear cada zona")]
        public double[] zoneCost = { 0, 2500, 60000, 1000000, 10000000, 100000000 };
        public string[] zoneNames =
        {
            "Huerto del abuelo", "Desierto de Kemet", "Selva de Itzamná",
            "Tundra glaciar", "Ruinas sumergidas", "Templo de la Luna"
        };

        [Header("Rarezas")]
        public RarityRow[] rarities =
        {
            new RarityRow { name = "Común",      chancePercent = 60, multiplier = 1f,   copiesForRank = new[] { 1, 3, 6, 12, 25, 50, 100 } },
            new RarityRow { name = "Poco común", chancePercent = 25, multiplier = 2.5f, copiesForRank = new[] { 1, 2, 4, 8, 15, 30, 60 } },
            new RarityRow { name = "Raro",       chancePercent = 10, multiplier = 6f,   copiesForRank = new[] { 1, 2, 3, 5, 8, 15, 30 } },
            new RarityRow { name = "Épico",      chancePercent = 4,  multiplier = 15f,  copiesForRank = new[] { 1, 2, 3, 4, 6, 10, 15 } },
            new RarityRow { name = "Legendario", chancePercent = 1,  multiplier = 40f,  copiesForRank = new[] { 1, 2, 3, 4, 5, 6, 8 } },
        };

        [Tooltip("Mejoras de MAPA compradas necesarias para que empiece a salir cada rareza (Común, Poco común, Raro, Épico, Legendario).")]
        public int[] rarityUnlockMapLevel = { 0, 1, 3, 5, 7 };

        [Header("Rangos (E → S)")]
        public RankRow[] ranks =
        {
            new RankRow { name = "E", multiplier = 1f },
            new RankRow { name = "F", multiplier = 1.5f },
            new RankRow { name = "D", multiplier = 2.25f },
            new RankRow { name = "C", multiplier = 3.5f },
            new RankRow { name = "B", multiplier = 5.5f },
            new RankRow { name = "A", multiplier = 9f },
            new RankRow { name = "S", multiplier = 15f },
        };
        [Tooltip("Rango S tras la habilidad 'Restaurador maestro'")]
        public float masterRankSMultiplier = 20f;

        [Header("Copias y colección")]
        [Tooltip("Una copia que sale con el objeto ya en S se vende por N minutos de su ingreso base")]
        public float autoSellMinutes = 30f;
        [Tooltip("Bonus a los ingresos de la sala al completar los 6 objetos (0.25 = +25 %)")]
        public float zoneCompleteBonus = 0.25f;

        [Header("Barra de excavación")]
        public int barThreshold = 400;
        [Tooltip("Cada nivel de Pico reforzado suma este % al progreso por interacción")]
        public float pickBonusPerLevel = 0.2f;
        [Tooltip("Tiempo mínimo entre dos QTEs (s)")]
        public float minSecondsBetweenQtes = 60f;
        [Range(0f, 1f), Tooltip("Tras un Fallo la barra queda en este % del umbral")]
        public float barAfterFail = 0.25f;

        [Header("QTE")]
        public float qteWaitSeconds = 20f;
        public float qteWaitSecondsFocus = 60f;
        [Tooltip("Bono de un Perfecto, en minutos de ingresos actuales")]
        public float perfectBonusMinutes = 2f;
        [Range(0f, 1f), Tooltip("Fallo: polvo de excavación = este % del bono de un Perfecto")]
        public float failDustFraction = 0.10f;

        [Header("Ingreso pasivo")]
        public float idleSecondsThreshold = 300f;
        [Range(0f, 1f)] public float idleIncomeFraction = 0.25f;

        // ---------- Comprobación rápida: clic derecho sobre el asset → "Comprobar ejemplos" ----------
        [ContextMenu("Comprobar ejemplos")]
        void CheckExamples()
        {
            var m = new Archaeo.Economy.EconomyMath(this);
            Check("Botón de latón, rango E", m.IncomePerMinute(0, EconomyRarity.Common, 1), 1);
            Check("Botón de latón, rango S", m.IncomePerMinute(0, EconomyRarity.Common, 100), 15);
            Check("Botón de latón, rango D (6 copias)", m.IncomePerMinute(0, EconomyRarity.Common, 6), 2.25);
            Check("Diente de dinosaurio, rango S", m.IncomePerMinute(0, EconomyRarity.Legendary, 8), 600);
            Check("Corazón de la Luna, rango E", m.IncomePerMinute(5, EconomyRarity.Legendary, 1), 120000);
            Check("Corazón de la Luna, rango S", m.IncomePerMinute(5, EconomyRarity.Legendary, 8), 1800000);
            Check("Máscara dorada, rango A (6 copias)", m.IncomePerMinute(1, EconomyRarity.Legendary, 6), 1800);
            Check("Interacciones sin mejoras", m.InteractionsToFill(0, 1f), 400);
            Check("Interacciones con Pico 5", m.InteractionsToFill(5, 1f), 200);
            Check("Interacciones con Pico 1 y Guantes (x2)", m.InteractionsToFill(1, 2f), 400 / (1.2 * 2));
            Check("QTE/h con 100 interacciones/min", m.QtesPerHour(100, 400), 15);
        }

        static void Check(string label, double got, double expected)
        {
            bool ok = Math.Abs(got - expected) < 0.001;
            string msg = $"[Economía] {label}: {got} (esperado {expected})";
            if (ok) Debug.Log("OK  " + msg); else Debug.LogError("MAL " + msg);
        }
    }
}
