using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.Qte
{
    /// Ayudas para construir interfaz desde código (sin prefabs).
    public static class QteUi
    {
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// Rectángulo de color. 'anchor' sirve a la vez de ancla y de pivote: (0.5,0.5) = centro.
        public static Image Box(Transform parent, string name, Color color, Vector2 size, Vector2 pos, Vector2? anchor = null, bool raycast = false)
        {
            var rt = Rect(parent, name);
            Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
            rt.anchorMin = rt.anchorMax = rt.pivot = a;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static TMP_Text Label(Transform parent, string name, string text, float fontSize, Color color,
                                     Vector2 size, Vector2 pos, Vector2? anchor = null,
                                     TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect(parent, name);
            Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
            rt.anchorMin = rt.anchorMax = rt.pivot = a;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        /// Estira un rectángulo para rellenar a su padre.
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
