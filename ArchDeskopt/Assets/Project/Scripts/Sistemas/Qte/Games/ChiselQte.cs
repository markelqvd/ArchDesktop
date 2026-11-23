using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.Qte.Games
{
    /// QTE nº 2 "Golpe de cincel": una aguja oscila sobre la roca; clic cuando pasa por la grieta.
    /// Perfecto = aguja en el centro de la grieta. Bien = dentro de la grieta. Fallo = fuera o sin clic.
    /// Escala con la rareza: grieta más estrecha y aguja más rápida.
    public class ChiselQte : QteGame
    {
        public override string Name => "Golpe de cincel";
        public override string Hint => "Clic cuando la aguja pase por la grieta";

        float TrackWidth;
        const float TrackHeight = 14f;

        RectTransform needle;
        Image needleImage;
        float needleX, dir = 1f, speed;
        float crackCenter, crackHalf, perfectHalf;

        protected override float ComputeDuration() => 5f - 1.5f * Ctx.Difficulty01;

        protected override void Setup()
        {
            float d = Ctx.Difficulty01;
            TrackWidth = Mathf.Max(100f, Root.rect.width - 8f);
            float k = TrackWidth / 280f;   // todo se diseñó para 280 px de pista
            float width = Mathf.Lerp(70f, 28f, d) * k * Ctx.WindowScale * (Ctx.Easy ? 1.5f : 1f);
            width = Mathf.Min(width, TrackWidth * 0.5f);
            speed = Mathf.Lerp(170f, 340f, d) * k * (Ctx.Easy ? 0.7f : 1f);

            crackHalf = width * 0.5f;
            perfectHalf = crackHalf * 0.4f;

            float limit = Mathf.Max(0f, TrackWidth * 0.5f - crackHalf - 8f);
            crackCenter = ((float)Ctx.Rng.NextDouble() * 2f - 1f) * limit;

            // Roca (pista)
            QteUi.Box(Root, "Track", new Color(0.30f, 0.27f, 0.24f), new Vector2(TrackWidth, TrackHeight), Vector2.zero);
            // Grieta (zona de acierto) y su centro
            QteUi.Box(Root, "Crack", new Color(0.95f, 0.72f, 0.20f), new Vector2(width, TrackHeight), new Vector2(crackCenter, 0f));
            QteUi.Box(Root, "CrackCenter", new Color(1f, 1f, 1f, 0.9f), new Vector2(1f, TrackHeight), new Vector2(crackCenter, 0f));

            // Aguja
            needleX = -TrackWidth * 0.5f;
            dir = 1f;
            needleImage = QteUi.Box(Root, "Needle", Color.white, new Vector2(3f, 26f), new Vector2(needleX, 0f));
            needle = needleImage.rectTransform;
        }

        protected override void OnTick(float dt)
        {
            if (needle == null) return;

            needleX += dir * speed * dt;
            float edge = TrackWidth * 0.5f;
            if (needleX > edge) { needleX = edge; dir = -1f; }
            else if (needleX < -edge) { needleX = -edge; dir = 1f; }

            needle.anchoredPosition = new Vector2(needleX, 0f);
        }

        public override void PointerDown(Vector2 local)
        {
            if (IsFinished) return;

            float miss = Mathf.Abs(needleX - crackCenter);
            QteResult result = miss <= perfectHalf ? QteResult.Perfect
                             : miss <= crackHalf ? QteResult.Good
                             : QteResult.Fail;

            if (needleImage != null)
                needleImage.color = result == QteResult.Fail ? new Color(0.9f, 0.25f, 0.2f) : new Color(0.4f, 1f, 0.4f);

            Finish(result);
        }
    }
}
