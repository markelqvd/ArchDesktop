using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Data;

namespace Archaeo.UI
{
    public class MuseumItemSlot : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Image rarityFrame;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text levelText;

        public void Setup(ItemData item, int level)
        {
            icon.sprite = item.sprite;
            nameText.text = item.displayName;
            levelText.text = $"Nv. {level}";
            if (item.rarity != null) rarityFrame.color = item.rarity.color;
        }
    }
}