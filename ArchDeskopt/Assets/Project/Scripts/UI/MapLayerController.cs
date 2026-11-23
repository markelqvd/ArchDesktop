using System;
using UnityEngine;
using UnityEngine.UI;
using Archaeo.Data;
using Archaeo.Discovery;
using Archaeo.Platform;
using Archaeo.Save;
using Archaeo.Sound;
using Archaeo.Upgrades;

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

        [Header("Qué cambia el mapa")]
        [Tooltip("Marcado: el mapa cambia al comprar las mejoras de la pestaña Mapa de la tienda (1 mejora = 1 capa). Desmarcado: por objetos restaurados.")]
        [SerializeField] bool useMapUpgrades = true;

        [Header("Qué cuenta (solo si no se usan mejoras de mapa)")]
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
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.MapPurchased += OnMapPurchased;
        }

        void OnDestroy()
        {
            if (discovery != null) discovery.RestorationCompleted -= OnRestored;
            if (SaveManager.Instance != null) SaveManager.Instance.Loaded -= OnLoaded;
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.MapPurchased -= OnMapPurchased;
        }

        // Compra de una mejora de mapa: nueva capa, con polvo
        void OnMapPurchased(int level)
        {
            if (!useMapUpgrades) return;
            int target = TargetIndex();
            if (target == current || transitioning) return;
            ChangeLayer(target);
        }

        // Al restaurar un objeto: si toca subir de capa, con polvo
        void OnRestored(ArtifactData artifact)
        {
            if (useMapUpgrades) return;
            int target = TargetIndex();
            if (target == current || transitioning) return;

            ChangeLayer(target);
        }

        void ChangeLayer(int target)
        {
            if (dust == null)
            {
                Debug.LogWarning("[Mapa] El campo 'Dust' está vacío: el cambio de capa es instantáneo. " +
                                 "Arrastra el objeto Dust al campo Dust de este controlador.", this);
                ApplyLayer(target);
                return;
            }

            Debug.Log($"[Mapa] Cambio a '{layers[target].name}' con polvo.", this);
            GameAudio.Play(AudioIds.Map_Transition);
            transitioning = true;
            dust.Play(() => ApplyLayer(target), OnDustDone);
        }

        /// Prueba rápida: cambia a la siguiente capa (y vuelve a la primera tras la última). Solo en Play.
        [ContextMenu("Probar cambio de capa")]
        void TestChange()
        {
            if (!Application.isPlaying || layers == null || layers.Length < 2 || transitioning) return;

            int target = current >= layers.Length - 1 ? 0 : current + 1;
            ChangeLayer(target);
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
            if (useMapUpgrades)
            {
                int level = UpgradeManager.Instance != null ? UpgradeManager.Instance.MapLevel : 0;
                return Mathf.Clamp(level, 0, layers.Length - 1);
            }

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
            MusicDirector.SetMapLayer(index);

            Sprite sprite = layers[index].sprite;
            if (sprite != null) mapImage.sprite = sprite;

            // Un objeto se ve si está en la lista de la capa actual (puede estar en varias capas)
            for (int i = 0; i < layers.Length; i++)
            {
                var objects = layers[i].onlyInThisLayer;
                if (objects == null) continue;
                foreach (var go in objects)
                    if (go != null) go.SetActive(false);
            }

            var visible = layers[index].onlyInThisLayer;
            if (visible != null)
                foreach (var go in visible)
                    if (go != null) go.SetActive(true);
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