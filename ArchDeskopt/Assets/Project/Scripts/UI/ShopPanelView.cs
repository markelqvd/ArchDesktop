using System.Collections.Generic;
using UnityEngine;
using Archaeo.Economy;
using Archaeo.Upgrades;

namespace Archaeo.UI
{
    /// Panel de la tienda: una fila por cada mejora disponible.
    /// Se actualiza solo mientras está abierto (monedas y niveles).
    /// Colócalo en el objeto ShopPanel.
    public class ShopPanelView : MonoBehaviour
    {
        [SerializeField] Transform content;          // Content del Scroll View
        [SerializeField] UpgradeRowView rowPrefab;   // prefab UI_UpgradeRow

        readonly List<UpgradeRowView> rows = new List<UpgradeRowView>();
        UpgradeManager upgrades;
        EconomyManager economy;

        void OnEnable()
        {
            upgrades = UpgradeManager.Instance;
            economy = EconomyManager.Instance;
            if (upgrades == null) return;

            if (rows.Count == 0) Build();

            upgrades.Changed += RefreshAll;
            if (economy != null) economy.CoinsChanged += RefreshAll;
            RefreshAll();
        }

        void OnDisable()
        {
            if (upgrades != null) upgrades.Changed -= RefreshAll;
            if (economy != null) economy.CoinsChanged -= RefreshAll;
        }

        void Build()
        {
            foreach (var upgrade in upgrades.All)
            {
                if (upgrade == null) continue;
                var row = Instantiate(rowPrefab, content);
                row.Bind(upgrade);
                rows.Add(row);
            }
        }

        void RefreshAll()
        {
            foreach (var row in rows) row.Refresh();
        }
    }
}