using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Archaeo.Data;
using Archaeo.Discovery;
using Archaeo.Economy;

namespace Archaeo.UI
{
    /// Museo con el fondo de pilares: cada pilar muestra un objeto del catálogo (9 por página).
    ///   - Los objetos que aún no tienes salen como silueta oscura.
    ///   - Al pasar el ratón por encima aparece un cuadro con el nombre, la rareza, el rango y los ingresos.
    /// Colócalo en el objeto MuseumPanel. Sustituye a la lista antigua (que se oculta sola).
    public class MuseumPanelView : MonoBehaviour
    {
        [Header("Antiguo (se oculta, se deja para no perder las referencias)")]
        [SerializeField] Transform content;
        [SerializeField] MuseumItemSlot slotPrefab;
        [SerializeField] GameObject emptyText;

        [Header("Fondo")]
        [Tooltip("FondoMuseo.png. Si se deja vacío, en el Editor se busca solo; en la build hay que asignarlo (menú Archaeo → Museo).")]
        [SerializeField] Sprite background;

        // Medidas del fondo en píxeles (255 x 153), con el origen arriba a la izquierda
        const float BgW = 255f, BgH = 153f;
        const float ItemSize = 36f;
        const int PerPage = 9;

        // Centro-x y borde superior (donde se apoya el objeto) de cada pilar: 4 de atrás y 5 de delante
        static readonly Vector2[] Pedestals =
        {
            new Vector2(55f, 51f), new Vector2(103f, 51f), new Vector2(151f, 51f), new Vector2(199f, 51f),
            new Vector2(33f, 85f), new Vector2(80f, 85f), new Vector2(127f, 85f), new Vector2(174f, 85f), new Vector2(220f, 85f),
        };

        RectTransform container, stage, tooltip;
        TMP_Text tooltipText, pageText;
        readonly List<PedestalSlot> slots = new List<PedestalSlot>();
        Button prevButton, nextButton;

        DiscoveryManager discovery;
        List<ArtifactData> catalog = new List<ArtifactData>();
        int page;
        bool built;

        int PageCount => Mathf.Max(1, Mathf.CeilToInt(catalog.Count / (float)PerPage));

        void OnEnable()
        {
            discovery = DiscoveryManager.Instance;
            if (discovery == null) return;

            if (!built) Build();

            discovery.Collection.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (discovery != null) discovery.Collection.Changed -= Refresh;
            HideTooltip();
        }

        // ───────── Construcción ─────────
        void Build()
        {
            built = true;

            // Ocultar la lista antigua y usar su mismo hueco
            var scroll = content != null ? content.GetComponentInParent<ScrollRect>(true) : GetComponentInChildren<ScrollRect>(true);
            RectTransform parent;
            if (scroll != null)
            {
                scroll.gameObject.SetActive(false);
                parent = (RectTransform)scroll.transform.parent;
                container = QuickRect(parent, "MuseumShowcase");
                var src = (RectTransform)scroll.transform;
                container.anchorMin = src.anchorMin; container.anchorMax = src.anchorMax;
                container.offsetMin = src.offsetMin; container.offsetMax = src.offsetMax;
                container.pivot = src.pivot;
            }
            else
            {
                container = QuickRect(transform, "MuseumShowcase");
                container.anchorMin = Vector2.zero; container.anchorMax = Vector2.one;
                container.offsetMin = new Vector2(8f, 8f); container.offsetMax = new Vector2(-8f, -48f);
            }
            if (emptyText != null) emptyText.SetActive(false);

            // Escenario con la proporción del fondo, centrado
            stage = QuickRect(container, "Stage");
            stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
            var fitter = stage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = BgW / BgH;

            var bg = stage.gameObject.AddComponent<Image>();
            bg.raycastTarget = false;
            Sprite sprite = ResolveBackground();
            if (sprite != null) { bg.sprite = sprite; bg.color = Color.white; }
            else
            {
                bg.color = new Color(0.24f, 0.2f, 0.2f);
                Debug.LogWarning("[Museo] Falta el fondo. Ejecuta Archaeo → Museo → Poner fondo del museo.", this);
            }

            // Un hueco por pilar
            for (int i = 0; i < PerPage; i++)
            {
                var go = new GameObject("Pedestal" + i, typeof(RectTransform), typeof(Image), typeof(PedestalSlot));
                go.transform.SetParent(stage, false);
                var rt = (RectTransform)go.transform;

                // Caja del objeto, en proporción al fondo: centrada sobre el pilar y apoyada en su borde superior
                Vector2 p = Pedestals[i];
                rt.anchorMin = new Vector2((p.x - ItemSize * 0.5f) / BgW, 1f - p.y / BgH);
                rt.anchorMax = new Vector2((p.x + ItemSize * 0.5f) / BgW, 1f - (p.y - ItemSize) / BgH);
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                var img = go.GetComponent<Image>();
                img.preserveAspect = true;
                var slot = go.GetComponent<PedestalSlot>();
                slot.owner = this;
                slot.image = img;
                slots.Add(slot);
            }

            BuildTooltip();
            BuildPager();
        }

