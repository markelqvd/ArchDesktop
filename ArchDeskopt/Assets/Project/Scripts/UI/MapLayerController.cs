using System;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Data;
using Archaeo.Discovery;
using Archaeo.Platform;
using Archaeo.Save;

namespace Archaeo.UI
{
    /// Cambia el mapa según el progreso: cada capa tiene su imagen y el número de objetos
    /// que hay que haber restaurado para verla. Al subir de capa durante el juego aparece una nube de polvo
    /// que esconde el cambio. Al cargar una partida se pone la capa que toca al instante, sin animación.
    /// Colócalo en UI_MainWindow (o en cualquier objeto de la ventana principal).
    public class MapLayerController : MonoBehaviour
    {
        [Serializable]
        public class Layer
        {
            public string name;
            public Sprite sprite;

            [Tooltip("Objetos restaurados necesarios para que se vea esta capa. Las capas van de menor a mayor.")]
            public int minRestoredObjects;

            [Tooltip("Objetos que solo se ven con esta capa (p. ej. la hoguera, con la capa 2).")]
            public GameObject[] onlyInThisLayer;
        }

        [Tooltip("La imagen que dibuja el mapa (la misma que define la forma de la ventana).")]
        [SerializeField] Image mapImage;
        [SerializeField] Layer[] layers;

        [Header("Transición")]
        [Tooltip("Nube de polvo que se usa al cambiar de capa. Si se deja vacío, el cambio es instantáneo.")]
        [SerializeField] PixelDustBurst dust;

        [Header("Qué cuenta")]
        [Tooltip("Marcado: cuentan los objetos restaurados (los del museo). Desmarcado: cuentan también los encontrados sin restaurar.")]
        [SerializeField] bool countRestoredOnly = true;

        DiscoveryManager discovery;
        int current = -1;
        bool transitioning;

        void Start()
        {
            if (mapImage == null || layers == null || layers.Length == 0)
            {
                Debug.LogWarning("[Mapa] Falta la imagen del mapa o las capas.", this);
                return;
            }

            CheckSizes();

            discovery = DiscoveryManager.Instance;
            ApplyLayer(TargetIndex());

            if (discovery != null) discovery.RestorationCompleted += OnRestored;
            if (SaveManager.Instance != null) SaveManager.Instance.Loaded += OnLoaded;
        }

        void OnDestroy()
        {
            if (discovery != null) discovery.RestorationCompleted -= OnRestored;
            if (SaveManager.Instance != null) SaveManager.Instance.Loaded -= OnLoaded;
        }

        // Al restaurar un objeto: si toca subir de capa, con polvo
        void OnRestored(ArtifactData artifact)
        {
            int target = TargetIndex();
            if (target == current || transitioning) return;

            if (dust == null)
            {
                ApplyLayer(target);
                return;
            }

            transitioning = true;
            dust.Play(() => ApplyLayer(target), OnDustDone);
        }

        void OnDustDone()
        {
            transitioning = false;

            // Por si se restauró otro objeto mientras tanto
            int target = TargetIndex();
            if (target != current) ApplyLayer(target);
        }

        // Al cargar la partida: la capa que toca, al instante
        void OnLoaded()
        {
            if (!transitioning) ApplyLayer(TargetIndex());
        }

        int TargetIndex()
        {
            int count = 0;
            if (discovery != null)
            {
                foreach (var entry in discovery.Collection.Entries)
                    if (!countRestoredOnly || entry.Restored) count++;
            }

            int index = 0;
            for (int i = 0; i < layers.Length; i++)
                if (count >= layers[i].minRestoredObjects) index = i;

            return index;
        }

        void ApplyLayer(int index)
        {
            current = index;

            Sprite sprite = layers[index].sprite;
            if (sprite != null) mapImage.sprite = sprite;

            // Cada objeto solo se ve con la capa de su lista
            for (int i = 0; i < layers.Length; i++)
            {
                var objects = layers[i].onlyInThisLayer;
                if (objects == null) continue;

                foreach (var go in objects)
                    if (go != null) go.SetActive(i == index);
            }
        }

        /// Todas las capas deben medir lo mismo en píxeles: si no, los píxeles del mapa se deformarían.
        void CheckSizes()
        {
            Sprite first = null;
            foreach (var layer in layers)
            {
                if (layer.sprite == null) continue;

                if (first == null) { first = layer.sprite; continue; }

                if (layer.sprite.rect.size != first.rect.size)
                {
                    Debug.LogWarning($"[Mapa] La capa '{layer.name}' mide {layer.sprite.rect.width}x{layer.sprite.rect.height} px " +
                                     $"y la primera {first.rect.width}x{first.rect.height} px. Tienen que medir lo mismo, " +
                                     "o los píxeles del mapa se verán deformados al cambiar de capa.", this);
                }
            }
        }
    }
}