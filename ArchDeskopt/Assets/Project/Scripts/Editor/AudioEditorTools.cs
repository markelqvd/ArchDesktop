using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Archaeo.Sound;

namespace Archaeo.SoundTools
{
    /// Rutas de la carpeta de audio del proyecto.
    public static class AudioEditorPaths
    {
        public static string Root => AssetDatabase.IsValidFolder("Assets/Project") ? "Assets/Project/Audio" : "Assets/Audio";
        public static string Events => Root + "/Eventos";

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static List<T> LoadAll<T>() where T : Object
        {
            var list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }
            return list;
        }
    }

    /// Menú Archaeo > Audio: crea los assets del catálogo, asigna sonidos por nombre y da informes.
    public static class AudioCatalogTools
    {
        [MenuItem("Archaeo/Audio/1. Crear o completar eventos de sonido")]
        public static void CreateOrUpdate()
        {
            string root = AudioEditorPaths.Root;
            foreach (string sub in new[] { "", "/Eventos", "/Musica", "/SFX", "/UI", "/Ambiente" })
                AudioEditorPaths.EnsureFolder(root + sub);

            // ── Eventos de sonido ──
            var existingEvents = AudioEditorPaths.LoadAll<SoundEvent>()
                .Where(e => !string.IsNullOrEmpty(e.eventId))
                .GroupBy(e => e.eventId)
                .ToDictionary(g => g.Key, g => g.First());

            int createdEvents = 0;
            foreach (var def in AudioCatalog.Events)
            {
                if (!existingEvents.TryGetValue(def.Id, out SoundEvent ev))
                {
                    ev = ScriptableObject.CreateInstance<SoundEvent>();
                    ev.eventId = def.Id;
                    ev.bus = def.Bus;
                    ev.volumeDb = def.VolumeDb;
                    ev.cooldownSeconds = def.Cooldown;
                    ev.maxInstances = def.MaxInstances;
                    ev.loop = def.Loop;
                    ev.playInBackground = def.PlayInBackground;
                    ev.duckMusicDb = def.DuckMusicDb;
                    ev.duckSeconds = def.DuckSeconds;
                    ev.priority = def.Priority;
                    ev.fadeInSeconds = def.Loop ? 0.2f : 0f;
                    ev.fadeOutSeconds = def.Loop ? 0.4f : 0f;

                    AssetDatabase.CreateAsset(ev, AudioEditorPaths.Events + "/SFX_" + def.Id + ".asset");
                    createdEvents++;
                }

                ev.description = def.Description;   // la descripción siempre se mantiene al día con el código
                EditorUtility.SetDirty(ev);
            }

            // ── Pistas de música ──
            var existingTracks = AudioEditorPaths.LoadAll<MusicTrack>()
                .Where(t => !string.IsNullOrEmpty(t.trackId))
                .GroupBy(t => t.trackId)
                .ToDictionary(g => g.Key, g => g.First());

            int createdTracks = 0;
            foreach (var def in AudioCatalog.Tracks)
            {
                if (!existingTracks.TryGetValue(def.Id, out MusicTrack track))
                {
                    track = ScriptableObject.CreateInstance<MusicTrack>();
                    track.trackId = def.Id;
                    AssetDatabase.CreateAsset(track, root + "/Musica/MUS_" + def.Id + ".asset");
                    createdTracks++;
                }

                track.description = def.Description;
                EditorUtility.SetDirty(track);
            }

            AssetDatabase.SaveAssets();

            // ── Biblioteca ──
            AudioLibrary library = AudioEditorPaths.LoadAll<AudioLibrary>().FirstOrDefault();
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(library, root + "/AudioLibrary.asset");
            }

            library.events = AudioEditorPaths.LoadAll<SoundEvent>().OrderBy(e => e.eventId).ToArray();
            library.tracks = AudioEditorPaths.LoadAll<MusicTrack>().OrderBy(t => t.trackId).ToArray();
            EditorUtility.SetDirty(library);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int assigned = AudioAutoAssign.Run(false);

            Debug.Log($"[Audio] Listo. Eventos nuevos: {createdEvents}, pistas nuevas: {createdTracks}, " +
                      $"sonidos asignados por nombre: {assigned}. Biblioteca: {library.events.Length} eventos y " +
                      $"{library.tracks.Length} pistas ({AssetDatabase.GetAssetPath(library)}).", library);
        }

        [MenuItem("Archaeo/Audio/2. Asignar sonidos por nombre de archivo")]
        static void AssignByName()
        {
            int assigned = AudioAutoAssign.Run(true);
            Debug.Log($"[Audio] Sonidos asignados por nombre: {assigned}.");
        }

        [MenuItem("Archaeo/Audio/3. Informe de cobertura")]
        static void Coverage()
        {
            var events = AudioEditorPaths.LoadAll<SoundEvent>();
            var tracks = AudioEditorPaths.LoadAll<MusicTrack>();

            var missingEvents = events.Where(e => !e.HasClips).Select(e => e.eventId).OrderBy(s => s).ToList();
            var missingTracks = tracks.Where(t => !t.HasAudio).Select(t => t.trackId).OrderBy(s => s).ToList();

            string message =
                $"[Audio] Eventos con sonido: {events.Count - missingEvents.Count}/{events.Count}.  " +
                $"Pistas con audio: {tracks.Count - missingTracks.Count}/{tracks.Count}.";

            if (missingEvents.Count > 0)
                message += "\nEventos sin sonido:\n  - " + string.Join("\n  - ", missingEvents);
            if (missingTracks.Count > 0)
                message += "\nPistas sin audio:\n  - " + string.Join("\n  - ", missingTracks);

            Debug.Log(message);
        }

        [MenuItem("Archaeo/Audio/4. Comprobar el catálogo")]
        static void CheckCatalog()
        {
            var ids = AudioCatalog.Events.Select(e => e.Id).Concat(AudioCatalog.Tracks.Select(t => t.Id)).ToList();
            var duplicated = ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (duplicated.Count > 0) Debug.LogError("[Audio] Ids duplicados en el catálogo: " + string.Join(", ", duplicated));
            else Debug.Log($"[Audio] Catálogo correcto: {ids.Count} ids, sin duplicados.");
        }
    }

    /// Asigna los archivos de audio a su evento según el nombre del archivo.
    ///   UI_Click_01.wav, UI_Click_02.wav -> evento UI_Click
    ///   Music_Excavation.wav (bucle) y Music_Excavation_Intro.wav (intro) -> pista Music_Excavation
    /// Solo AÑADE: nunca quita sonidos que alguien haya puesto a mano.
    public static class AudioAutoAssign
    {
        public static int Run(bool verbose)
        {
            // Los ids más largos primero: así "Artifact_New_Rare_01" va a Artifact_New_Rare y no a Artifact_New
            var events = AudioEditorPaths.LoadAll<SoundEvent>()
                .Where(e => !string.IsNullOrEmpty(e.eventId))
                .OrderByDescending(e => e.eventId.Length).ToList();
            var tracks = AudioEditorPaths.LoadAll<MusicTrack>()
                .Where(t => !string.IsNullOrEmpty(t.trackId))
                .OrderByDescending(t => t.trackId.Length).ToList();

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioEditorPaths.Root });
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();

            int added = 0;

            foreach (string path in paths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                string name = Path.GetFileNameWithoutExtension(path);

                // Música
                MusicTrack track = tracks.FirstOrDefault(t =>
                    name == t.trackId || name == t.trackId + "_Loop" || name == t.trackId + "_Intro");
                if (track != null)
                {
                    bool isIntro = name.EndsWith("_Intro");
                    if (isIntro && track.introClip != clip) { track.introClip = clip; added++; EditorUtility.SetDirty(track); }
                    if (!isIntro && track.loopClip != clip) { track.loopClip = clip; added++; EditorUtility.SetDirty(track); }
                    continue;
                }

                // Efectos
                SoundEvent ev = events.FirstOrDefault(e => name == e.eventId || name.StartsWith(e.eventId + "_"));
                if (ev == null) continue;

                var list = ev.clips != null ? ev.clips.ToList() : new List<AudioClip>();
                if (list.Contains(clip)) continue;

                list.Add(clip);
                ev.clips = list.ToArray();
                added++;
                EditorUtility.SetDirty(ev);

                if (verbose) Debug.Log($"[Audio] '{name}' -> {ev.eventId}", ev);
            }

            if (added > 0) AssetDatabase.SaveAssets();
            return added;
        }
    }
}