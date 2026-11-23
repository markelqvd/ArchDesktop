#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Archaeo.Data;
using Archaeo.UI;

namespace Archaeo.EditorTools
{
    /// Crea (o actualiza) el catálogo del juego:
    ///   - los 20 objetos con su sprite y su rareza,
    ///   - las mejoras de la tienda (pestaña Mejoras y pestaña Mapa),
    ///   - y rellena las 8 capas del mapa en la escena abierta.
    /// Es seguro repetirlo: lo que ya existe se actualiza sin duplicar.
    public static class CatalogGenerator
    {
        const string Root = "Assets/Project/Data";
        const string ItemsDir = Root + "/Items";
        const string ZonesDir = Root + "/Zona";
        const string ArtDir = "Assets/Project/Arte";
        const string UpgradesDir = "Assets/Resources/Upgrades";

        struct Def
        {
            public string file, name; public EconomyRarity rarity;
            public Def(string f, string n, EconomyRarity r) { file = f; name = n; rarity = r; }
        }

        static readonly EconomyRarity C = EconomyRarity.Common, U = EconomyRarity.Uncommon,
            R = EconomyRarity.Rare, E = EconomyRarity.Epic, L = EconomyRarity.Legendary;

        // Cambia aquí la rareza de cada objeto y vuelve a ejecutar el menú.
        static readonly Def[] Items =
        {
            new Def("01_anfora_vino", "Ánfora de vino", C),
            new Def("02_lampara_aceite", "Lámpara de aceite", C),
            new Def("03_plato_ceramica", "Plato de cerámica", C),
            new Def("04_espejo_bronce", "Espejo de bronce", U),
            new Def("05_hoz_hierro", "Hoz de hierro", C),
            new Def("06_kylix", "Kylix", U),
            new Def("07_cuenco_ceramica", "Cuenco de cerámica", C),
            new Def("08_dracma_plata", "Dracma de plata", U),
            new Def("09_estatuilla_atenea", "Estatuilla de Atenea", R),
            new Def("10_lira", "Lira", R),
            new Def("11_lekythos", "Lekythos", C),
            new Def("12_fibula_bronce", "Fíbula de bronce", U),
            new Def("13_placa_votiva", "Placa votiva", U),
            new Def("14_fragmento_mosaico", "Fragmento de mosaico", R),
            new Def("15_casco_corintio", "Casco corintio", E),
            new Def("16_espada_xiphos", "Espada xiphos", R),
            new Def("17_escudo_hoplita", "Escudo hoplita", E),
            new Def("18_collar_oro", "Collar de oro", E),
            new Def("19_mascara_funeraria_oro", "Máscara funeraria de oro", L),
            new Def("20_corona_laurel_oro", "Corona de laurel de oro", L),
        };

        struct Skill
        {
            public string name, desc; public UpgradeType type; public int max, cost; public float growth;
            public Skill(string n, UpgradeType t, int m, int c, float g, string d) { name = n; type = t; max = m; cost = c; growth = g; desc = d; }
        }

        static readonly Skill[] Skills =
        {
            new Skill("Guantes de trabajo", UpgradeType.Gloves, 1, 300, 1f, "Cada clic cuenta por 2"),
            new Skill("Teclado del escriba", UpgradeType.Keyboard, 1, 300, 1f, "Cada tecla cuenta por 2"),
            new Skill("Pico reforzado", UpgradeType.Pickaxe, 6, 800, 1.9f, "+20 % de progreso por interacción y nivel"),
            new Skill("Modo concentración", UpgradeType.Focus, 1, 200, 1f, "El aviso del QTE espera 60 s en vez de 20 s"),
            new Skill("Mapa del tesoro", UpgradeType.TreasureMap, 1, 1500, 1f, "El primer QTE de cada sesión sale al abrir el juego"),
            new Skill("Excavadora automática", UpgradeType.AutoDig, 5, 4000, 2.6f, "+1 interacción por segundo y nivel, aunque no toques nada"),
            new Skill("Pulso firme", UpgradeType.Pulse, 5, 1000, 2.2f, "Zonas de acierto un 10 % más grandes por nivel"),
            new Skill("Reloj de arena", UpgradeType.Hourglass, 4, 2000, 2.5f, "+0,5 s en cada QTE por nivel"),
            new Skill("Instinto", UpgradeType.Instinct, 3, 6000, 3f, "10 % por nivel de que salga la versión fácil del QTE"),
            new Skill("Cadena de aciertos", UpgradeType.Chain, 1, 15000, 1f, "Cada Perfecto seguido suma +10 % al bono de monedas (máx. 10)"),
            new Skill("Ojo experto", UpgradeType.Expert, 1, 40000, 1f, "Un Perfecto da 1 copia extra del objeto"),
            new Skill("Vitrinas iluminadas", UpgradeType.Showcase, 10, 1000, 1.7f, "+10 % de ingresos globales por nivel"),
            new Skill("Folleto del museo", UpgradeType.Pamphlet, 1, 12000, 1f, "+1 % de ingresos por cada objeto distinto expuesto"),
            new Skill("Lupa", UpgradeType.Lupa, 5, 2500, 2.4f, "+10 % de probabilidad relativa de Raro, Épico y Legendario por nivel"),
            new Skill("Detector de metales", UpgradeType.Detector, 1, 3000, 1f, "Antes del QTE muestra la rareza del objeto en juego"),
            new Skill("Trébol fósil", UpgradeType.Clover, 5, 8000, 2.3f, "5 % por nivel de obtener 2 objetos en un QTE"),
        };

