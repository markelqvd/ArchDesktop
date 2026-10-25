using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Activity;

namespace Archaeo.UI
{
    /// Cada cierto número de acciones del jugador (clics o pulsaciones) aparece una mini animación
    /// en un lugar aleatorio de la zona de excavación: un pico dando picazos o una brocha limpiando.
    /// Colócalo en un RectTransform vacío de la ventana principal (EffectsLayer), detrás del resto de la UI.
    public class DigEffectView : MonoBehaviour
    {
        [Header("Zona donde aparecen (si está vacío, este mismo objeto)")]
        [SerializeField] RectTransform area;

        [Header("Sprites")]
        [SerializeField] Sprite pickSprite;
        [SerializeField] Sprite brushSprite;
        [Tooltip("Cada píxel del sprite se dibuja de este tamaño (unidades de la UI).")]
        [SerializeField] float pixelScale = 6f;

        [Header("Frecuencia")]
        [Tooltip("La animación sale cada X acciones, con X al azar entre estos dos valores.")]
        [SerializeField] int minActions = 12;
        [SerializeField] int maxActions = 18;
        [Tooltip("Máximo de animaciones al mismo tiempo.")]
        [SerializeField] int maxAtOnce = 2;

        [Header("Colores de las partículas")]
        [SerializeField] Color dustColor = new Color(0.55f, 0.40f, 0.27f, 1f);
        [SerializeField] Color sparkleColor = new Color(1f, 0.95f, 0.65f, 1f);

        // Ángulos del pico (grados). 0 = sprite sin girar; negativo = hacia arriba
        const float PickRaised = -35f;
        const float PickStrike = 20f;
        const float BrushSwing = 12f;

        ActivityManager activity;
        int actions;
        int nextAt;
        int running;

        RectTransform Area => area != null ? area : (RectTransform)transform;

        void Start()
        {
            activity = ActivityManager.Instance;
            if (activity == null) return;   // p.ej. al probar Main sin pasar por Boot

            nextAt = Random.Range(minActions, maxActions + 1);
            activity.ActivityRegistered += OnActivity;
        }

        void OnDestroy()
        {
            if (activity != null) activity.ActivityRegistered -= OnActivity;
        }

        void OnActivity(ActivityEvent e)
        {
            actions++;
            if (actions < nextAt) return;

            actions = 0;
            nextAt = Random.Range(minActions, maxActions + 1);
            Play();
        }

        /// Lanza una animación al azar. También se puede probar desde el menú del componente, en Play.
        [ContextMenu("Probar animación")]
        public void Play()
        {
            if (running >= maxAtOnce || !Area.gameObject.activeInHierarchy) return;

            bool usePick = Random.value < 0.5f;
            if (usePick && pickSprite != null) StartCoroutine(PickRoutine());
            else if (brushSprite != null) StartCoroutine(BrushRoutine());
            else if (pickSprite != null) StartCoroutine(PickRoutine());
        }

        // ───────────── Pico: 2 o 3 picazos ─────────────
        IEnumerator PickRoutine()
        {
            running++;

            Vector2 size = pickSprite.rect.size * pixelScale;
            Vector2 position = RandomPosition(size);
            RectTransform rt = CreateSprite(pickSprite, position, out CanvasGroup group);

            yield return StartCoroutine(Pop(rt));

            int strikes = Random.Range(2, 4);   // 2 o 3
            yield return StartCoroutine(Rotate(rt, 0f, PickRaised, 0.16f, false));

            for (int i = 0; i < strikes; i++)
            {
                // Picazo rápido, con aceleración, y tierra al golpear
                yield return StartCoroutine(Rotate(rt, PickRaised, PickStrike, 0.07f, true));
                StartCoroutine(ParticleBurst(HeadPosition(position, size, PickStrike), dustColor, 5, 130f, 0.45f, 450f));

                yield return new WaitForSecondsRealtime(0.06f);

                if (i < strikes - 1)
                    yield return StartCoroutine(Rotate(rt, PickStrike, PickRaised, 0.14f, false));
            }

            yield return StartCoroutine(Rotate(rt, PickStrike, 0f, 0.12f, false));
            yield return StartCoroutine(FadeAndDestroy(group));
            running--;
        }

