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

        public void Setup(ArtifactData artifact, int level)
        {
            icon.sprite = artifact.sprite;
            nameText.text = artifact.displayName;
            levelText.text = $"Nv. {level}";
            if (artifact.rarity != null) rarityFrame.color = artifact.rarity.color;
        }
    }
}