        // Mejoras de la pestaña Mapa: cada una cambia el mapa a la capa siguiente
        static readonly string[] MapNames =
        {
            "Delimitar el terreno", "Descubrir los cimientos", "Excavar la zanja de acceso", "Ampliar la excavación",
            "Abrir la trinchera principal", "Segunda terraza", "Zona de hallazgos protegida",
        };
        static readonly int[] MapCosts = { 150, 600, 2000, 6000, 18000, 50000, 150000 };

        static readonly string[] RarityNames = { "Común", "Poco común", "Raro", "Épico", "Legendario" };

        // ───────────────────────── Generar ─────────────────────────
        [MenuItem("Archaeo/Economía/Generar catálogo (20 objetos + mejoras)")]
        public static void Generate()
        {
            var cfg = FindFirst<EconomyConfig>();
            if (cfg == null) { Debug.LogError("[Catálogo] No encuentro ningún EconomyConfig. Créalo primero."); return; }

            EnsureFolder(ItemsDir); EnsureFolder(ZonesDir); EnsureFolder(UpgradesDir);

            // Rarezas: probabilidad por clase y nivel de mapa necesario
            float[] chances = { 60, 25, 10, 4, 1 };
            for (int i = 0; i < 5 && i < cfg.rarities.Length; i++) cfg.rarities[i].chancePercent = chances[i];
            cfg.rarityUnlockMapLevel = new[] { 0, 1, 3, 5, 7 };
            EditorUtility.SetDirty(cfg);

            var oldRarities = LoadOldRarities();

            // Zona única
            var zone = AssetDatabase.LoadAssetAtPath<ZoneData>(ZonesDir + "/Zone_Local.asset");
            if (zone == null) zone = GetOrCreate<ZoneData>(ZonesDir + "/Zone_Local.asset");
            zone.id = "zone_local";
            zone.displayName = "Yacimiento";
            zone.unlockCost = 0;
            zone.items = new List<ArtifactData>();

            // El primer objeto reutiliza el asset antiguo (conserva su referencia en la escena Boot)
            string reuse = FindAssetPath("Item_Z1_1_", "ArtifactData");

            var all = new List<ArtifactData>();
            for (int i = 0; i < Items.Length; i++)
            {
                var def = Items[i];
                string assetName = "Item_" + def.file;
                string path = $"{ItemsDir}/{assetName}.asset";

                if (i == 0 && reuse != null && !File.Exists(path))
                {
                    string err = AssetDatabase.RenameAsset(reuse, assetName);
                    if (!string.IsNullOrEmpty(err)) Debug.LogWarning("[Catálogo] No se pudo renombrar el primer objeto: " + err);
                }

                var item = GetOrCreate<ArtifactData>(path);
                item.id = "g_" + def.file.Substring(3);
                item.displayName = def.name;
                item.era = "Grecia antigua";
                item.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{def.file}.png");
                if (item.sprite == null) Debug.LogWarning($"[Catálogo] Falta el sprite {def.file}.png", item);
                item.economyRarity = def.rarity;
                item.zoneIndex = 0;
                item.zone = zone;
                item.rarity = oldRarities[(int)def.rarity];
                item.baseIncomePerMinute = cfg.zoneBase[0] * cfg.rarities[(int)def.rarity].multiplier;
                item.levelIncomes = new float[0];
                item.restorationType = RestorationType.Clean;
                EditorUtility.SetDirty(item);

                zone.items.Add(item);
                all.Add(item);
            }
            EditorUtility.SetDirty(zone);

            // Base de datos maestra
            var db = FindFirst<ArtifactDatabase>();
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<ArtifactDatabase>();
                AssetDatabase.CreateAsset(db, Root + "/ArtifactDatabase.asset");
            }
            var so = new SerializedObject(db);
            var list = so.FindProperty("artifacts");
            list.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);

