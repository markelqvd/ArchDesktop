using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.Qte.Games
{
    /// QTE nº 9 "Capas de tierra": clics rápidos para atravesar 3 estratos.
    /// Perfecto = menos de 2 s. Con más rareza, los estratos son más duros.
    public class LayersQte : QteGame
    {
        public override string Name => "Capas de tierra";
        public override string Hint => "¡Clic rápido para atravesar los estratos!";

        static readonly Color[] LayerColors =
        {
            new Color(0.55f, 0.38f, 0.24f), new Color(0.78f, 0.52f, 0.25f), new Color(0.50f, 0.50f, 0.52f)
        };

        Image[] bars;
        float barWidth;
        int hitsPerLayer, layer, hits;

        protected override float ComputeDuration() => 5f;

        protected override void Setup()
        {
            hitsPerLayer = Mathf.Max(3, Mathf.RoundToInt(ByDifficulty(4f, 7f)) - (Ctx.Easy ? 1 : 0));
            barWidth = Size.x - 16f;
            float h = (Size.y - 6f) / 3f - 2f;

            bars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float y = Size.y * 0.5f - 3f - h * 0.5f - i * (h + 2f);
                bars[i] = QteUi.Box(Root, "Layer" + i, LayerColors[i], new Vector2(barWidth, h), new Vector2(0f, y - Size.y * 0.5f + Size.y * 0.5f));
                // Se encoge desde el centro hacia los lados: anchura = fracción restante
            }
        }

        public override void PointerDown(Vector2 local)
        {
            if (IsFinished) return;

            hits++;
            float remaining = 1f - (float)hits / hitsPerLayer;
            var rt = bars[layer].rectTransform;
            rt.sizeDelta = new Vector2(barWidth * Mathf.Max(0f, remaining), rt.sizeDelta.y);

            if (hits >= hitsPerLayer)
            {
                bars[layer].gameObject.SetActive(false);   // el estrato salta en terrones
                layer++;
                hits = 0;

                if (layer >= 3)
                    Finish(Elapsed < 2f + (Ctx.Easy ? 0.5f : 0f) ? QteResult.Perfect : QteResult.Good);
            }
        }
    }
}
