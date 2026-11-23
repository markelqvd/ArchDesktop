using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.Qte.Games
{
    /// QTE nº 17 "Péndulo trampa": la cuchilla va y viene; clic para que el explorador cruce el hueco.
    /// Perfecto = cruza cuando la cuchilla está lejos del hueco. Con más rareza, 2 péndulos desfasados.
    public class PendulumQte : QteGame
    {
        public override string Name => "Péndulo trampa";
        public override string Hint => "Clic para cruzar cuando las cuchillas estén lejos";

        const float CrossTime = 0.5f;     // lo que tarda el explorador en cruzar
        const float GapTime = 0.25f;      // en ese momento pasa por el hueco

        Image[] blades;
        float[] phase, amp;
        float omega, gapHalf;
        Image explorer;
        float startX, endX;

        bool crossing;
        float crossT;
        QteResult pending;

        protected override float ComputeDuration() => 5f;

        protected override void Setup()
        {
            int count = Ctx.Difficulty01 >= 0.5f ? 2 : 1;
            blades = new Image[count];
            phase = new float[count];
            amp = new float[count];

            omega = 2f * Mathf.PI * ByDifficulty(0.55f, 0.95f) * SpeedFactor;
            gapHalf = 13f;

            // Hueco (zona segura) en el centro
            Box("Gap", new Color(0.05f, 0.05f, 0.05f, 0.8f), new Vector2(gapHalf * 2f, Size.y - 8f), Vector2.zero);

            for (int i = 0; i < count; i++)
            {
                phase[i] = i * Mathf.PI * 0.7f;
                amp[i] = Size.x * (0.33f - 0.05f * i);
                blades[i] = Box("Blade" + i, i == 0 ? new Color(0.9f, 0.25f, 0.2f) : new Color(0.95f, 0.55f, 0.2f),
                    new Vector2(4f, Size.y - 14f - 6f * i), new Vector2(0f, 0f));
            }

            startX = -Size.x * 0.5f + 10f;
            endX = Size.x * 0.5f - 10f;
            explorer = Box("Explorer", new Color(0.85f, 0.85f, 0.92f), new Vector2(8f, 14f), new Vector2(startX, -Size.y * 0.5f + 12f));
        }

        float BladeX(int i, float t) => amp[i] * Mathf.Sin(omega * t + phase[i]);

        protected override void OnTick(float dt)
        {
            for (int i = 0; i < blades.Length; i++)
                blades[i].rectTransform.anchoredPosition = new Vector2(BladeX(i, Elapsed), 0f);

            if (!crossing) return;

            crossT += dt;
            float k = Mathf.Clamp01(crossT / CrossTime);
            explorer.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, k), explorer.rectTransform.anchoredPosition.y);

            if (crossT >= CrossTime)
            {
                explorer.color = pending == QteResult.Fail ? new Color(0.9f, 0.25f, 0.2f) : new Color(0.4f, 1f, 0.4f);
                Finish(pending);
            }
        }

        public override void PointerDown(Vector2 local)
        {
            if (IsFinished || crossing) return;

            crossing = true;
            crossT = 0f;

            // ¿Dónde estarán las cuchillas cuando el explorador pase por el hueco?
            float tGap = Elapsed + GapTime;
            float safety = 1f;
            for (int i = 0; i < blades.Length; i++)
                safety = Mathf.Min(safety, Mathf.Abs(BladeX(i, tGap)) / amp[i]);

            float perfect = 0.70f / Tolerance;
            float good = 0.40f / Tolerance;
            pending = safety >= perfect ? QteResult.Perfect : safety >= good ? QteResult.Good : QteResult.Fail;
        }

        // El tiempo agotado no debe cortar un cruce ya iniciado
        protected override void OnTimeUp()
        {
            if (!crossing) Finish(QteResult.Fail);
        }
    }
}