        // ───────────── Brocha: limpiando ─────────────
        IEnumerator BrushRoutine()
        {
            running++;

            Vector2 size = brushSprite.rect.size * pixelScale;
            Vector2 position = RandomPosition(size);
            RectTransform rt = CreateSprite(brushSprite, position, out CanvasGroup group);

            yield return StartCoroutine(Pop(rt));

            float angle = 0f;
            float target = BrushSwing;

            for (int i = 0; i < 4; i++)
            {
                yield return StartCoroutine(Rotate(rt, angle, target, 0.16f, false));
                StartCoroutine(ParticleBurst(HeadPosition(position, size, target), dustColor, 3, 60f, 0.4f, 120f));

                angle = target;
                target = -target;
            }

            yield return StartCoroutine(Rotate(rt, angle, 0f, 0.1f, false));

            // Destellos de "limpio"
            StartCoroutine(ParticleBurst(HeadPosition(position, size, 0f), sparkleColor, 4, 70f, 0.5f, -60f));
            yield return new WaitForSecondsRealtime(0.25f);

            yield return StartCoroutine(FadeAndDestroy(group));
            running--;
        }

        // ───────────── Utilidades ─────────────
        /// Posición aleatoria (esquina inferior izquierda del área = 0,0). El sprite se dibuja hacia
        /// arriba y a la izquierda de este punto (su "mano"), así que se deja margen para que quepa.
        Vector2 RandomPosition(Vector2 size)
        {
            Rect r = Area.rect;
            float x = Random.Range(size.x, Mathf.Max(size.x + 1f, r.width));
            float y = Random.Range(0f, Mathf.Max(1f, r.height - size.y));
            return new Vector2(x, y);
        }

        /// Dónde queda la punta del sprite (arriba a la izquierda) al girarlo "angle" grados alrededor de la mano.
        static Vector2 HeadPosition(Vector2 hand, Vector2 size, float angle)
        {
            Vector3 offset = new Vector3(-size.x * 0.85f, size.y * 0.85f, 0f);
            return hand + (Vector2)(Quaternion.Euler(0f, 0f, angle) * offset);
        }

        RectTransform CreateSprite(Sprite sprite, Vector2 position, out CanvasGroup group)
        {
            var go = new GameObject(sprite.name + "Effect", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rt = (RectTransform)go.transform;
            rt.SetParent(Area, false);

            rt.anchorMin = rt.anchorMax = Vector2.zero;   // coordenadas desde la esquina inferior izquierda del área
            rt.pivot = new Vector2(1f, 0f);               // la mano: abajo a la derecha
            rt.sizeDelta = sprite.rect.size * pixelScale;
            rt.anchoredPosition = position;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;                   // nunca bloquea clics

            group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            return rt;
        }

        IEnumerator Pop(RectTransform rt)
        {
            const float duration = 0.14f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                float scale = k < 0.7f ? Mathf.Lerp(0f, 1.15f, k / 0.7f) : Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f);
                rt.localScale = Vector3.one * scale;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        IEnumerator Rotate(RectTransform rt, float from, float to, float duration, bool accelerate)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                k = accelerate ? k * k : Mathf.SmoothStep(0f, 1f, k);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(from, to, k));
                yield return null;
            }
            rt.localRotation = Quaternion.Euler(0f, 0f, to);
        }

        IEnumerator FadeAndDestroy(CanvasGroup group)
        {
            const float duration = 0.2f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (group == null) yield break;
                group.alpha = 1f - t / duration;
                yield return null;
            }

            if (group != null) Destroy(group.gameObject);
        }

        /// Un puñado de cuadraditos que salen disparados hacia arriba y se desvanecen.
        IEnumerator ParticleBurst(Vector2 origin, Color color, int count, float speed, float life, float gravity)
        {
            var items = new RectTransform[count];
            var images = new Image[count];
            var velocities = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(Area, false);
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);

                float s = pixelScale * Random.Range(0.6f, 1f);
                rt.sizeDelta = new Vector2(s, s);
                rt.anchoredPosition = origin;

                var image = go.GetComponent<Image>();
                image.color = color;
                image.raycastTarget = false;

                items[i] = rt;
                images[i] = image;

                float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;   // hacia arriba
                velocities[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed * Random.Range(0.5f, 1f);
            }

            for (float t = 0f; t < life; t += Time.unscaledDeltaTime)
            {
                float dt = Time.unscaledDeltaTime;
                float fade = 1f - t / life;

                for (int i = 0; i < count; i++)
                {
                    if (items[i] == null) continue;
                    velocities[i].y -= gravity * dt;
                    items[i].anchoredPosition += velocities[i] * dt;

                    Color c = color;
                    c.a = color.a * fade;
                    images[i].color = c;
                }
                yield return null;
            }

            for (int i = 0; i < count; i++)
                if (items[i] != null) Destroy(items[i].gameObject);
        }
    }
}