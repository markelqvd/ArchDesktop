using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Data;
using Archaeo.Upgrades;

namespace Archaeo.UI
{
    /// Una fila de la tienda: nombre, nivel y efecto, y el botón de comprar con su precio.
    public class UpgradeRowView : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text infoText;
        [SerializeField] Button buyButton;
        [SerializeField] TMP_Text buyButtonText;

        UpgradeData upgrade;

        public void Bind(UpgradeData data)
        {
            upgrade = data;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => UpgradeManager.Instance?.TryBuy(upgrade));
            Refresh();
        }

        public void Refresh()
        {
            var manager = UpgradeManager.Instance;
            if (manager == null || upgrade == null) return;

            int level = manager.GetLevel(upgrade);
            nameText.text = upgrade.displayName;

            if (upgrade.IsMaxed(level))
            {
                infoText.text = $"Nivel {level} (máx.) · {upgrade.EffectText(level)}";
                buyButtonText.text = "MÁX.";
                buyButton.interactable = false;
            }
            else
            {
                infoText.text = $"Nivel {level} · {upgrade.EffectText(level)} → {UpgradeData.FormatValue(upgrade.ValueAt(level + 1))}";
                buyButtonText.text = $"Mejorar ({manager.NextCost(upgrade)})";
                buyButton.interactable = manager.CanAfford(upgrade);
            }
        }
    }
}