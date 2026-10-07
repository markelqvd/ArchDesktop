using System.Collections.Generic;
using UnityEngine;

namespace Archaeo.Data
{
    [CreateAssetMenu(menuName = "Archaeo/Zone", fileName = "Zone_")]
    public class ZoneData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite background;
        public int unlockCost;
        public int activityPointsPerFind = 1000;     // PA necesarios por hallazgo (GDD: 1000 en zona 1)
        public List<ItemData> items = new();
    }
}