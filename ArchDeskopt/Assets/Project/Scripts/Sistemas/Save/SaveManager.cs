using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Archaeo.Data;
using Archaeo.Discovery;
using Archaeo.Economy;
using Archaeo.Excavation;

namespace Archaeo.Save
{
    /// Guarda y carga la partida. Colócalo en un objeto de la escena Boot.
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Guardado")]
        [SerializeField] float autosaveSeconds = 30f;

        [Header("Ingresos con el juego cerrado")]
        [Tooltip("Horas máximas que cuentan mientras el juego estuvo cerrado. 0 = desactivado.")]
        [SerializeField] float maxOfflineHours = 8f;

        const string FileName = "save.json";
        const int CurrentVersion = 1;

        string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        DiscoveryManager discovery;
        EconomyManager economy;
        ExcavationSystem excavation;

        bool loaded;            // hasta que no se carga no se guarda, para no pisar el guardado con datos vacíos
        bool saveRequested;
        float lastSaveTime = -999f;
        float nextAutosave;

        /// (monedas ganadas, segundos de ausencia)
        public event Action<double, double> OfflineIncomeEarned;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        IEnumerator Start()
        {
            if (Instance != this) yield break;

            // Esperar un frame a que el resto de sistemas haya hecho su Start
            yield return null;

            discovery = DiscoveryManager.Instance;
            economy = EconomyManager.Instance;
            excavation = ExcavationSystem.Instance;

            Debug.Log($"[Guardado] Archivo: {SavePath}");
            Load();
            loaded = true;

            if (discovery != null)
            {
                discovery.ArtifactFound += (a, isNew, level) => RequestSave();
                discovery.RestorationCompleted += a => RequestSave();
            }

            nextAutosave = Time.realtimeSinceStartup + autosaveSeconds;
        }

        void Update()
        {
            if (!loaded) return;

            float now = Time.realtimeSinceStartup;
            bool requestedAndReady = saveRequested && now - lastSaveTime >= 1f;   // como mucho 1 por segundo

            if (requestedAndReady || now >= nextAutosave) Save();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit() => Save();

        /// Pide un guardado en cuanto sea posible (no más de uno por segundo).
        public void RequestSave() => saveRequested = true;

        // ───────────── Guardar ─────────────
        public void Save()
        {
            if (!loaded) return;

            try
            {
                string json = JsonUtility.ToJson(Capture(), true);

                // Se escribe en un temporal y se intercambia, dejando copia .bak
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, SavePath + ".bak");
                else File.Move(temp, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Guardado] No se pudo guardar: {e.Message}");
            }

            saveRequested = false;
            lastSaveTime = Time.realtimeSinceStartup;
            nextAutosave = lastSaveTime + autosaveSeconds;
        }

        SaveData Capture()
        {
            var data = new SaveData
            {
                version = CurrentVersion,
                savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            if (economy != null) data.coins = economy.Coins;

            if (excavation != null)
            {
                data.excavationPoints = excavation.Progress.Points;
                data.totalFinds = excavation.Progress.TotalFinds;
            }

            if (discovery != null)
            {
                foreach (var e in discovery.Collection.Entries)
                {
                    data.artifacts.Add(new ArtifactSave
                    {
                        id = e.Artifact.id,
                        level = e.Level,
                        restored = e.Restored
                    });
                }

                foreach (var a in discovery.PendingList)
                    data.pending.Add(a.id);
            }

            return data;
        }

        // ───────────── Cargar ─────────────
        public bool Load()
        {
            SaveData data = ReadFile(SavePath) ?? ReadFile(SavePath + ".bak");
            if (data == null)
            {
                Debug.Log("[Guardado] No hay guardado: partida nueva.");
                return false;
            }

            Apply(data);
            Debug.Log($"[Guardado] Partida cargada: {data.artifacts.Count} artefactos, {data.coins:0} monedas.");
            return true;
        }

        SaveData ReadFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;

                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.version <= 0) return null;

                if (data.version > CurrentVersion)
                {
                    Debug.LogWarning($"[Guardado] {Path.GetFileName(path)} es de una versión más nueva del juego ({data.version}). Se ignora.");
                    return null;
                }

                // Aquí irían las migraciones cuando CurrentVersion suba
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Guardado] No se pudo leer {Path.GetFileName(path)}: {e.Message}");
                return null;
            }
        }

        void Apply(SaveData data)
        {
            if (excavation != null)
                excavation.Progress.SetState(data.excavationPoints, data.totalFinds);

            if (discovery != null)
            {
                ArtifactDatabase db = discovery.Database;
                if (db == null)
                    Debug.LogWarning("[Guardado] DiscoveryManager no tiene ArtifactDatabase: no se puede cargar la colección.");

                var entries = new List<ArtifactCollection.Entry>();
                var byId = new Dictionary<string, ArtifactData>();

                foreach (var s in data.artifacts)
                {
                    if (db != null && db.TryGet(s.id, out ArtifactData artifact))
                    {
                        byId[s.id] = artifact;
                        entries.Add(new ArtifactCollection.Entry
                        {
                            Artifact = artifact,
                            Level = Mathf.Max(1, s.level),
                            Restored = s.restored
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"[Guardado] Artefacto desconocido en el guardado: '{s.id}'. Se ignora.");
                    }
                }

                discovery.Collection.ReplaceAll(entries);

                // Pendientes: los guardados en orden, más cualquier no restaurado que faltara
                var pending = new List<ArtifactData>();
                foreach (var id in data.pending)
                    if (byId.TryGetValue(id, out ArtifactData a) && !pending.Contains(a)) pending.Add(a);
                foreach (var entry in entries)
                    if (!entry.Restored && !pending.Contains(entry.Artifact)) pending.Add(entry.Artifact);

                discovery.RestorePending(pending);
            }

            if (economy != null)
            {
                economy.SetCoins(data.coins);
                ApplyOfflineIncome(data);
            }
        }

        void ApplyOfflineIncome(SaveData data)
        {
            if (maxOfflineHours <= 0f || data.savedAtUnix <= 0) return;

            double away = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - data.savedAtUnix;
            if (away < 5) return;   // reinicios rápidos: no cuenta

            away = Math.Min(away, maxOfflineHours * 3600.0);
            double earned = economy.IncomePerSecond * away;
            if (earned < 1) return;

            economy.AddCoins(earned);
            Debug.Log($"[Guardado] Ingresos mientras estabas fuera ({away / 60:0.#} min): +{earned:0} monedas");
            OfflineIncomeEarned?.Invoke(earned, away);
        }

        // ───────────── Utilidades ─────────────
        /// Úsalo con el juego PARADO (menú de los tres puntos del componente).
        [ContextMenu("Borrar guardado")]
        void DeleteSave()
        {
            foreach (string path in new[] { SavePath, SavePath + ".bak", SavePath + ".tmp" })
                if (File.Exists(path)) File.Delete(path);

            Debug.Log("[Guardado] Guardado borrado.");
        }
    }
}