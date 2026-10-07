using UnityEngine;

namespace Archaeo.Data
{
    public enum UpgradeType { DigPower, Tools, Team, Helper, Museum }

    [CreateAssetMenu(menuName = "Archaeo/Upgrade", fileName = "Upgrade_")]
    public class UpgradeData : ScriptableObject
    {
        public string id;
        public string displayName;
        public UpgradeType type;
        public int maxLevel = 10;
        public int baseCost = 100;
        public float costGrowth = 1.6f;
        public float[] valuePerLevel;   // p.ej. DigPower: 1,2,4,7...  Helper: 0.5, 2, 5 PA/s

        public int CostForLevel(int level) =>
            Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, level));
    }
}