        Sprite ResolveBackground()
        {
            if (background != null) return background;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Arte/FondoMuseo.png");
#else
            return null;
#endif
        }

        void BuildTooltip()
        {
            tooltip = QuickRect(container, "Tooltip");
            tooltip.sizeDelta = new Vector2(150f, 48f);

            var bg = tooltip.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.04f, 0.04f, 0.92f);
            bg.raycastTarget = false;

            var t = QuickRect(tooltip, "Text");
            t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one;
            t.offsetMin = new Vector2(6f, 3f); t.offsetMax = new Vector2(-6f, -3f);
            tooltipText = t.gameObject.AddComponent<TextMeshProUGUI>();
            tooltipText.fontSize = 11f;
            tooltipText.alignment = TextAlignmentOptions.Center;
            tooltipText.color = Color.white;
            tooltipText.raycastTarget = false;

            tooltip.gameObject.SetActive(false);
        }

        void BuildPager()
        {
            prevButton = MakeButton("Prev", "<", new Vector2(-40f, 4f), () => SetPage(page - 1));
            nextButton = MakeButton("Next", ">", new Vector2(40f, 4f), () => SetPage(page + 1));

            var t = QuickRect(container, "PageText");
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0f);
            t.sizeDelta = new Vector2(50f, 18f);
            t.anchoredPosition = new Vector2(0f, 4f);
            pageText = t.gameObject.AddComponent<TextMeshProUGUI>();
            pageText.fontSize = 11f;
            pageText.alignment = TextAlignmentOptions.Center;
            pageText.color = Color.white;
            pageText.raycastTarget = false;
        }

        Button MakeButton(string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var rt = QuickRect(container, name);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(22f, 18f);
            rt.anchoredPosition = pos;

            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.12f, 0.10f, 0.10f, 0.85f);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(onClick);

            var t = QuickRect(rt, "Label");
            t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one;
            t.offsetMin = t.offsetMax = Vector2.zero;
            var text = t.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 12f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }

        static RectTransform QuickRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        // ───────── Contenido ─────────
        void SetPage(int p)
        {
            page = Mathf.Clamp(p, 0, PageCount - 1);
            HideTooltip();
            Refresh();
        }

        void Refresh()
        {
            if (!built || discovery == null) return;

            // Catálogo completo en orden; si no hay base de datos, los objetos de la colección
            catalog.Clear();
            if (discovery.Database != null)
                foreach (var a in discovery.Database.All) if (a != null) catalog.Add(a);
            if (catalog.Count == 0)
                foreach (var e in discovery.Collection.Entries) catalog.Add(e.Artifact);

            page = Mathf.Clamp(page, 0, PageCount - 1);

            for (int i = 0; i < slots.Count; i++)
            {
                int index = page * PerPage + i;
                var slot = slots[i];

                if (index >= catalog.Count) { slot.gameObject.SetActive(false); continue; }

                slot.gameObject.SetActive(true);
                slot.item = catalog[index];

                bool owned = discovery.Collection.TryGet(slot.item.id, out var entry) && entry.Restored;
                slot.owned = owned;

                if (slot.item.sprite != null)
                {
                    slot.image.sprite = slot.item.sprite;
                    slot.image.color = owned ? Color.white : new Color(0f, 0f, 0f, 0.65f);   // silueta
                }
                else
                {
                    slot.image.sprite = null;
                    slot.image.color = owned ? new Color(0.9f, 0.7f, 0.3f) : new Color(0f, 0f, 0f, 0.5f);
                }
            }

            if (pageText != null) pageText.text = $"{page + 1}/{PageCount}";
            bool multiple = PageCount > 1;
            prevButton.gameObject.SetActive(multiple);
            nextButton.gameObject.SetActive(multiple);
            prevButton.interactable = page > 0;
            nextButton.interactable = page < PageCount - 1;
            pageText.gameObject.SetActive(multiple);
        }

        // ───────── Cuadro al pasar el ratón ─────────
        internal void ShowTooltip(PedestalSlot slot)
        {
            if (slot.item == null) return;

            tooltipText.text = BuildText(slot);
            tooltip.gameObject.SetActive(true);
            tooltip.SetAsLastSibling();

            // Encima del objeto; si no cabe arriba, debajo
            var slotRect = (RectTransform)slot.transform;
            var corners = new Vector3[4];
            slotRect.GetWorldCorners(corners);
            Vector3 top = (corners[1] + corners[2]) * 0.5f;
            Vector3 bottom = (corners[0] + corners[3]) * 0.5f;

            Vector2 localTop = container.InverseTransformPoint(top);
            bool above = localTop.y + tooltip.sizeDelta.y + 4f <= container.rect.yMax;

            tooltip.pivot = new Vector2(0.5f, above ? 0f : 1f);
            Vector2 local = container.InverseTransformPoint(above ? top : bottom);
            local.y += above ? 4f : -4f;

            float half = tooltip.sizeDelta.x * 0.5f;
            local.x = Mathf.Clamp(local.x, container.rect.xMin + half, container.rect.xMax - half);
            tooltip.localPosition = local;
        }

        internal void HideTooltip()
        {
            if (tooltip != null) tooltip.gameObject.SetActive(false);
        }

        string BuildText(PedestalSlot slot)
        {
            var item = slot.item;
            if (!slot.owned) return "<b>???</b>\n<size=9>Aún no descubierto</size>";

            var eco = EconomyManager.Instance;
            discovery.Collection.TryGet(item.id, out var entry);
            int copies = entry != null ? entry.Level : 1;

            string rarity = item.rarity != null ? item.rarity.displayName : "";
            string rank = "";
            if (eco != null && eco.Formulas != null && eco.Config != null)
            {
                int r = eco.Formulas.RankIndex(item.economyRarity, copies);
                if (r >= 0) rank = $" · Rango {eco.Config.ranks[Mathf.Min(r, eco.Config.ranks.Length - 1)].name}";
                rarity = eco.Config.rarities[(int)item.economyRarity].name;
            }

            float perSecond = eco != null ? eco.IncomePerMinute(item, copies) / 60f : 0f;
            string copiesText = copies == 1 ? "1 copia" : $"{copies} copias";
            return $"<b>{item.displayName}</b>\n<size=9>{rarity}{rank}\n{copiesText} · +{perSecond:0.##} monedas/s</size>";
        }

        /// Hueco de un pilar: avisa al museo cuando el ratón entra o sale.
        internal class PedestalSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public MuseumPanelView owner;
            public Image image;
            public ArtifactData item;
            public bool owned;

            public void OnPointerEnter(PointerEventData e) => owner.ShowTooltip(this);
            public void OnPointerExit(PointerEventData e) => owner.HideTooltip();
        }
    }
}
