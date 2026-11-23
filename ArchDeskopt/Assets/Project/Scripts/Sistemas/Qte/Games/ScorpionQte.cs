using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.Qte.Games
{
    /// QTE nº 10 "Escorpión": un escorpión corre hacia la reliquia; clic encima para espantarlo.
    /// Perfecto = al primer clic. Con más rareza: trayectoria en zigzag y 2 escorpiones.
    public class ScorpionQte : QteGame
    {
        public override string Name => "Escorpión";
        public override string Hint => "¡Espanta al escorpión con un clic!";

        class Scorpion
        {
            public Image img;
            public float x, baseY, phase, t;
            public bool scared;
        }

        readonly List<Scorpion> scorpions = new List<Scorpion>();
        float relicX, speed, zigzag;
        int misses;

        protected override float ComputeDuration() => 5f;

        protected override void Setup()
        {
            relicX = Size.x * 0.25f;
            Box("Relic", new Color(1f, 0.82f, 0.3f), new Vector2(12f, 12f), new Vector2(relicX, 0f));

            int count = Ctx.Difficulty01 >= 0.5f ? 2 : 1;
            float startX = -Size.x * 0.5f + 10f;
            float distance = relicX - startX;
            // Llega a la reliquia hacia el 80 % del tiempo si nadie lo espanta
            speed = distance / (Duration * 0.8f) * (1f + 0.25f * Ctx.Difficulty01) * SpeedFactor;
            zigzag = Size.y * ByDifficulty(0.10f, 0.28f);

            for (int i = 0; i < count; i++)
            {
                var s = new Scorpion
                {
                    x = startX - i * 26f,
                    baseY = ((float)Ctx.Rng.NextDouble() * 2f - 1f) * Size.y * 0.15f,
                    phase = (float)Ctx.Rng.NextDouble() * 6f,
                };
                s.img = Box("Scorpion" + i, new Color(0.65f, 0.15f, 0.10f), new Vector2(14f, 9f), new Vector2(s.x, s.baseY));
                scorpions.Add(s);
            }
        }

        protected override void OnTick(float dt)
        {
            bool allScared = scorpions.Count > 0;

            foreach (var s in scorpions)
            {
                s.t += dt;

                if (s.scared)
                {
                    s.x -= 300f * dt;
                    s.img.rectTransform.anchoredPosition = new Vector2(s.x, s.img.rectTransform.anchoredPosition.y);
                    s.img.gameObject.SetActive(s.x > -Size.x * 0.5f - 20f);
                    continue;
                }

                allScared = false;
                s.x += speed * dt;
                float y = s.baseY + Mathf.Sin(s.t * 7f + s.phase) * zigzag;
                s.img.rectTransform.anchoredPosition = new Vector2(s.x, y);

                if (s.x >= relicX - 8f) { Finish(QteResult.Fail); return; }   // llegó a la reliquia
            }

            if (allScared && !IsFinished)
                Finish(misses == 0 ? QteResult.Perfect : misses <= 2 ? QteResult.Good : QteResult.Fail);
        }

        public override void PointerDown(Vector2 local)
        {
            if (IsFinished) return;

            Scorpion best = null;
            float bestDist = 18f * Tolerance;
            foreach (var s in scorpions)
            {
                if (s.scared) continue;
                float d = Vector2.Distance(local, s.img.rectTransform.anchoredPosition);
                if (d <= bestDist) { bestDist = d; best = s; }
            }

            if (best != null)
            {
                best.scared = true;
                best.img.color = new Color(0.9f, 0.5f, 0.3f);   // huye
            }
            else
            {
                misses++;
            }
        }
    }
}
