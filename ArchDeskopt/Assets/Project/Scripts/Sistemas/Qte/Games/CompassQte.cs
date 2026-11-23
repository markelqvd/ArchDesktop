using UnityEngine;

namespace Archaeo.Qte.Games
{
    /// QTE nº 3 "Brújula": la aguja gira; clic cuando apunta a la X. Perfecto = desvío menor de 5°.
    /// Con más rareza, la aguja cambia de sentido a mitad del tiempo y va más rápida.
    public class CompassQte : QteGame
    {
        public override string Name => "Brújula";
        public override string Hint => "Clic cuando la aguja apunte a la X";

        RectTransform needle;
        float angle, dir = 1f, speed, target, tolPerfect, tolGood;
        bool reversed;

        protected override float ComputeDuration() => 5f - Ctx.Difficulty01;

        protected override void Setup()
        {
            float face = Mathf.Min(Size.y - 4f, 56f);
            Box("Face", new Color(0.20f, 0.20f, 0.22f), new Vector2(face, face), Vector2.zero);

            speed = ByDifficulty(90f, 200f) * SpeedFactor;
            tolPerfect = 5f * Tolerance;
            tolGood = 18f * Tolerance;

            angle = Rand(0f, 360f);
            do { target = Rand(0f, 360f); } while (Mathf.Abs(Mathf.DeltaAngle(angle, target)) < 60f);
            dir = Ctx.Rng.NextDouble() < 0.5 ? 1f : -1f;

            // La X (una marca dorada girada 45°)
            float r = face * 0.38f;
            var mark = Box("X", new Color(1f, 0.8f, 0.25f), new Vector2(8f, 8f),
                new Vector2(Mathf.Sin(target * Mathf.Deg2Rad), Mathf.Cos(target * Mathf.Deg2Rad)) * r);
            mark.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

            // Aguja: pivote en la base, en el centro
            var n = QteUi.Box(Root, "Needle", Color.white, new Vector2(3f, face * 0.42f), Vector2.zero, new Vector2(0.5f, 0f));
            needle = n.rectTransform;
            needle.localEulerAngles = new Vector3(0f, 0f, -angle);
        }

        float Rand(float a, float b) => a + (float)Ctx.Rng.NextDouble() * (b - a);

        protected override void OnTick(float dt)
        {
            angle += dir * speed * dt;
            if (Ctx.Difficulty01 > 0.25f && !reversed && Elapsed > Duration * 0.5f) { dir = -dir; reversed = true; }
            if (needle != null) needle.localEulerAngles = new Vector3(0f, 0f, -angle);
        }

        public override void PointerDown(Vector2 local)
        {
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, target));
            Finish(diff <= tolPerfect ? QteResult.Perfect : diff <= tolGood ? QteResult.Good : QteResult.Fail);
        }
    }
}
