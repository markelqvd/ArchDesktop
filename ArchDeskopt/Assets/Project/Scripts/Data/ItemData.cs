using UnityEngine;

namespace Archaeo.Data
{
    [CreateAssetMenu(menuName = "Archaeo/Item", fileName = "Item_")]
    public class ItemData : ScriptableObject
    {
        public string id;                 // único y estable (se usa en el guardado, NO cambiar)
        public string displayName;
        [TextArea] public string description;
        public string era;
        public Sprite sprite;
        public RarityData rarity;
        public ZoneData zone;

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