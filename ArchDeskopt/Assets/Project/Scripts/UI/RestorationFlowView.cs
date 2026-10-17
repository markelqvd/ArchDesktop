using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Core;
using Archaeo.Data;
using Archaeo.Discovery;

namespace Archaeo.UI
{
    /// Conecta los hallazgos con la restauración:
    ///   hallazgo nuevo -> botón "Restaurar" -> panel de restauración -> museo.
    /// Colócalo en el objeto Canvas de la escena Main.
    public class RestorationFlowView : MonoBehaviour
    {
        [Header("Botón de aviso (dentro de la ventana principal)")]
        [SerializeField] Button restoreButton;
        [SerializeField] TMP_Text restoreButtonText;

        [Header("Panel de restauración")]
        [SerializeField] RestorationPanelView restorationPanel;

        [Header("Avisos (opcional)")]
        [SerializeField] NotificationToast toastPrefab;
        [SerializeField] Transform toastParent;

        DiscoveryManager discovery;
        ArtifactData current;   // artefacto que se está restaurando ahora

        void Awake()
        {
            restoreButton.onClick.AddListener(StartRestoration);
            restorationPanel.OnCompleted += FinishRestoration;

            restoreButton.gameObject.SetActive(false);
            restorationPanel.Hide();
        }

        void Start()
        {
            discovery = DiscoveryManager.Instance;
            if (discovery == null) return;   // p.ej. al probar Main sin pasar por Boot

            discovery.PendingChanged += Refresh;
            discovery.ArtifactFound += OnArtifactFound;
            Refresh();
        }

        void OnDestroy()
        {
            if (discovery == null) return;
            discovery.PendingChanged -= Refresh;
            discovery.ArtifactFound -= OnArtifactFound;
        }

        void OnArtifactFound(ArtifactData artifact, bool isNew, int level)
        {
            // Los repetidos no interrumpen: solo se avisa de los nuevos
            if (isNew) Toast("¡Se ha encontrado algo!");
        }

        void Refresh()
        {
            int pending = discovery.PendingCount;

            // El botón solo se ve si hay pendientes y no se está restaurando ya
            bool showButton = pending > 0 && current == null;
            restoreButton.gameObject.SetActive(showButton);
            if (showButton)
                restoreButtonText.text = pending > 1 ? $"¡Hallazgo! Restaurar ({pending})" : "¡Hallazgo! Restaurar";

            SyncState(pending);
        }

        // Excavating <-> Discovery según haya o no hallazgos pendientes
        void SyncState(int pending)
        {
            var machine = GameManager.Instance != null ? GameManager.Instance.StateMachine : null;
            if (machine == null) return;

            if (machine.Current == GameState.Excavating && pending > 0)
                machine.TryChangeState(GameState.Discovery);
            else if (machine.Current == GameState.Discovery && pending == 0)
                machine.TryChangeState(GameState.Excavating);
        }

        void StartRestoration()
        {
            current = discovery.NextPending;
            if (current == null) return;

            GameManager.Instance?.StateMachine.TryChangeState(GameState.Restoration);
            restoreButton.gameObject.SetActive(false);
            restorationPanel.Show(current);
        }

        void FinishRestoration()
        {
            if (current == null) return;

            ArtifactData done = current;
            current = null;

            restorationPanel.Hide();
            discovery.CompleteRestoration(done);

            GameManager.Instance?.StateMachine.TryChangeState(GameState.Excavating);
            Toast($"{done.displayName} añadido al museo");
            Refresh();   // si quedan pendientes, vuelve a Discovery y reaparece el botón
        }

        void Toast(string message)
        {
            if (toastPrefab == null) return;
            var parent = toastParent != null ? toastParent : transform;
            Instantiate(toastPrefab, parent).Show(message);
        }
    }
}