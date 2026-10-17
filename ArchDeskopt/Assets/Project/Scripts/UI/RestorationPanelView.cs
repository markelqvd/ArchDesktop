using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Data;

namespace Archaeo.UI
{
    /// Panel de restauración: muestra el artefacto y el minijuego que le corresponde.
    /// Tipos sin minijuego todavía usan el botón "Completar" provisional.
    public class RestorationPanelView : MonoBehaviour
    {
        [SerializeField] Image itemImage;
        [SerializeField] TMP_Text titleText;
        [SerializeField] Button completeButton;        // provisional
        [SerializeField] CleanMinigameView cleanGame;  // minijuego de tipo Limpiar

        public event Action OnCompleted;

        void Awake()
        {
            completeButton.onClick.AddListener(() => OnCompleted?.Invoke());
            if (cleanGame != null) cleanGame.Completed += () => OnCompleted?.Invoke();
        }

        public void Show(ArtifactData artifact)
        {
            gameObject.SetActive(true);

            titleText.text = artifact != null ? artifact.displayName : "???";
            ApplyImage(artifact);

            bool useClean = artifact != null
                            && artifact.restorationType == RestorationType.Clean
                            && cleanGame != null;

            if (cleanGame != null) cleanGame.gameObject.SetActive(useClean);
            completeButton.gameObject.SetActive(!useClean);

            if (useClean) cleanGame.Begin();
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