using UnityEngine;

namespace Archaeo.Data
{
    public enum UpgradeType { DigPower, Tools, Team, Helper, Museum }

    /// Una mejora comprable en la tienda.
    /// Los niveles van de startLevel a maxLevel. El valor del efecto en cada nivel
    /// está en valuePerLevel (el elemento 0 es el nivel 1, el elemento 1 el nivel 2...).
    [CreateAssetMenu(menuName = "Archaeo/Upgrade", fileName = "Upgrade_")]
    public class UpgradeData : ScriptableObject
    {
        public string id;                 // único y estable (se usa en el guardado, NO cambiar)
        public string displayName;
        public UpgradeType type;

        [Tooltip("Nivel con el que se empieza. Potencia de excavación: 1. Ayudantes: 0 (aún no tienes ninguno).")]
        public int startLevel = 1;
        public int maxLevel = 10;

        [Tooltip("Coste de la primera compra.")]
        public int baseCost = 100;
        [Tooltip("Cada compra siguiente cuesta este factor más que la anterior.")]
        public float costGrowth = 1.6f;

        [Tooltip("Valor del efecto en cada nivel. Elemento 0 = nivel 1. Potencia de excavación: 1, 2, 4, 7.")]
        public float[] valuePerLevel;

        [Tooltip("Texto del efecto. {0} es el valor. Ej.: \"Cada clic vale {0}\"")]
        public string effectFormat = "{0}";

        public bool IsMaxed(int level) => level >= maxLevel;

        /// Valor del efecto en un nivel (0 si el nivel es 0 o no hay datos).
        public float ValueAt(int level)
        {
            if (valuePerLevel == null || valuePerLevel.Length == 0 || level <= 0) return 0f;
            return valuePerLevel[Mathf.Min(level, valuePerLevel.Length) - 1];
        }

        /// Coste de subir de currentLevel a currentLevel + 1.
        public int CostToUpgrade(int currentLevel)
        {
            int purchases = Mathf.Max(0, currentLevel - startLevel);
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, purchases));
        }

        public string EffectText(int level) => string.Format(effectFormat, FormatValue(ValueAt(level)));

        public static string FormatValue(float value) => value.ToString("0.##");
    }
}