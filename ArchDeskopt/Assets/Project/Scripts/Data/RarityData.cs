using UnityEngine;

namespace Archaeo.Data
{
    [CreateAssetMenu(menuName = "Archaeo/Rarity", fileName = "Rarity_")]
    public class RarityData : ScriptableObject
    {
        public string displayName;
        public Color color = Color.white;
        [Range(0f, 1f)] public float dropWeight = 1f;       // probabilidad relativa de aparición
        public int initialReward = 10;                      // monedas al restaurar por 1ª vez
        public float incomeMultiplier = 1f;                 // multiplicador de monedas/min
    }
}