            DeleteOldGenerated(all);
            GenerateUpgrades(cfg);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Catálogo] Listo: {all.Count} objetos, {Skills.Length} mejoras y {MapNames.Length} mejoras de mapa. Ahora ejecuta 'Comprobar catálogo'.");
        }

        static void GenerateUpgrades(EconomyConfig cfg)
        {
            int order = 0;
            foreach (var s in Skills)
            {
                var u = GetOrCreate<UpgradeData>($"{UpgradesDir}/Upg_{Slug(s.name)}.asset");
                u.id = "skill_" + Slug(s.name);
                u.displayName = s.name;
                u.type = s.type;
                u.description = s.desc;
                u.startLevel = 0;
                u.maxLevel = s.max;
                u.baseCost = s.cost;
                u.costGrowth = s.growth;
                u.requires = null;
                u.order = order++;
                EditorUtility.SetDirty(u);
            }

            UpgradeData previous = null;
            for (int i = 0; i < MapNames.Length; i++)
            {
                var u = GetOrCreate<UpgradeData>($"{UpgradesDir}/Map_{i + 1}_{Slug(MapNames[i])}.asset");
                u.id = $"map_{i + 1}";
                u.displayName = MapNames[i];
                u.type = UpgradeType.Map;
                u.startLevel = 0;
                u.maxLevel = 1;
                u.baseCost = MapCosts[i];
                u.costGrowth = 1f;
                u.order = i;
                u.requires = previous;

                string unlock = "";
                for (int r = 0; r < cfg.rarityUnlockMapLevel.Length; r++)
                    if (cfg.rarityUnlockMapLevel[r] == i + 1)
                        unlock += (unlock.Length > 0 ? " y " : "") + RarityNames[r];
                u.description = "Cambia el mapa." + (unlock.Length > 0 ? $" Empiezan a salir objetos {unlock}." : "");

                EditorUtility.SetDirty(u);
                previous = u;
            }
        }

        static void DeleteOldGenerated(List<ArtifactData> keep)
        {
            var keepPaths = new HashSet<string>();
            foreach (var k in keep) keepPaths.Add(AssetDatabase.GetAssetPath(k));

            int deleted = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:ArtifactData", new[] { ItemsDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileName(path);
                if (file.StartsWith("Item_Z") && !keepPaths.Contains(path) && AssetDatabase.DeleteAsset(path)) deleted++;
            }
            foreach (var guid in AssetDatabase.FindAssets("t:ZoneData", new[] { ZonesDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileName(path);
                if (file.StartsWith("Zone_") && file.Contains("_zone_") && AssetDatabase.DeleteAsset(path)) deleted++;
            }
            if (deleted > 0) Debug.Log($"[Catálogo] Borrados {deleted} assets de la versión anterior (36 objetos / 6 zonas).");
        }

        // ───────────────────────── Comprobar ─────────────────────────
        [MenuItem("Archaeo/Economía/Comprobar catálogo")]
        public static void Verify()
        {
            var cfg = FindFirst<EconomyConfig>();
            var db = FindFirst<ArtifactDatabase>();
            if (cfg == null || db == null) { Debug.LogError("[Catálogo] Falta EconomyConfig o ArtifactDatabase."); return; }

            var counts = new int[5];
            int noSprite = 0;
            foreach (var a in db.All)
            {
                if (a == null) continue;
                counts[(int)a.economyRarity]++;
                if (a.sprite == null) noSprite++;
            }

            Debug.Log($"[Catálogo] Objetos: {db.All.Count} · Común {counts[0]}, Poco común {counts[1]}, Raro {counts[2]}, Épico {counts[3]}, Legendario {counts[4]}");
            Debug.Log(noSprite == 0 ? "OK  [Catálogo] Todos los objetos tienen sprite" : $"MAL [Catálogo] {noSprite} objetos sin sprite");

            var ups = Resources.LoadAll<UpgradeData>("Upgrades");
            int maps = 0, skills = 0;
            foreach (var u in ups) { if (u.type == UpgradeType.Map) maps++; else skills++; }
            Debug.Log($"[Catálogo] Mejoras: {skills} de la pestaña Mejoras y {maps} de la pestaña Mapa");

            for (int r = 0; r < 5; r++)
                Debug.Log($"[Catálogo] {RarityNames[r]}: empieza a salir con {cfg.rarityUnlockMapLevel[r]} mejoras de mapa ({cfg.rarities[r].chancePercent} %)");
        }

        // ───────────────────────── Mapa ─────────────────────────
        static readonly string[] LayerFiles =
        {
            "Zona_1_Capa_1", "Zona_1_Capa_2", "Zona1_capa3", "Zona1_capa4",
            "Zona1_capa5", "Zona1_capa6", "Zona1_capa7", "Zona1_capa8",
        };

        [MenuItem("Archaeo/Mapa/Rellenar las 8 capas (escena abierta)")]
        public static void FillMapLayers()
        {
            var ctrl = Object.FindFirstObjectByType<MapLayerController>();
            if (ctrl == null) { Debug.LogError("[Mapa] No hay ningún MapLayerController en la escena abierta. Abre la escena Main."); return; }

            var so = new SerializedObject(ctrl);
            var layers = so.FindProperty("layers");

            // Objetos que ya se veían por capas: los de la capa 1 se quedan ahí; los demás se ven desde la capa 2 en adelante
            var first = new List<Object>();
            var rest = new List<Object>();
            for (int i = 0; i < layers.arraySize; i++)
            {
                var objs = layers.GetArrayElementAtIndex(i).FindPropertyRelative("onlyInThisLayer");
                for (int k = 0; k < objs.arraySize; k++)
                {
                    var o = objs.GetArrayElementAtIndex(k).objectReferenceValue;
                    if (o == null) continue;
                    var target = i == 0 ? first : rest;
                    if (!target.Contains(o)) target.Add(o);
                }
            }

            layers.arraySize = LayerFiles.Length;
            for (int i = 0; i < LayerFiles.Length; i++)
            {
                var el = layers.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("name").stringValue = $"Capa {i + 1}";
                el.FindPropertyRelative("sprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{LayerFiles[i]}.png");
                el.FindPropertyRelative("minRestoredObjects").intValue = i;

                var objs = el.FindPropertyRelative("onlyInThisLayer");
                var list = i == 0 ? first : rest;
                objs.arraySize = list.Count;
                for (int k = 0; k < list.Count; k++) objs.GetArrayElementAtIndex(k).objectReferenceValue = list[k];
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(ctrl.gameObject.scene);
            Debug.Log("[Mapa] 8 capas asignadas. Guarda la escena (Ctrl+S).");
        }

        // ───────────────────────── Utilidades ─────────────────────────
        static RarityData[] LoadOldRarities()
        {
            // Común, Poco común, Raro, Épico (era "Muy Raro"), Legendario (era "Reliquia")
            string[] files = { "Comun", "Poco Comun", "Raro", "Muy Raro", "Reliquia" };
            var result = new RarityData[5];
            for (int i = 0; i < 5; i++)
            {
                result[i] = AssetDatabase.LoadAssetAtPath<RarityData>($"{Root}/Rarezas/{files[i]}.asset");
                if (result[i] != null) { result[i].displayName = RarityNames[i]; EditorUtility.SetDirty(result[i]); }
                else Debug.LogWarning($"[Catálogo] No encuentro la rareza antigua '{files[i]}'; el objeto quedará sin RarityData.");
            }
            return result;
        }

        static string FindAssetPath(string namePrefix, string type)
        {
            foreach (var guid in AssetDatabase.FindAssets($"{namePrefix} t:{type}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).StartsWith(namePrefix)) return path;
            }
            return null;
        }

        static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static T FindFirst<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }

        static string Slug(string s)
        {
            s = s.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (char ch in s)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            }
            return sb.ToString().Trim('_').Replace("__", "_");
        }
    }
}
#endif
