using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.Qte.Games
{
    /// QTE nº 20 "Rayo de sol": cada clic gira un espejo (/ o \). Lleva el rayo hasta el ídolo.
    /// Perfecto = como mucho 1 clic de más. Con más rareza, 3 espejos.
    public class SunRayQte : QteGame
    {
        public override string Name => "Rayo de sol";
        public override string Hint => "Gira los espejos para iluminar el ídolo";

        const int Cols = 7, Rows = 3;

        // Direcciones: 0 derecha, 1 arriba, 2 izquierda, 3 abajo
        static readonly Vector2Int[] Dirs = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };
        static readonly int[] Slash = { 1, 0, 3, 2 };      // "/"  : derecha->arriba, arriba->derecha...
        static readonly int[] Back = { 3, 2, 1, 0 };       // "\"  : derecha->abajo, abajo->derecha...

        readonly Dictionary<Vector2Int, bool> mirrors = new Dictionary<Vector2Int, bool>();   // true = "/"
        readonly Dictionary<Vector2Int, Image> mirrorImages = new Dictionary<Vector2Int, Image>();
        Vector2Int sun = new Vector2Int(0, 1), idol;
        Image idolImage;
        RectTransform rays;
        float cellW, cellH;
        int clicks, wrongAtStart;

        protected override float ComputeDuration() => 5.5f;

        protected override void Setup()
        {
            cellW = Size.x / Cols;
            cellH = Size.y / Rows;

            // Camino correcto: se calcula primero y luego se desordenan los espejos
            bool top = Ctx.Rng.Next(2) == 0;
            int r = top ? 2 : 0;
            var solution = new List<KeyValuePair<Vector2Int, bool>>();

            if (Ctx.Difficulty01 >= 0.5f)
            {
                // 3 espejos: sol -> A(1,1) -> B(1,r) -> C(4,r) -> ídolo
                int outA = top ? 1 : 3;
                solution.Add(Pair(new Vector2Int(1, 1), 0, outA));
                solution.Add(Pair(new Vector2Int(1, r), outA, 0));
                int outC = top ? 3 : 1;
                solution.Add(Pair(new Vector2Int(4, r), 0, outC));
                idol = new Vector2Int(4, top ? 0 : 2);
            }
            else
            {
                // 2 espejos: sol -> A(2,1) -> B(2,r) -> ídolo
                int outA = top ? 1 : 3;
                solution.Add(Pair(new Vector2Int(2, 1), 0, outA));
                solution.Add(Pair(new Vector2Int(2, r), outA, 0));
                idol = new Vector2Int(5, r);
            }

            foreach (var kv in solution)
            {
                bool correct = kv.Value;
                mirrors[kv.Key] = Ctx.Rng.Next(2) == 0 ? correct : !correct;
            }

            // Que no empiece ya resuelto
            bool anyWrong = false;
            foreach (var kv in solution) if (mirrors[kv.Key] != kv.Value) anyWrong = true;
            if (!anyWrong) mirrors[solution[0].Key] = !solution[0].Value;

            foreach (var kv in solution) if (mirrors[kv.Key] != kv.Value) wrongAtStart++;

            rays = QteUi.Rect(Root, "Rays");
            rays.anchorMin = rays.anchorMax = rays.pivot = new Vector2(0.5f, 0.5f);

            Box("Sun", new Color(1f, 0.85f, 0.2f), new Vector2(12f, 12f), CellCenter(sun));
            idolImage = Box("Idol", new Color(0.45f, 0.60f, 0.80f), new Vector2(12f, 14f), CellCenter(idol));

            float len = Mathf.Min(cellW, cellH) * 0.95f;
            foreach (var kv in solution)
            {
                var img = Box("Mirror", new Color(0.80f, 0.90f, 0.95f), new Vector2(3f, len), CellCenter(kv.Key));
                mirrorImages[kv.Key] = img;
            }

            Retrace();
        }

        /// Espejo necesario para ir de la dirección 'din' a 'dout' (true = "/").
        static KeyValuePair<Vector2Int, bool> Pair(Vector2Int cell, int din, int dout)
            => new KeyValuePair<Vector2Int, bool>(cell, Slash[din] == dout);

        Vector2 CellCenter(Vector2Int c)
            => new Vector2(-Size.x * 0.5f + cellW * (c.x + 0.5f), -Size.y * 0.5f + cellH * (c.y + 0.5f));

        /// Sigue el rayo y lo dibuja. Devuelve true si llega al ídolo.
        bool Retrace()
        {
            for (int i = rays.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(rays.GetChild(i).gameObject);

            foreach (var kv in mirrors)
                mirrorImages[kv.Key].rectTransform.localEulerAngles = new Vector3(0f, 0f, kv.Value ? -45f : 45f);

            var pos = sun;
            int dir = 0;
            bool lit = false;

            for (int step = 0; step < 30; step++)
            {
                bool horizontal = dir == 0 || dir == 2;
                QteUi.Box(rays, "Ray", new Color(1f, 0.9f, 0.4f, 0.9f),
                    horizontal ? new Vector2(cellW, 3f) : new Vector2(3f, cellH), CellCenter(pos));

                if (pos == idol) { lit = true; break; }

                if (mirrors.TryGetValue(pos, out bool slash))
                    dir = (slash ? Slash : Back)[dir];

                pos += Dirs[dir];
                if (pos.x < 0 || pos.x >= Cols || pos.y < 0 || pos.y >= Rows) break;
            }

            idolImage.color = lit ? new Color(1f, 0.9f, 0.4f) : new Color(0.45f, 0.60f, 0.80f);
            return lit;
        }

        public override void PointerDown(Vector2 local)
        {
            if (IsFinished) return;

            Vector2Int best = default;
            float bestDist = Mathf.Max(cellW, cellH) * 0.7f;
            bool found = false;

            foreach (var kv in mirrors)
            {
                float d = Vector2.Distance(local, CellCenter(kv.Key));
                if (d <= bestDist) { bestDist = d; best = kv.Key; found = true; }
            }
            if (!found) return;

            clicks++;
            mirrors[best] = !mirrors[best];

            if (Retrace()) Finish(clicks <= wrongAtStart + 1 ? QteResult.Perfect : QteResult.Good);
        }
    }
}
