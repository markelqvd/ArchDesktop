using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.Qte.Games
{
    /// QTE nº 14 "Jeroglíficos": 3 símbolos parpadean; repítelos en el mismo orden.
    /// Perfecto = sin errores. Bien = 1 error. Con más rareza, secuencia de 4 o 5.
    public class HieroglyphQte : QteGame
    {
        public override string Name => "Jeroglíficos";
        public override string Hint => "Mira la secuencia y repítela";

        static readonly Color[] Colors =
        {
            new Color(0.95f, 0.35f, 0.30f), new Color(0.35f, 0.60f, 1f),
            new Color(0.40f, 0.90f, 0.45f), new Color(1f, 0.85f, 0.30f),
        };

        const float Step = 0.5f;     // segundos por símbolo mostrado
        const float Lit = 0.35f;

        readonly List<Image> symbols = new List<Image>();
        int[] sequence;
        int progress, errors, length;
        float flash;     // tiempo que queda de destello tras un clic
        int flashIndex = -1;

        int Len => 3 + Mathf.RoundToInt(2f * Ctx.Difficulty01);

        protected override float ComputeDuration() => Len * Step + 2.5f + 0.2f * Len;

        protected override void Setup()
        {
            length = Len;
            sequence = new int[length];
            for (int i = 0; i < length; i++) sequence[i] = Ctx.Rng.Next(4);

            float size = Mathf.Min(Size.y - 10f, 28f);
            float spacing = size + 10f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * spacing;
                symbols.Add(Box("Symbol" + i, Dim(Colors[i]), new Vector2(size, size), new Vector2(x, 0f)));
            }
        }

        static Color Dim(Color c) => new Color(c.r * 0.40f, c.g * 0.40f, c.b * 0.40f);

        bool Showing => Elapsed < length * Step;

        protected override void OnTick(float dt)
        {
            int lit = -1;
            if (Showing)
            {
                int idx = Mathf.FloorToInt(Elapsed / Step);
                if (idx < length && (Elapsed - idx * Step) < Lit) lit = sequence[idx];
            }
            else if (flash > 0f)
            {
                flash -= dt;
                lit = flashIndex;
            }

            for (int i = 0; i < 4; i++)
                symbols[i].color = i == lit ? Colors[i] : Dim(Colors[i]);
        }

        public override void PointerDown(Vector2 local)
        {
            if (IsFinished || Showing) return;

            for (int i = 0; i < 4; i++)
            {
                var rt = symbols[i].rectTransform;
                Vector2 d = local - rt.anchoredPosition;
                if (Mathf.Abs(d.x) > rt.sizeDelta.x * 0.5f || Mathf.Abs(d.y) > rt.sizeDelta.y * 0.5f) continue;

                flashIndex = i;
                flash = 0.15f;

                if (i == sequence[progress])
                {
                    progress++;
                    if (progress >= length) Finish(errors == 0 ? QteResult.Perfect : QteResult.Good);
                }
                else
                {
                    errors++;
                    if (errors >= 2) Finish(QteResult.Fail);
                }
                return;
            }
        }
    }
}
