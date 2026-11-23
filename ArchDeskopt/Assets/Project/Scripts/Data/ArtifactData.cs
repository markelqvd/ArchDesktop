using UnityEngine;

namespace Archaeo.Data
{
    [CreateAssetMenu(menuName = "Archaeo/Item", fileName = "Item_")]
    public class ArtifactData : ScriptableObject
    {
        public string id;                 // único y estable (se usa en el guardado, NO cambiar)
        public string displayName;
        [TextArea] public string description;
        public string era;
        public Sprite sprite;
        public RarityData rarity;
        public ZoneData zone;

        [Header("Economía (documento de diseño)")]
        public EconomyRarity economyRarity = EconomyRarity.Common;
        [Range(0, 5), Tooltip("0 = Huerto del abuelo ... 5 = Templo de la Luna")]
        public int zoneIndex;

        [Header("Economía")]
        public float baseIncomePerMinute = 10f;
        public float[] levelIncomes;      // opcional: ingresos/min por nivel (nivel 1 = índice 0)
        public int baseLevelUpCost = 50;
        public float costGrowth = 1.5f;

        [Header("Restauración")]
        public RestorationType restorationType;
    }

    public enum RestorationType { Clean, Reconstruct, Examine }
}