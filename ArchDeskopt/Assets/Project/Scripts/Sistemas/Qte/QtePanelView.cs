using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Archaeo.Qte
{
    /// Panel del QTE en la esquina, minimalista. Se construye solo, sin prefabs.
    /// Colócalo UNA vez en el objeto Canvas de la escena Main.
    /// Tres tamaños: aviso (píldora pequeña), juego (compacto) y resultado (una línea).
    public class QtePanelView : MonoBehaviour
    {
        [Header("Posición")]
        [Tooltip("Separación del borde inferior derecho de la ventana")]
        [SerializeField] Vector2 margin = new Vector2(6f, 6f);

        [Header("Tamaños")]
        [SerializeField] Vector2 waitingSize = new Vector2(120f, 26f);
        [SerializeField] Vector2 playingSize = new Vector2(220f, 96f);
        [SerializeField] Vector2 resultSize = new Vector2(220f, 26f);

        [Header("Pruebas")]
        [Tooltip("Nombre exacto de un QTE para forzarlo (p. ej. Brújula, Mosaico, Capas de tierra, Escorpión, Jeroglíficos, Péndulo trampa, Rayo de sol, Golpe de cincel). Vacío = el de la zona.")]
        [SerializeField] string forceQte = "";

        static readonly Color[] RarityColors =
        {
            new Color(0.75f, 0.75f, 0.75f), new Color(0.35f, 0.85f, 0.45f), new Color(0.35f, 0.6f, 1f),
            new Color(0.75f, 0.45f, 1f), new Color(1f, 0.65f, 0.15f),
        };

        QteManager manager;
        RectTransform panel, gameRoot;
        TMP_Text mainLabel, hintLabel;
        Image swatch, timeFill;
        QteGame game;
        QteState shownState = (QteState)(-1);

        void Awake()
        {
            BuildUi();
            panel.gameObject.SetActive(false);
        }

        void Start()
        {
            manager = QteManager.EnsureExists();
            manager.StateChanged += OnStateChanged;
            OnStateChanged();
        }

        void OnDestroy()
        {
            if (manager != null) manager.StateChanged -= OnStateChanged;
        }

        [ContextMenu("Forzar QTE (pruebas)")]
        void DebugTrigger() => QteManager.Instance?.DebugTrigger();

        // ───────── Interfaz ─────────
        void BuildUi()
        {
            panel = QteUi.Rect(transform, "QtePanel");
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0f);
            panel.anchoredPosition = new Vector2(-margin.x, margin.y);
            panel.sizeDelta = waitingSize;

            // Borde de 1 px + fondo (el fondo recibe los clics)
            var border = QteUi.Box(panel, "Border", new Color(0.80f, 0.65f, 0.35f, 0.9f), Vector2.zero, Vector2.zero);
            QteUi.Stretch(border.rectTransform);
            var bg = QteUi.Box(panel, "Background", new Color(0.09f, 0.08f, 0.07f, 0.94f), Vector2.zero, Vector2.zero, null, true);
            QteUi.Stretch(bg.rectTransform);
            bg.rectTransform.offsetMin = new Vector2(1f, 1f);
            bg.rectTransform.offsetMax = new Vector2(-1f, -1f);
            bg.gameObject.AddComponent<PanelInput>().owner = this;

            // Aviso / resultado: un solo texto, con un cuadradito de color de rareza a la izquierda
            swatch = QteUi.Box(panel, "Swatch", Color.gray, new Vector2(8f, 8f), new Vector2(9f, 0f), new Vector2(0f, 0.5f));
            mainLabel = QteUi.Label(panel, "Main", "", 12f, Color.white, Vector2.zero, Vector2.zero, null, TextAlignmentOptions.Left);
            QteUi.Stretch(mainLabel.rectTransform);
            mainLabel.rectTransform.offsetMin = new Vector2(22f, 3f);
            mainLabel.rectTransform.offsetMax = new Vector2(-6f, -1f);
            mainLabel.enableAutoSizing = false;
            mainLabel.overflowMode = TextOverflowModes.Ellipsis;

            // Juego: una pista de texto arriba y el área del minijuego debajo
            hintLabel = QteUi.Label(panel, "Hint", "", 10f, new Color(0.8f, 0.78f, 0.7f), new Vector2(0f, 14f), new Vector2(0f, -3f), new Vector2(0.5f, 1f));
            hintLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            hintLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            hintLabel.rectTransform.sizeDelta = new Vector2(-8f, 14f);

            gameRoot = QteUi.Rect(panel, "GameRoot");
            gameRoot.anchorMin = gameRoot.anchorMax = gameRoot.pivot = new Vector2(0.5f, 0.5f);

            // Barra de tiempo fina (3 px) pegada abajo
            var barBg = QteUi.Box(panel, "TimeBg", new Color(0.25f, 0.22f, 0.20f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f));
            barBg.rectTransform.anchorMin = new Vector2(0f, 0f);
            barBg.rectTransform.anchorMax = new Vector2(1f, 0f);
            barBg.rectTransform.offsetMin = new Vector2(1f, 1f);
            barBg.rectTransform.offsetMax = new Vector2(-1f, 4f);
            timeFill = QteUi.Box(barBg.transform, "TimeFill", new Color(0.95f, 0.72f, 0.20f), Vector2.zero, Vector2.zero);
            QteUi.Stretch(timeFill.rectTransform);
        }

        void Layout(Vector2 size, bool showMain, bool showSwatch, bool showGame)
        {
            panel.sizeDelta = size;
            mainLabel.gameObject.SetActive(showMain);
            swatch.gameObject.SetActive(showSwatch);
            hintLabel.gameObject.SetActive(showGame);
            gameRoot.gameObject.SetActive(showGame);

            if (showGame)
            {
                gameRoot.sizeDelta = new Vector2(size.x - 16f, size.y - 30f);
                gameRoot.anchoredPosition = new Vector2(0f, -4f);
            }
        }

        // ───────── Estados ─────────
        void OnStateChanged()
        {
            var state = manager.State;
            if (state == shownState) return;
            shownState = state;

            ClearGame();

            switch (state)
            {
                case QteState.Idle:
                    panel.gameObject.SetActive(false);
                    break;

                case QteState.Waiting:
                    panel.gameObject.SetActive(true);
                    panel.SetAsLastSibling();
                    Layout(waitingSize, true, true, false);
                    SetSwatch(manager.ShowRarity);   // solo el Detector de metales enseña la rareza antes de jugar
                    mainLabel.text = "¡Excavar!";
                    break;

                case QteState.Playing:
                    Layout(playingSize, false, false, true);
                    game = (string.IsNullOrEmpty(forceQte) ? null : QteCatalog.CreateByName(forceQte)) ?? manager.CreateGame();
                    game.Finished += OnGameFinished;
                    game.Begin(gameRoot, manager.BuildContext());
                    hintLabel.text = game.Hint;
                    break;

                case QteState.Result:
                    Layout(resultSize, true, true, false);
                    SetSwatch(true);
                    ShowResult();
                    break;
            }
        }

        void SetSwatch(bool reveal)
        {
            if (!reveal) { swatch.color = new Color(0.45f, 0.45f, 0.45f); return; }

            int r = manager.Artifact != null ? (int)manager.Artifact.economyRarity : 0;
            swatch.color = RarityColors[Mathf.Clamp(r, 0, RarityColors.Length - 1)];
        }

        void ClearGame()
        {
            if (game == null) return;
            game.Finished -= OnGameFinished;
            game.Cleanup();
            game = null;
        }

        void OnGameFinished(QteResult result) => manager.Resolve(result);

        void ShowResult()
        {
            var r = manager.LastResult;
            var g = manager.LastGrant;
            string name = manager.Artifact != null ? manager.Artifact.displayName : "";

            if (r == QteResult.Fail)
            {
                mainLabel.text = $"<color=#FF6B5B>Fallo</color>  <size=10>polvo +{Format(manager.LastCoins)}</size>";
                return;
            }

            string head = r == QteResult.Perfect ? "<color=#FFD24A>¡Perfecto!</color>" : "<color=#7DFF8A>¡Bien!</color>";
            string tail = g.Outcome == Archaeo.Discovery.GrantOutcome.New ? "nuevo"
                        : g.Outcome == Archaeo.Discovery.GrantOutcome.Sold ? $"vendida +{Format(g.Coins)}"
                        : $"copia {g.Level}";
            mainLabel.text = $"{head} {name} <size=10>({tail})</size>";
        }

        static string Format(double v) => v >= 1000 ? v.ToString("0") : v.ToString("0.##");

        // ───────── Bucle ─────────
        void Update()
        {
            if (manager == null) return;

            switch (manager.State)
            {
                case QteState.Waiting:
                    SetBar(manager.WaitTotal > 0 ? manager.WaitLeft / manager.WaitTotal : 0f);
                    break;
                case QteState.Playing:
                    if (game != null)
                    {
                        game.Tick(Time.unscaledDeltaTime);
                        SetBar(game.Duration > 0 ? game.TimeLeft / game.Duration : 0f);
                    }
                    break;
                case QteState.Result:
                    SetBar(manager.ResultLeft / 2f);
                    break;
            }
        }

        void SetBar(float fraction)
            => timeFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);

        // ───────── Ratón ─────────
        Vector2 ToLocal(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(gameRoot, e.position, e.pressEventCamera != null ? e.pressEventCamera : e.enterEventCamera, out var local);
            return local;
        }

        void HandleDown(PointerEventData e)
        {
            if (manager == null) return;
            if (manager.State == QteState.Waiting) manager.BeginPlaying();
            else if (manager.State == QteState.Playing && game != null) game.PointerDown(ToLocal(e));
        }

        void HandleUp(PointerEventData e) { if (manager != null && manager.State == QteState.Playing && game != null) game.PointerUp(ToLocal(e)); }
        void HandleMove(PointerEventData e) { if (manager != null && manager.State == QteState.Playing && game != null) game.PointerMove(ToLocal(e)); }
        void HandleScroll(PointerEventData e) { if (manager != null && manager.State == QteState.Playing && game != null) game.Scroll(e.scrollDelta.y); }

        /// Reenvía los eventos del fondo al panel.
        class PanelInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IDragHandler, IScrollHandler
        {
            public QtePanelView owner;
            public void OnPointerDown(PointerEventData e) => owner.HandleDown(e);
            public void OnPointerUp(PointerEventData e) => owner.HandleUp(e);
            public void OnPointerMove(PointerEventData e) => owner.HandleMove(e);
            public void OnDrag(PointerEventData e) => owner.HandleMove(e);
            public void OnScroll(PointerEventData e) => owner.HandleScroll(e);
        }
    }
}
