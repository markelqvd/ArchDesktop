using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Humo en pixel art para una hoguera: volutas hechas de cuadraditos que suben, crecen por etapas
    /// y se desvanecen en escalones. Todo va encajado en la rejilla de píxeles del arte y se mueve "a saltos",
    /// como una animación de pocos fotogramas. No necesita imágenes: los sprites se generan por código.
    /// Colócalo en un RectTransform vacío justo encima de la llama; el humo se dibuja como hijo suyo.
    public class PixelSmokeEmitter : MonoBehaviour
    {
        class Puff
        {
            public RectTransform Rect;
            public Image Image;
            public float Age, Life, StartX, Phase;
            public int Stage = -1;
        }

        // Formas del humo según su edad (de arriba abajo; X = píxel lleno)
        static readonly string[][] Patterns =
        {
            new[] { "X" },
            new[] { "XX", "XX" },
            new[] { ".X.", "XXX", ".X." },
            new[] { ".XX.", "XXXX", ".XX." },
        };

        // Opacidad de cada etapa
        static readonly float[] StageAlpha = { 1f, 0.9f, 0.65f, 0.35f };

        /// Cada píxel del arte se dibuja con el tamaño del píxel del mapa (se define en WindowManager).
        float pixelScale => PixelArt.UnitsPerPixel;

        [Header("Emisión")]
        [Tooltip("Volutas por segundo (aproximado: el intervalo varía un poco).")]
        [SerializeField] float puffsPerSecond = 2.5f;
        [SerializeField] Vector2 lifeRange = new Vector2(1.6f, 2.4f);
        [SerializeField] int maxPuffs = 24;
        [Tooltip("Anchura de la zona de nacimiento, en píxeles del arte (a cada lado).")]
        [SerializeField] float spawnSpread = 1.5f;

        [Header("Movimiento (en píxeles del arte)")]
        [Tooltip("Píxeles que sube por segundo.")]
        [SerializeField] float riseSpeed = 6f;
        [Tooltip("Cuánto se balancea a los lados.")]
        [SerializeField] float swayAmount = 2f;
        [SerializeField] float swaySpeed = 1.6f;
        [Tooltip("Cuántas veces por segundo se actualiza el humo. Bajo = más aspecto de animación a pocos fotogramas.")]
        [SerializeField, Range(4f, 30f)] float stepsPerSecond = 8f;

        [Header("Colores")]
        [Tooltip("Color del humo recién salido (oscuro, como hollín).")]
        [SerializeField] Color freshColor = new Color(0.36f, 0.31f, 0.31f, 1f);
        [Tooltip("Color del humo ya viejo (claro).")]
        [SerializeField] Color agedColor = new Color(0.80f, 0.78f, 0.75f, 1f);

        readonly List<Puff> active = new List<Puff>();
        readonly Stack<Puff> pool = new Stack<Puff>();
        Sprite[] sprites;
        Texture2D[] textures;

        float nextEmit;
        float nextStep;
        float lastStep;

        void Awake() => BuildSprites();

        void OnEnable()
        {
            lastStep = Time.unscaledTime;
            nextStep = lastStep;
            nextEmit = Time.unscaledTime;
        }

        void OnDestroy()
        {
            if (sprites != null)
                foreach (var s in sprites) if (s != null) Destroy(s);
            if (textures != null)
                foreach (var t in textures) if (t != null) Destroy(t);
        }

        void Update()
        {
            float now = Time.unscaledTime;   // reloj real: el humo sigue aunque la ventana esté en segundo plano

            if (now >= nextEmit && active.Count < maxPuffs && puffsPerSecond > 0f)
            {
                Spawn();
                nextEmit = now + (1f / puffsPerSecond) * Random.Range(0.6f, 1.4f);
            }

            // El humo se mueve a saltos, no de forma continua
            if (now < nextStep) return;

            float dt = now - lastStep;
            lastStep = now;
            nextStep = now + 1f / stepsPerSecond;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                Puff puff = active[i];
                puff.Age += dt;

                if (puff.Age >= puff.Life)
                {
                    puff.Image.gameObject.SetActive(false);
                    pool.Push(puff);
                    active.RemoveAt(i);
                    continue;
                }

                Apply(puff);
            }
        }

        // ───────────── Volutas ─────────────
        void Spawn()
        {
            Puff puff = pool.Count > 0 ? pool.Pop() : CreatePuff();

            puff.Age = 0f;
            puff.Life = Random.Range(lifeRange.x, lifeRange.y);
            puff.StartX = Random.Range(-spawnSpread, spawnSpread);
            puff.Phase = Random.Range(0f, 6.28f);
            puff.Stage = -1;

            puff.Image.gameObject.SetActive(true);
            Apply(puff);
            active.Add(puff);
        }

        Puff CreatePuff()
        {
            var go = new GameObject("Puff", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;   // una decoración nunca debe bloquear clics

            return new Puff { Rect = rect, Image = image };
        }

        void Apply(Puff puff)
        {
            float fraction = puff.Age / puff.Life;
            int stage = fraction < 0.2f ? 0 : fraction < 0.45f ? 1 : fraction < 0.75f ? 2 : 3;

            // Cambio de etapa: otra forma, otro tamaño, otro color y otra opacidad (todo en escalones)
            if (stage != puff.Stage)
            {
                puff.Stage = stage;
                Sprite sprite = sprites[stage];
                puff.Image.sprite = sprite;
                puff.Rect.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height) * pixelScale;

                Color color = Color.Lerp(freshColor, agedColor, stage / 3f);
                color.a = StageAlpha[stage];
                puff.Image.color = color;
            }

            // Posición encajada en la rejilla de píxeles
            float x = puff.StartX + Mathf.Sin(puff.Age * swaySpeed + puff.Phase) * swayAmount;
            float y = puff.Age * riseSpeed;
            puff.Rect.anchoredPosition = new Vector2(Snap(x), Snap(y));
        }

        float Snap(float artPixels) => Mathf.Round(artPixels) * pixelScale;

        // ───────────── Sprites generados por código ─────────────
        void BuildSprites()
        {
            sprites = new Sprite[Patterns.Length];
            textures = new Texture2D[Patterns.Length];

            for (int i = 0; i < Patterns.Length; i++)
            {
                string[] rows = Patterns[i];
                int height = rows.Length;
                int width = rows[0].Length;

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,       // píxeles nítidos
                    wrapMode = TextureWrapMode.Clamp
                };

                var pixels = new Color32[width * height];
                for (int r = 0; r < height; r++)
                {
                    for (int c = 0; c < width; c++)
                    {
                        bool filled = rows[r][c] == 'X';
                        // La textura tiene el origen abajo: la primera fila del patrón es la de arriba
                        pixels[(height - 1 - r) * width + c] = filled ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(false);

                textures[i] = texture;
                sprites[i] = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
            }
        }
    }
}