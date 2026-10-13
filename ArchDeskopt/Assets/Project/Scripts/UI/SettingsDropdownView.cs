using System;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Desplegable de ajustes: se abre y cierra con el botón naranja de la barra superior.
    /// "Mover la ventana" no se gestiona aquí: ese botón lleva su propio TitleBarView.
    public class SettingsDropdownView : MonoBehaviour
    {
        [SerializeField] Button toggleButton;
        [SerializeField] GameObject panel;

        [Header("Botones del desplegable")]
        [SerializeField] Button statsButton;
        [SerializeField] Button resizeButton;
        [SerializeField] Button optionsButton;
        [SerializeField] Button minimizeButton;
        [SerializeField] Button quitButton;

        // Se conectarán cuando existan los paneles correspondientes
        public event Action StatsRequested;
        public event Action ResizeRequested;
        public event Action OptionsRequested;

        void Awake()
        {
            panel.SetActive(false);

            toggleButton.onClick.AddListener(() => panel.SetActive(!panel.activeSelf));

            statsButton.onClick.AddListener(() => Choose(StatsRequested));
            resizeButton.onClick.AddListener(() => Choose(ResizeRequested));
            optionsButton.onClick.AddListener(() => Choose(OptionsRequested));

            minimizeButton.onClick.AddListener(() =>
            {
                panel.SetActive(false);
                WindowManager.Instance?.Minimize();
            });
            quitButton.onClick.AddListener(() => WindowManager.Instance?.Quit());
        }

        void Choose(Action action)
        {
            panel.SetActive(false);
            action?.Invoke();
        }
    }
}