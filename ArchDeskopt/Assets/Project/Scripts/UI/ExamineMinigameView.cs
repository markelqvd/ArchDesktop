using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.UI
{
    /// Minijuego de restauración "Examinar".
    /// Aparecen varios puntos sobre el artefacto y hay que hacer clic en todos
    /// antes de que se acabe el tiempo.
    /// Va en un RectTransform vacío situado justo encima de la imagen del artefacto.
    public class ExamineMinigameView : MonoBehaviour
    {
        const string PointName = "ExaminePoint";

        class Point
        {
            public RectTransform Rect;
            public float Phase;
        }

        [Header("Zona donde aparecen los puntos (si está vacío, este mismo objeto)")]
        [SerializeField] RectTransform area;
        [SerializeField] TMP_Text progressText;

        [Header("Dificultad")]
        [SerializeField, Range(2, 10)] int pointCount = 5;
        [Tooltip("Tamaño de cada punto, en unidades de la UI.")]
        [SerializeField] float pointSize = 34f;
        [Tooltip("Distancia mínima entre dos puntos.")]
        [SerializeField] float minDistance = 56f;
        [SerializeField] Color pointColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Header("Tiempo")]
        [Tooltip("Segundos para completar el examen. 0 = sin límite.")]
        [SerializeField] float timeLimitSeconds = 15f;
        [SerializeField] TMP_Text timerText;
        [Tooltip("Pausa tras agotarse el tiempo, antes de cerrar el panel.")]
        [SerializeField] float timeoutPauseSeconds = 0.8f;
        [Tooltip("Pausa tras completar, antes de cerrar el panel.")]
        [SerializeField] float finishPauseSeconds = 0.6f;

        public event System.Action Completed;
        public event System.Action TimedOut;

        static Sprite circleSprite;

        readonly List<Point> points = new List<Point>();
        int found;
        int total;
        bool finished;
        float deadline;
        int shownSeconds = -1;

        RectTransform Area => area != null ? area : (RectTransform)transform;

        /// Empieza un examen nuevo, con puntos en posiciones distintas y el tiempo completo.
        public void Begin()
        {
            StopAllCoroutines();
            ClearPoints();

            finished = false;
            found = 0;
            total = pointCount;

            SpawnPoints();
            UpdateProgressText();

            deadline = Time.realtimeSinceStartup + timeLimitSeconds;
            shownSeconds = -1;
            if (timerText != null)
            {
                timerText.gameObject.SetActive(timeLimitSeconds > 0f);
                UpdateTimerText(timeLimitSeconds);
            }
        }

        // ───────────── Creación de los puntos ─────────────
        void SpawnPoints()
        {
            Rect r = Area.rect;
            float pad = pointSize * 0.7f;

            for (int i = 0; i < total; i++)
            {
                Vector2 position = Vector2.zero;

                // Posición aleatoria separada de las demás (con varios intentos)
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    position = new Vector2(
                        Random.Range(pad, Mathf.Max(pad + 1f, r.width - pad)),
                        Random.Range(pad, Mathf.Max(pad + 1f, r.height - pad)));

                    if (FarEnough(position)) break;
                }

                points.Add(CreatePoint(position));
            }
        }

        bool FarEnough(Vector2 position)
        {
            foreach (var p in points)
                if (Vector2.Distance(p.Rect.anchoredPosition, position) < minDistance) return false;
            return true;
        }

        Point CreatePoint(Vector2 position)
        {
            var go = new GameObject(PointName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            var rt = (RectTransform)go.transform;
            rt.SetParent(Area, false);

            rt.anchorMin = rt.anchorMax = Vector2.zero;   // coordenadas desde la esquina inferior izquierda
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(pointSize, pointSize);
            rt.anchoredPosition = position;

            var image = go.GetComponent<Image>();
            image.sprite = GetCircleSprite();
            image.color = pointColor;

            var point = new Point { Rect = rt, Phase = Random.Range(0f, 6.28f) };

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => OnPointClicked(point));

            return point;
        }

        void ClearPoints()
        {
            points.Clear();

            // También los que estaban desapareciendo
            for (int i = Area.childCount - 1; i >= 0; i--)
            {
                Transform child = Area.GetChild(i);
                if (child.name == PointName) Destroy(child.gameObject);
            }
        }

        // ───────────── Juego ─────────────
        void OnPointClicked(Point point)
        {
            if (finished || !points.Contains(point)) return;

            points.Remove(point);
            found++;
            StartCoroutine(PopAndDestroy(point.Rect));
            UpdateProgressText();

            if (found >= total) Finish();
        }

        void Update()
        {
            // Los puntos "laten" para llamar la atención
            if (!finished)
            {
                foreach (var p in points)
                {
                    float pulse = 1f + 0.12f * Mathf.Sin((Time.unscaledTime + p.Phase) * 6f);
                    p.Rect.localScale = Vector3.one * pulse;
                }
            }

            // Reloj real: el tiempo corre aunque los frames vayan lentos
            if (!finished && timeLimitSeconds > 0f)
            {
                float remaining = deadline - Time.realtimeSinceStartup;
                UpdateTimerText(remaining);

                if (remaining <= 0f) TimeOut();
            }
        }

        void Finish()
        {
            finished = true;
            if (progressText != null) progressText.text = "¡Examinado!";
            StartCoroutine(PauseThen(finishPauseSeconds, () => Completed?.Invoke()));
        }

        void TimeOut()
        {
            finished = true;
            if (timerText != null) timerText.text = "¡Tiempo!";
            if (progressText != null) progressText.text = "Se acabó el tiempo";
            StartCoroutine(PauseThen(timeoutPauseSeconds, () => TimedOut?.Invoke()));
        }

        IEnumerator PauseThen(float seconds, System.Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }

        IEnumerator PopAndDestroy(RectTransform rt)
        {
            var group = rt.GetComponent<CanvasGroup>();
            const float duration = 0.15f;

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break;
                float k = t / duration;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 1.6f, k);
                group.alpha = 1f - k;
                yield return null;
            }

            if (rt != null) Destroy(rt.gameObject);
        }

        // ───────────── Textos ─────────────
        void UpdateProgressText()
        {
            if (progressText != null) progressText.text = $"Detalles: {found}/{total}";
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

        // ───────────── Sprite del punto (se genera por código) ─────────────
        static Sprite GetCircleSprite()
        {
            if (circleSprite != null) return circleSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            float center = (size - 1) / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;

                    // Anillo exterior opaco, centro más suave, borde suavizado
                    float alpha;
                    if (d > 1f) alpha = 0f;
                    else if (d > 0.88f) alpha = Mathf.InverseLerp(1f, 0.88f, d);
                    else if (d > 0.62f) alpha = 1f;
                    else alpha = 0.55f;

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);

            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return circleSprite;
        }
    }
}