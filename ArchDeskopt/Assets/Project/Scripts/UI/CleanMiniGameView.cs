using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Archaeo.UI
{
    /// Minijuego de restauración "Limpiar".
    /// Una capa de tierra cubre el artefacto; el jugador la quita manteniendo pulsado
    /// el botón izquierdo y pasando el ratón por encima, antes de que se acabe el tiempo.
    /// Va en un RawImage situado justo encima de la imagen del artefacto.
    [RequireComponent(typeof(RawImage))]
    public class CleanMinigameView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        const int Size = 128;          // resolución de la capa de tierra (píxeles)
        const byte CleanAlpha = 40;    // por debajo de esta opacidad, el píxel cuenta como limpio

        [SerializeField] TMP_Text progressText;

        [Header("Tiempo")]
        [Tooltip("Segundos para completar la limpieza. 0 = sin límite.")]
        [SerializeField] float timeLimitSeconds = 20f;
        [SerializeField] TMP_Text timerText;
        [Tooltip("Pausa tras agotarse el tiempo, antes de cerrar el panel.")]
        [SerializeField] float timeoutPauseSeconds = 0.8f;

        [Header("Dificultad")]
        [Tooltip("Radio del pincel, en píxeles de la capa de tierra (128 de ancho).")]
        [SerializeField, Range(4, 24)] int brushRadius = 9;
        [Tooltip("Fracción de tierra que hay que quitar para completar.")]
        [SerializeField, Range(0.5f, 1f)] float completeAt = 0.9f;
        [Tooltip("Pausa tras completar, para ver el objeto limpio.")]
        [SerializeField] float revealSeconds = 0.8f;

        public event Action Completed;
        public event Action TimedOut;

        RawImage image;
        RectTransform rect;
        Texture2D texture;
        Color32[] pixels;
        int cleaned;
        bool dirty;
        bool finished;
        bool hasLast;
        Vector2Int lastPoint;
        float deadline;
        int shownSeconds = -1;

        float Progress => cleaned / (float)(Size * Size);

        void Awake()
        {
            image = GetComponent<RawImage>();
            rect = (RectTransform)transform;
        }

        void OnDestroy()
        {
            if (texture != null) Destroy(texture);
        }

        /// Empieza una limpieza nueva con una capa de tierra distinta y el tiempo completo.
        public void Begin()
        {
            if (texture == null)
            {
                texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                pixels = new Color32[Size * Size];
                image.texture = texture;
            }

            StopAllCoroutines();   // por si quedaba una pausa de una partida anterior

            GenerateDirt();
            cleaned = 0;
            finished = false;
            hasLast = false;
            dirty = false;

            texture.SetPixels32(pixels);
            texture.Apply(false);
            UpdateProgressText();

            // Temporizador
            deadline = Time.realtimeSinceStartup + timeLimitSeconds;
            shownSeconds = -1;
            if (timerText != null)
            {
                timerText.gameObject.SetActive(timeLimitSeconds > 0f);
                UpdateTimerText(timeLimitSeconds);
            }
        }

        // ───────────── Generación de la tierra ─────────────
        void GenerateDirt()
        {
            float ox = UnityEngine.Random.value * 1000f;
            float oy = UnityEngine.Random.value * 1000f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float large = Mathf.PerlinNoise(x * 0.06f + ox, y * 0.06f + oy);
                    float fine = Mathf.PerlinNoise(x * 0.25f + oy, y * 0.25f + ox);
                    float shade = 0.65f + 0.5f * large + 0.15f * fine;

                    byte r = (byte)(Mathf.Clamp01(0.40f * shade) * 255f);
                    byte g = (byte)(Mathf.Clamp01(0.30f * shade) * 255f);
                    byte b = (byte)(Mathf.Clamp01(0.20f * shade) * 255f);
                    pixels[y * Size + x] = new Color32(r, g, b, 255);
                }
            }
        }

        // ───────────── Entrada ─────────────
        public void OnPointerDown(PointerEventData e)
        {
            if (finished) return;
            hasLast = false;
            HandlePointer(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (finished) return;
            HandlePointer(e);
        }

        public void OnPointerUp(PointerEventData e) => hasLast = false;

        void HandlePointer(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, e.position, e.pressEventCamera, out Vector2 local)) return;

            Rect r = rect.rect;
            float u = (local.x - r.x) / r.width;
            float v = (local.y - r.y) / r.height;

            if (u < 0f || u > 1f || v < 0f || v > 1f) { hasLast = false; return; }

            var p = new Vector2Int(Mathf.RoundToInt(u * (Size - 1)), Mathf.RoundToInt(v * (Size - 1)));

            if (hasLast) StampLine(lastPoint, p);
            else Stamp(p.x, p.y);

            lastPoint = p;
            hasLast = true;
        }

        // ───────────── Borrado ─────────────
        // Une dos puntos con varias pasadas del pincel para no dejar huecos si el ratón va rápido
        void StampLine(Vector2Int a, Vector2Int b)
        {
            float distance = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / (brushRadius * 0.5f)));

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Stamp(Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t)), Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t)));
            }
        }

        void Stamp(int cx, int cy)
        {
            int r = brushRadius;
            int x0 = Mathf.Max(0, cx - r), x1 = Mathf.Min(Size - 1, cx + r);
            int y0 = Mathf.Max(0, cy - r), y1 = Mathf.Min(Size - 1, cy + r);
            float r2 = r * r;

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float d2 = dx * dx + dy * dy;
                    if (d2 > r2) continue;

                    // Centro del pincel: limpio del todo. Borde: semitransparente
                    float d = Mathf.Sqrt(d2) / r;
                    byte target = d < 0.55f ? (byte)0 : (byte)(255f * Mathf.InverseLerp(0.55f, 1f, d));

                    int i = y * Size + x;
                    byte old = pixels[i].a;
                    if (target >= old) continue;

                    pixels[i].a = target;
                    if (old >= CleanAlpha && target < CleanAlpha) cleaned++;
                }
            }

            dirty = true;
        }

        // ───────────── Progreso y tiempo ─────────────
        void Update()
        {
            // Reloj real: el tiempo corre aunque los frames vayan lentos
            if (!finished && timeLimitSeconds > 0f)
            {
                float remaining = deadline - Time.realtimeSinceStartup;
                UpdateTimerText(remaining);

                if (remaining <= 0f)
                {
                    TimeOut();
                    return;
                }
            }

            if (!dirty) return;
            dirty = false;

            texture.SetPixels32(pixels);
            texture.Apply(false);
            UpdateProgressText();

            if (!finished && Progress >= completeAt) Finish();
        }

        void Finish()
        {
            finished = true;
            hasLast = false;

            // Revelar el resto de la tierra
            for (int i = 0; i < pixels.Length; i++) pixels[i].a = 0;
            texture.SetPixels32(pixels);
            texture.Apply(false);

            if (progressText != null) progressText.text = "¡Limpio!";
            StartCoroutine(RevealThenComplete());
        }

        void TimeOut()
        {
            finished = true;
            hasLast = false;

            if (timerText != null) timerText.text = "¡Tiempo!";
            if (progressText != null) progressText.text = "Se acabó el tiempo";
            StartCoroutine(PauseThenTimeOut());
        }

        IEnumerator RevealThenComplete()
        {
            yield return new WaitForSecondsRealtime(revealSeconds);
            Completed?.Invoke();
        }

        IEnumerator PauseThenTimeOut()
        {
            yield return new WaitForSecondsRealtime(timeoutPauseSeconds);
            TimedOut?.Invoke();
        }

        void UpdateProgressText()
        {
            if (progressText == null) return;
            // Se muestra relativo al umbral para que llegue a 100 % justo al completar
            int percent = Mathf.Min(100, Mathf.RoundToInt(Progress / completeAt * 100f));
            progressText.text = $"Limpieza: {percent} %";
        }

        void UpdateTimerText(float remaining)
        {
            if (timerText == null) return;

            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            if (seconds == shownSeconds) return;   // solo se reescribe cuando cambia el segundo

            shownSeconds = seconds;
            timerText.text = $"Tiempo: {seconds} s";
            timerText.color = seconds <= 5 ? new Color(1f, 0.4f, 0.35f) : Color.white;
        }
    }
}