using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.Qte.Games
{
    /// QTE nº 5 "Mosaico": cada clic gira una tesela 90°. Hay que dejar todas con la muesca arriba.
    /// Perfecto = como mucho 1 clic de más. Con más rareza, 4 o 5 teselas.
    public class MosaicQte : QteGame
    {
        public override string Name => "Mosaico";
        public override string Hint => "Gira las teselas hasta que encajen";

        readonly List<RectTransform> tiles = new List<RectTransform>();
        readonly List<Image> tileImages = new List<Image>();
        int[] rot;      // cuartos de vuelta actuales (0 = encajada)
        float[] xs;
        float tileSize;
        int clicks, minClicks;

        protected override float ComputeDuration() => 4f + 0.4f * (3 + Mathf.RoundToInt(2f * Ctx.Difficulty01));

        protected override void Setup()
        {
            int n = 3 + Mathf.RoundToInt(2f * Ctx.Difficulty01);
            rot = new int[n];
            xs = new float[n];
            tileSize = Mathf.Min(Size.y - 8f, 34f);
            float spacing = tileSize + 8f;
            float total = n * spacing - 8f;

            for (int i = 0; i < n; i++)
            {
                int need = 1 + Ctx.Rng.Next(2);          // clics necesarios: 1 o 2
                rot[i] = (4 - need) % 4;
                minClicks += need;
                xs[i] = -total * 0.5f + tileSize * 0.5f + i * spacing;

                var img = Box("Tile" + i, Color.white, new Vector2(tileSize, tileSize), new Vector2(xs[i], 0f));
                QteUi.Box(img.transform, "Notch", new Color(1f, 0.95f, 0.8f), new Vector2(12f, 7f), Vector2.zero, new Vector2(0.5f, 1f));
                tiles.Add(img.rectTransform);
                tileImages.Add(img);
            }
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].localEulerAngles = new Vector3(0f, 0f, -90f * rot[i]);
                tileImages[i].color = rot[i] == 0 ? new Color(0.85f, 0.68f, 0.25f) : new Color(0.50f, 0.38f, 0.26f);
            }
        }

        public override void PointerDown(Vector2 local)
        {
            if (IsFinished || Mathf.Abs(local.y) > tileSize * 0.5f) return;

            for (int i = 0; i < xs.Length; i++)
            {
                if (Mathf.Abs(local.x - xs[i]) > tileSize * 0.5f) continue;

                clicks++;
                rot[i] = (rot[i] + 1) % 4;
                Refresh();

                bool solved = true;
                foreach (int r in rot) if (r != 0) solved = false;
                if (solved) Finish(clicks <= minClicks + 1 ? QteResult.Perfect : QteResult.Good);
                return;
            }
        }
    }
}
