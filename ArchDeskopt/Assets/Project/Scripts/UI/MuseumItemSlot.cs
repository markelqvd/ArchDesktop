using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Data;

namespace Archaeo.UI
{
    /// Casilla de un objeto en el museo: imagen, nombre, nivel e ingresos.
    public class MuseumItemSlot : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Image rarityFrame;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text incomeText;   // "+1 / s"

        public void Setup(ArtifactData item, int level, float incomePerSecond = 0f)
        {
            nameText.text = item.displayName;
            levelText.text = $"Nv. {level}";
            if (incomeText != null) incomeText.text = $"+{incomePerSecond:0.##} / s";

            Color rarityColor = item.rarity != null ? item.rarity.color : Color.gray;
            if (rarityFrame != null) rarityFrame.color = rarityColor;

            if (item.sprite != null)
            {
                icon.sprite = item.sprite;
                icon.color = Color.white;
            }
            else
            {
                // Sin sprite todavía: cuadrado con el color de la rareza, más claro que el marco
                icon.sprite = null;
                icon.color = Color.Lerp(rarityColor, Color.white, 0.45f);
            }
        }
    }
}