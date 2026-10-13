using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archaeo.UI
{
    /// Vista principal. Solo pinta datos; la lógica vive en los Systems.
    public class MainWindowView : MonoBehaviour
    {
        [SerializeField] Slider excavationBar;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] TMP_Text zoneText;

        [Header("Botones de la barra superior")]
        [SerializeField] Button museumButton;
        [SerializeField] Button upgradesButton;   // Tienda de mejoras (se mantiene el nombre para no perder la referencia)
        [SerializeField] Button locationsButton;
        [SerializeField] Button settingsButton;   // Desplegable de ajustes

        public Button MuseumButton => museumButton;
        public Button UpgradesButton => upgradesButton;
        public Button LocationsButton => locationsButton;
        public Button SettingsButton => settingsButton;

        public void SetProgress(float normalized) => excavationBar.value = Mathf.Clamp01(normalized);
        public void SetCoins(long coins) => coinsText.text = coins.ToString("N0");
        public void SetZone(string zoneName) => zoneText.text = zoneName;
    }
}