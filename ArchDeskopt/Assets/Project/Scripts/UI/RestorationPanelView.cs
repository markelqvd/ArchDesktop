using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Data;

namespace Archaeo.UI
{
    /// Panel de restauración: muestra el artefacto y el minijuego que le corresponde según su tipo.
    ///   Limpiar -> CleanMinigameView      Examinar -> ExamineMinigameView
    /// Los tipos sin minijuego todavía usan el botón "Completar" provisional.
    public class RestorationPanelView : MonoBehaviour
    {
        [SerializeField] Image itemImage;
        [SerializeField] TMP_Text titleText;
        [SerializeField] Button completeButton;           // provisional
        [SerializeField] CleanMinigameView cleanGame;     // tipo Limpiar
        [SerializeField] ExamineMinigameView examineGame; // tipo Examinar

        /// El jugador ha completado la restauración.
        public event Action OnCompleted;
        /// Se acabó el tiempo sin completarla.
        public event Action OnFailed;

        /// ¿Este tipo de restauración ya tiene minijuego?
        public bool HasMinigame(RestorationType type) =>
            (type == RestorationType.Clean && cleanGame != null) ||
            (type == RestorationType.Examine && examineGame != null);

        void Awake()
        {
            completeButton.onClick.AddListener(() => OnCompleted?.Invoke());

            if (cleanGame != null)
            {
                cleanGame.Completed += () => OnCompleted?.Invoke();
                cleanGame.TimedOut += () => OnFailed?.Invoke();
            }

            if (examineGame != null)
            {
                examineGame.Completed += () => OnCompleted?.Invoke();
                examineGame.TimedOut += () => OnFailed?.Invoke();
            }
        }

        public void Show(ArtifactData artifact)
        {
            gameObject.SetActive(true);

            titleText.text = artifact != null ? artifact.displayName : "???";
            ApplyImage(artifact);

            RestorationType type = artifact != null ? artifact.restorationType : RestorationType.Clean;
            bool useClean = artifact != null && type == RestorationType.Clean && cleanGame != null;
            bool useExamine = artifact != null && type == RestorationType.Examine && examineGame != null;

            if (cleanGame != null) cleanGame.gameObject.SetActive(useClean);
            if (examineGame != null) examineGame.gameObject.SetActive(useExamine);
            completeButton.gameObject.SetActive(!useClean && !useExamine);

            if (useClean) cleanGame.Begin();
            else if (useExamine) examineGame.Begin();
        }

        public void Hide() => gameObject.SetActive(false);

        void ApplyImage(ArtifactData artifact)
        {
            if (artifact != null && artifact.sprite != null)
            {
                itemImage.sprite = artifact.sprite;
                itemImage.color = Color.white;
            }
            else
            {
                // Sin sprite todavía: cuadrado del color de la rareza
                itemImage.sprite = null;
                itemImage.color = artifact != null && artifact.rarity != null
                    ? artifact.rarity.color
                    : Color.gray;
            }
        }
    }
}