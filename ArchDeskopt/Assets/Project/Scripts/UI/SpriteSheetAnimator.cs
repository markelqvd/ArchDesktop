using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Anima un Image con una hoja de sprites: una imagen con varios fotogramas del mismo tamaño
    /// en fila (o en varias filas, leídos de izquierda a derecha y de arriba abajo).
    /// No hace falta cortar la imagen en Unity: se divide sola al arrancar.
    /// Colócalo en un objeto UI con un componente Image.
    [RequireComponent(typeof(Image))]
    public class SpriteSheetAnimator : MonoBehaviour
    {
        [Header("Hoja de sprites")]
        [SerializeField] Texture2D sheet;
        [SerializeField] int frameWidth = 16;
        [SerializeField] int frameHeight = 16;

        [Header("Animación")]
        [SerializeField] float framesPerSecond = 8f;
        [Tooltip("Empieza en un fotograma al azar, para que varias animaciones iguales no vayan sincronizadas.")]
        [SerializeField] bool randomStart = true;

        [Header("Tamaño en pantalla")]
        [Tooltip("Si está marcado, el objeto toma el tamaño del fotograma × Pixel Scale.")]
        [SerializeField] bool applySize = true;
        /// Cada píxel del arte se dibuja con el tamaño del píxel del mapa (se define en WindowManager).
        float pixelScale => PixelArt.UnitsPerPixel;

        readonly List<Sprite> frames = new List<Sprite>();
        Image image;
        float timeOffset;
        int current = -1;

        void Awake()
        {
            image = GetComponent<Image>();
            image.raycastTarget = false;     // una decoración nunca debe bloquear clics
            image.preserveAspect = true;

            if (sheet == null)
            {
                Debug.LogWarning($"[Animación] '{name}': falta la hoja de sprites (campo Sheet).", this);
                enabled = false;
                return;
            }

            BuildFrames();

            if (applySize)
                ((RectTransform)transform).sizeDelta = new Vector2(frameWidth, frameHeight) * pixelScale;

            if (randomStart && frames.Count > 0 && framesPerSecond > 0f)
                timeOffset = Random.value * frames.Count / framesPerSecond;
        }

        void OnDestroy()
        {
            foreach (var sprite in frames)
                if (sprite != null) Destroy(sprite);
        }

        void BuildFrames()
        {
            int columns = sheet.width / frameWidth;
            int rows = sheet.height / frameHeight;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    // En la textura el origen está abajo: la primera fila es la de arriba
                    float y = sheet.height - (row + 1) * frameHeight;
                    var rect = new Rect(col * frameWidth, y, frameWidth, frameHeight);

                    Sprite sprite = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    sprite.name = $"{sheet.name}_{frames.Count}";
                    frames.Add(sprite);
                }
            }

            if (frames.Count == 0)
                Debug.LogWarning($"[Animación] '{name}': la hoja ({sheet.width}x{sheet.height}) no contiene fotogramas de {frameWidth}x{frameHeight}.", this);
        }

        void Update()
        {
            if (frames.Count == 0 || framesPerSecond <= 0f) return;

            // Reloj real: la animación sigue aunque la ventana esté en segundo plano
            int index = (int)((Time.unscaledTime + timeOffset) * framesPerSecond) % frames.Count;
            if (index == current) return;

            current = index;
            image.sprite = frames[index];
        }
    }
}