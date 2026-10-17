using System.Collections.Generic;
using UnityEngine;
using Archaeo.Discovery;
using Archaeo.Economy;

namespace Archaeo.UI
{
    /// Panel del museo: una casilla por cada objeto RESTAURADO.
    /// Se actualiza solo mientras está abierto (nuevos objetos, subidas de nivel).
    /// Colócalo en el objeto MuseumPanel.
    public class MuseumPanelView : MonoBehaviour
    {
        [SerializeField] Transform content;          // Content del Scroll View (con Grid Layout Group)
        [SerializeField] MuseumItemSlot slotPrefab;  // prefab UI_MuseumItemSlot
        [SerializeField] GameObject emptyText;       // "Aún no has restaurado ningún objeto"

        readonly Dictionary<string, MuseumItemSlot> slots = new Dictionary<string, MuseumItemSlot>();
        DiscoveryManager discovery;

        void OnEnable()
        {
            discovery = DiscoveryManager.Instance;
            if (discovery == null) return;

            discovery.Collection.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (discovery != null) discovery.Collection.Changed -= Refresh;
        }

        void Refresh()
        {
            var economy = EconomyManager.Instance;
            int shown = 0;

            foreach (var entry in discovery.Collection.Entries)
            {
                if (!entry.Restored) continue;   // solo cuentan los restaurados
                shown++;

                string id = entry.Artifact.id;
                if (!slots.TryGetValue(id, out MuseumItemSlot slot))
                {
                    slot = Instantiate(slotPrefab, content);
                    slots[id] = slot;
                }

                float incomePerSecond = economy != null
                    ? economy.IncomePerMinute(entry.Artifact, entry.Level) / 60f
                    : 0f;

                slot.Setup(entry.Artifact, entry.Level, incomePerSecond);
            }

            if (emptyText != null) emptyText.SetActive(shown == 0);
        }
    }
}