using System;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Platform;

namespace Archaeo.UI
{
    /// Gestiona la zona de paneles que aparece encima de la ventana principal.
    /// Solo hay un panel abierto a la vez. Colócalo en el objeto Canvas.
    public class PanelHostView : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public Button button;     // botón que abre/cierra el panel (puede ser null)
            public GameObject panel;
        }

        [SerializeField] RectTransform area;        // PanelArea: contenedor de los paneles
        [SerializeField] int panelHeight = 400;     // alto de la zona, en unidades de referencia
        [SerializeField] Entry[] entries;

        [Header("Paneles abiertos desde el desplegable de ajustes")]
        [SerializeField] SettingsDropdownView dropdown;
        [SerializeField] GameObject optionsPanel;

        GameObject current;

        void Awake()
        {
            foreach (var e in entries)
            {
                if (e.panel == null) continue;
                e.panel.SetActive(false);
                if (e.button != null)
                {
                    var p = e.panel;
                    e.button.onClick.AddListener(() => Toggle(p));
                }
            }

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(false);
                if (dropdown != null)
                    dropdown.OptionsRequested += () => Toggle(optionsPanel);
            }

            area.gameObject.SetActive(false);
        }

        public void Toggle(GameObject panel)
        {
            if (current == panel) Close();
            else Show(panel);
        }

        public void Show(GameObject panel)
        {
            if (current != null) current.SetActive(false);
            current = panel;
            panel.SetActive(true);
            area.gameObject.SetActive(true);
            WindowManager.Instance?.SetExtraHeight(panelHeight);
        }

        public void Close()
        {
            if (current != null) current.SetActive(false);
            current = null;
            area.gameObject.SetActive(false);
            WindowManager.Instance?.SetExtraHeight(0);
        }
    }
}