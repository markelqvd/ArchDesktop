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
        [SerializeField] Button museumButton;
        [SerializeField] Button upgradesButton;

        public Button MuseumButton => museumButton;
        public Button UpgradesButton => upgradesButton;

        public void SetProgress(float normalized) => excavationBar.value = Mathf.Clamp01(normalized);
        public void SetCoins(long coins) => coinsText.text = coins.ToString("N0");
        public void SetZone(string zoneName) => zoneText.text = zoneName;
    }
}