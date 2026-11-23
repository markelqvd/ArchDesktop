using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Data;
using Archaeo.Economy;
using Archaeo.Upgrades;

namespace Archaeo.UI
{
    /// Panel de la tienda con dos pestañas: "Mejoras" y "Mapa".
    /// Se actualiza solo mientras está abierto (monedas y niveles).
    /// Colócalo en el objeto ShopPanel. Los botones de pestaña se crean solos si no los asignas.
    public class ShopPanelView : MonoBehaviour
    {
        [SerializeField] Transform content;          // Content del Scroll View
        [SerializeField] UpgradeRowView rowPrefab;   // prefab UI_UpgradeRow

        [Header("Pestañas (opcional: si están vacías se crean solas)")]
        [SerializeField] Button upgradesTab;
        [SerializeField] Button mapTab;
        [Tooltip("Posición de las pestañas automáticas respecto a la esquina superior izquierda del panel")]
        [SerializeField] Vector2 tabOffset = new Vector2(8f, -4f);
        [SerializeField] Vector2 tabSize = new Vector2(80f, 20f);

        class Entry { public UpgradeRowView row; public bool isMap; }

        readonly List<Entry> rows = new List<Entry>();
        UpgradeManager upgrades;
        EconomyManager economy;
        bool showMap;
        Image upgradesTabImage, mapTabImage;

        void OnEnable()
        {
            upgrades = UpgradeManager.Instance;
            economy = EconomyManager.Instance;
            if (upgrades == null) return;

            if (rows.Count == 0) Build();
            BuildTabs();
            ApplyTab();

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
                rows.Add(new Entry { row = row, isMap = upgrade.type == UpgradeType.Map });
            }
        }

        // ───────── Pestañas ─────────
        void BuildTabs()
        {
            if (upgradesTab != null || mapTab != null)
            {
                // Botones puestos a mano
                if (upgradesTab != null) { upgradesTab.onClick.RemoveAllListeners(); upgradesTab.onClick.AddListener(() => SelectTab(false)); upgradesTabImage = upgradesTab.GetComponent<Image>(); }
                if (mapTab != null) { mapTab.onClick.RemoveAllListeners(); mapTab.onClick.AddListener(() => SelectTab(true)); mapTabImage = mapTab.GetComponent<Image>(); }
                return;
            }

            // Ya creadas en una apertura anterior
            if (upgradesTabImage != null) return;

            upgradesTab = MakeTab("TabMejoras", "Mejoras", tabOffset, () => SelectTab(false), out upgradesTabImage);
            mapTab = MakeTab("TabMapa", "Mapa", tabOffset + new Vector2(tabSize.x + 4f, 0f), () => SelectTab(true), out mapTabImage);
        }

        Button MakeTab(string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick, out Image image)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = tabSize;
            rt.anchoredPosition = pos;

            image = go.GetComponent<Image>();
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 12f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            go.transform.SetAsLastSibling();
            return button;
        }

        void SelectTab(bool map)
        {
            showMap = map;
            ApplyTab();
        }

        void ApplyTab()
        {
            foreach (var e in rows) e.row.gameObject.SetActive(e.isMap == showMap);

            if (upgradesTabImage != null) upgradesTabImage.color = showMap ? new Color(0.25f, 0.22f, 0.20f) : new Color(0.80f, 0.60f, 0.25f);
            if (mapTabImage != null) mapTabImage.color = showMap ? new Color(0.80f, 0.60f, 0.25f) : new Color(0.25f, 0.22f, 0.20f);
        }

        void RefreshAll()
        {
            foreach (var e in rows) e.row.Refresh();
        }
    }
}
