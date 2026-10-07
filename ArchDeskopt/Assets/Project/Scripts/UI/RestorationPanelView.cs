using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Data;

namespace Archaeo.UI
{
    /// Placeholder del QTE: muestra el objeto y un botón para completar.
    /// El puzzle real sustituirá al botón en días posteriores.
    public class RestorationPanelView : MonoBehaviour
    {
        [SerializeField] Image itemImage;
        [SerializeField] TMP_Text titleText;
        [SerializeField] Button completeButton;

        public event Action OnCompleted;

        void Awake() => completeButton.onClick.AddListener(() => OnCompleted?.Invoke());

        public void Show(ItemData item)
        {
            titleText.text = item != null ? item.displayName : "???";
            itemImage.sprite = item != null ? item.sprite : null;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}