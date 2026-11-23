using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Data;
using Archaeo.Sound;
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
            buyButton.onClick.AddListener(() =>
            {
                if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryBuy(upgrade))
                    GameAudio.Play(AudioIds.UI_BuyUpgrade);
            });
            UISoundIgnore.Mark(buyButton.gameObject);   // la compra tiene su propio sonido
            Refresh();
        }

        public void Refresh()
        {
            var manager = UpgradeManager.Instance;
            if (manager == null || upgrade == null) return;

            int level = manager.GetLevel(upgrade);
            nameText.text = upgrade.displayName;

            // Mejoras del diseño nuevo (con descripción)
            if (!string.IsNullOrEmpty(upgrade.description))
            {
                bool isMap = upgrade.type == UpgradeType.Map;
                bool multi = upgrade.maxLevel - upgrade.startLevel > 1;

                if (upgrade.IsMaxed(level))
                {
                    infoText.text = multi ? $"{upgrade.description}\nNivel {level}/{upgrade.maxLevel} (máx.)" : upgrade.description;
                    buyButtonText.text = isMap ? "HECHO" : "MÁX.";
                    buyButton.interactable = false;
                }
                else if (!manager.IsUnlocked(upgrade))
                {
                    infoText.text = $"{upgrade.description}\nPrimero: {upgrade.requires.displayName}";
                    buyButtonText.text = "Bloqueado";
                    buyButton.interactable = false;
                }
                else
                {
                    infoText.text = multi ? $"{upgrade.description}\nNivel {level}/{upgrade.maxLevel}" : upgrade.description;
                    buyButtonText.text = $"{(isMap ? "Hacer" : level == 0 ? "Comprar" : "Mejorar")} ({manager.NextCost(upgrade):N0})";
                    buyButton.interactable = manager.CanAfford(upgrade);
                }
                return;
            }

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