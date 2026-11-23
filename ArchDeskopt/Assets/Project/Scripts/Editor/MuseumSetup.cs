#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Archaeo.UI;

namespace Archaeo.EditorTools
{
    public static class MuseumSetup
    {
        [MenuItem("Archaeo/Museo/Poner fondo del museo (escena abierta)")]
        public static void SetBackground()
        {
            var view = Object.FindFirstObjectByType<MuseumPanelView>(FindObjectsInactive.Include);
            if (view == null) { Debug.LogError("[Museo] No hay ningún MuseumPanelView en la escena abierta. Abre la escena Main."); return; }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Arte/FondoMuseo.png");
            if (sprite == null) { Debug.LogError("[Museo] No encuentro Assets/Project/Arte/FondoMuseo.png"); return; }

            var so = new SerializedObject(view);
            so.FindProperty("background").objectReferenceValue = sprite;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log("[Museo] Fondo asignado. Guarda la escena (Ctrl+S).");
        }
    }
}
#endif
