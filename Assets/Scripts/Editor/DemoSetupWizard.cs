using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemy;
using TowerDefense.Pooling;
using TowerDefense.Tower;
using TowerDefense.Projectile;
using TowerDefense.UI;
using UnityEditor.Events;
using UnityEngine.Events;

namespace TowerDefense.Editor
{
    public class DemoSetupWizard : EditorWindow
    {
        private const string ENEMY_LAYER = "Enemy";

        [MenuItem("Tower Defense/Setup Playable Demo")]
        public static void ShowWindow()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Setup Error",
                    "You cannot setup the playable demo while the editor is in Play Mode. Please exit Play Mode and try again.",
                    "OK");
                return;
            }

            if (EditorUtility.DisplayDialog(
                "Setup Playable Demo",
                "This script will generate basic placeholder prefabs, create test ScriptableObjects, " +
                "and set up a fully playable demo scene (Level1.unity) linking all managers and UI components.\n\n" +
                "Do you want to proceed?",
                "Yes, Setup Demo",
                "Cancel"))
            {
                SetupDemo();
            }
        }

        [MenuItem("Tower Defense/Setup LevelDemo Scene")]
        public static void ShowLevelDemoWindow()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Setup Error",
                    "You cannot setup the LevelDemo scene while the editor is in Play Mode. Please exit Play Mode and try again.",
                    "OK");
                return;
            }

            if (EditorUtility.DisplayDialog(
                "Setup LevelDemo Scene",
                "This script will generate a new interactive test scene (LevelDemo.unity) with buttons to spawn enemies and place towers dynamically.\n\n" +
                "Do you want to proceed?",
                "Yes, Setup LevelDemo",
                "Cancel"))
            {
                SetupLevelDemo();
            }
        }

        private static void SetupDemo()
        {
            // Ensure folders exist
            EnsureFolderExists("Assets/Prefabs");
            EnsureFolderExists("Assets/ScriptableObjects");
            EnsureFolderExists("Assets/Scenes");

            // Setup Project tags and layers
            CreateLayer(ENEMY_LAYER);
            CreateTag("Enemy");

            // Load extra built-in sprites for quick placeholders
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // 1. Create Projectile Prefab
            GameObject projTemp = new GameObject("ProjectileTemp");
            SpriteRenderer projSR = projTemp.AddComponent<SpriteRenderer>();
            projSR.sprite = knobSprite;
            projSR.color = Color.yellow;
            projTemp.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            ProjectileController projCtrl = projTemp.AddComponent<ProjectileController>();
            
            GameObject projectilePrefab = PrefabUtility.SaveAsPrefabAsset(projTemp, "Assets/Prefabs/Projectile.prefab");
            DestroyImmediate(projTemp);
            Debug.Log("[Setup] Created Projectile Prefab.");

            // 2. Create 4 Enemy Prefabs
            GameObject enemyBasicPrefab = CreateEnemyPrefab("Enemy_Basic", typeof(BasicEnemy), Color.white, Vector3.one, knobSprite);
            GameObject enemyFastPrefab = CreateEnemyPrefab("Enemy_Fast", typeof(FastEnemy), Color.yellow, new Vector3(0.8f, 0.8f, 1f), knobSprite);
            GameObject enemyTankPrefab = CreateEnemyPrefab("Enemy_Tank", typeof(TankEnemy), new Color(0.8f, 0.2f, 0.2f), new Vector3(1.6f, 1.6f, 1f), knobSprite);
            GameObject enemyArmorPrefab = CreateEnemyPrefab("Enemy_Armor", typeof(ArmorEnemy), new Color(0.5f, 0.6f, 0.7f), new Vector3(1.2f, 1.2f, 1f), knobSprite);

            // Legacy fallback Enemy prefab
            if (!System.IO.File.Exists("Assets/Prefabs/Enemy.prefab"))
            {
                FileUtil.CopyFileOrDirectory("Assets/Prefabs/Enemy_Basic.prefab", "Assets/Prefabs/Enemy.prefab");
            }
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");

            // 3. Create ScriptableObjects
            // Create 4 EnemyData configuration assets
            CreateEnemyDataAsset("TestEnemyData_Basic", EnemyType.Basic, 10);
            CreateEnemyDataAsset("TestEnemyData_Fast", EnemyType.Fast, 15);
            CreateEnemyDataAsset("TestEnemyData_Tank", EnemyType.Tank, 30);
            CreateEnemyDataAsset("TestEnemyData_Armor", EnemyType.Armor, 20);

            // Legacy fallback TestEnemyData
            EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
            SerializedObject enemyDataSO = new SerializedObject(enemyData);
            enemyDataSO.FindProperty("enemyType").enumValueIndex = (int)EnemyType.Basic;
            enemyDataSO.FindProperty("goldReward").intValue = 10;
            enemyDataSO.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(enemyData, "Assets/ScriptableObjects/TestEnemyData.asset");

            // Create TowerData
            TowerData towerData = ScriptableObject.CreateInstance<TowerData>();
            SerializedObject towerDataSO = new SerializedObject(towerData);
            towerDataSO.FindProperty("towerName").stringValue = "Basic Tower";
            towerDataSO.FindProperty("cost").intValue = 50;
            towerDataSO.FindProperty("range").floatValue = 4.0f;
            towerDataSO.FindProperty("fireRate").floatValue = 1.5f;
            towerDataSO.FindProperty("damage").intValue = 3;
            towerDataSO.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            towerDataSO.FindProperty("projectileSpeed").floatValue = 6.0f;
            towerDataSO.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(towerData, "Assets/ScriptableObjects/TestTowerData.asset");

            // Create WaveData 1 to 4 with user example setups
            WaveData w1 = CreateWaveAsset("TestWaveData_W1", 3, 4.0f, 4, 3.0f, 2, 6.0f, 2, 5.0f);
            WaveData w2 = CreateWaveAsset("TestWaveData_W2", 4, 4.0f, 5, 3.0f, 3, 6.0f, 0, 5.0f);
            WaveData w3 = CreateWaveAsset("TestWaveData_W3", 5, 3.5f, 6, 2.5f, 4, 5.5f, 1, 4.5f);
            WaveData w4 = CreateWaveAsset("TestWaveData_W4", 6, 3.0f, 8, 2.0f, 5, 5.0f, 3, 4.0f);

            // Legacy fallback TestWaveData asset
            WaveData waveData = ScriptableObject.CreateInstance<WaveData>();
            waveData.BasicCount = 5;
            waveData.BasicSpawnInterval = 1.5f;
            EditorUtility.SetDirty(waveData);
            AssetDatabase.CreateAsset(waveData, "Assets/ScriptableObjects/TestWaveData.asset");

            // Create LevelData 1
            LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
            SerializedObject levelDataSO = new SerializedObject(levelData);
            levelDataSO.FindProperty("levelName").stringValue = "Level 1";
            levelDataSO.FindProperty("startingGold").intValue = 100;
            levelDataSO.FindProperty("baseMaxHealth").intValue = 10;
            SerializedProperty wavesProp = levelDataSO.FindProperty("waves");
            wavesProp.ClearArray();
            wavesProp.InsertArrayElementAtIndex(0); wavesProp.GetArrayElementAtIndex(0).objectReferenceValue = w1;
            wavesProp.InsertArrayElementAtIndex(1); wavesProp.GetArrayElementAtIndex(1).objectReferenceValue = w2;
            wavesProp.InsertArrayElementAtIndex(2); wavesProp.GetArrayElementAtIndex(2).objectReferenceValue = w3;
            wavesProp.InsertArrayElementAtIndex(3); wavesProp.GetArrayElementAtIndex(3).objectReferenceValue = w4;
            levelDataSO.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(levelData, "Assets/ScriptableObjects/TestLevelData.asset");

            // Create LevelData 2
            LevelData levelData2 = ScriptableObject.CreateInstance<LevelData>();
            SerializedObject levelData2SO = new SerializedObject(levelData2);
            levelData2SO.FindProperty("levelName").stringValue = "Level 2";
            levelData2SO.FindProperty("startingGold").intValue = 200;
            levelData2SO.FindProperty("baseMaxHealth").intValue = 15;
            SerializedProperty wavesProp2 = levelData2SO.FindProperty("waves");
            wavesProp2.ClearArray();
            wavesProp2.InsertArrayElementAtIndex(0); wavesProp2.GetArrayElementAtIndex(0).objectReferenceValue = w1;
            wavesProp2.InsertArrayElementAtIndex(1); wavesProp2.GetArrayElementAtIndex(1).objectReferenceValue = w2;
            wavesProp2.InsertArrayElementAtIndex(2); wavesProp2.GetArrayElementAtIndex(2).objectReferenceValue = w3;
            wavesProp2.InsertArrayElementAtIndex(3); wavesProp2.GetArrayElementAtIndex(3).objectReferenceValue = w4;
            levelData2SO.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(levelData2, "Assets/ScriptableObjects/TestLevelData2.asset");

            // Create LevelData 3
            LevelData levelData3 = ScriptableObject.CreateInstance<LevelData>();
            SerializedObject levelData3SO = new SerializedObject(levelData3);
            levelData3SO.FindProperty("levelName").stringValue = "Level 3";
            levelData3SO.FindProperty("startingGold").intValue = 300;
            levelData3SO.FindProperty("baseMaxHealth").intValue = 20;
            SerializedProperty wavesProp3 = levelData3SO.FindProperty("waves");
            wavesProp3.ClearArray();
            wavesProp3.InsertArrayElementAtIndex(0); wavesProp3.GetArrayElementAtIndex(0).objectReferenceValue = w1;
            wavesProp3.InsertArrayElementAtIndex(1); wavesProp3.GetArrayElementAtIndex(1).objectReferenceValue = w2;
            wavesProp3.InsertArrayElementAtIndex(2); wavesProp3.GetArrayElementAtIndex(2).objectReferenceValue = w3;
            wavesProp3.InsertArrayElementAtIndex(3); wavesProp3.GetArrayElementAtIndex(3).objectReferenceValue = w4;
            levelData3SO.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(levelData3, "Assets/ScriptableObjects/TestLevelData3.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Load persistent references from AssetDatabase to ensure type: 3 (Asset) serialization
            EnemyData persistentEnemyData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData.asset");
            TowerData persistentTowerData = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/ScriptableObjects/TestTowerData.asset");
            WaveData persistentWaveData = AssetDatabase.LoadAssetAtPath<WaveData>("Assets/ScriptableObjects/TestWaveData.asset");
            LevelData persistentLevelData = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/TestLevelData.asset");
            LevelData persistentLevelData2 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/TestLevelData2.asset");
            LevelData persistentLevelData3 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/TestLevelData3.asset");
            GameObject persistentEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            GameObject persistentProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");

            // Update TowerData projectile reference
            if (persistentTowerData != null && persistentProjectilePrefab != null)
            {
                SerializedObject persistentTowerDataSO = new SerializedObject(persistentTowerData);
                persistentTowerDataSO.FindProperty("projectilePrefab").objectReferenceValue = persistentProjectilePrefab;
                persistentTowerDataSO.ApplyModifiedProperties();
            }

            // Update WaveData
            if (persistentWaveData != null)
            {
                persistentWaveData.BasicCount = 5;
                persistentWaveData.BasicSpawnInterval = 1.5f;
                EditorUtility.SetDirty(persistentWaveData);
            }

            // Update LevelData wave reference
            if (persistentLevelData != null && persistentWaveData != null)
            {
                SerializedObject persistentLevelDataSO = new SerializedObject(persistentLevelData);
                SerializedProperty persistentWavesProp = persistentLevelDataSO.FindProperty("waves");
                if (persistentWavesProp.arraySize > 0)
                {
                    persistentWavesProp.GetArrayElementAtIndex(0).objectReferenceValue = persistentWaveData;
                }
                persistentLevelDataSO.ApplyModifiedProperties();
            }
            if (persistentLevelData2 != null && persistentWaveData != null)
            {
                SerializedObject persistentLevelDataSO2 = new SerializedObject(persistentLevelData2);
                SerializedProperty persistentWavesProp2 = persistentLevelDataSO2.FindProperty("waves");
                if (persistentWavesProp2.arraySize > 0)
                {
                    persistentWavesProp2.GetArrayElementAtIndex(0).objectReferenceValue = persistentWaveData;
                }
                persistentLevelDataSO2.ApplyModifiedProperties();
            }
            if (persistentLevelData3 != null && persistentWaveData != null)
            {
                SerializedObject persistentLevelDataSO3 = new SerializedObject(persistentLevelData3);
                SerializedProperty persistentWavesProp3 = persistentLevelDataSO3.FindProperty("waves");
                if (persistentWavesProp3.arraySize > 0)
                {
                    persistentWavesProp3.GetArrayElementAtIndex(0).objectReferenceValue = persistentWaveData;
                }
                persistentLevelDataSO3.ApplyModifiedProperties();
            }

            // Update EnemyPrefab with correct persistent enemyData
            if (persistentEnemyPrefab != null && persistentEnemyData != null)
            {
                GameObject loadedEnemy = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Enemy.prefab");
                SerializedObject loadedEnemySO = new SerializedObject(loadedEnemy.GetComponent<EnemyMovement>());
                loadedEnemySO.FindProperty("enemyData").objectReferenceValue = persistentEnemyData;
                loadedEnemySO.ApplyModifiedProperties();
                SerializedObject loadedEnemyHealthSO = new SerializedObject(loadedEnemy.GetComponent<EnemyHealth>());
                loadedEnemyHealthSO.FindProperty("enemyData").objectReferenceValue = persistentEnemyData;
                loadedEnemyHealthSO.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(loadedEnemy, "Assets/Prefabs/Enemy.prefab");
                PrefabUtility.UnloadPrefabContents(loadedEnemy);
            }

            // 4. Create Tower Prefab (needs reference to TestTowerData and Projectile Prefab)
            GameObject towerTemp = new GameObject("TowerTemp");
            SpriteRenderer towerSR = towerTemp.AddComponent<SpriteRenderer>();
            towerSR.sprite = uiSprite;
            towerSR.color = Color.cyan;

            // Add a CircleCollider2D so that towers can detect overlap with each other
            CircleCollider2D towerCollider = towerTemp.AddComponent<CircleCollider2D>();
            towerCollider.radius = 0.4f;
            towerCollider.isTrigger = true;

            TowerController towerCtrl = towerTemp.AddComponent<TowerController>();
            GameObject shootPointGO = new GameObject("ShootPoint");
            shootPointGO.transform.SetParent(towerTemp.transform, false);

            SerializedObject towerCtrlSO = new SerializedObject(towerCtrl);
            towerCtrlSO.FindProperty("towerData").objectReferenceValue = persistentTowerData;
            towerCtrlSO.FindProperty("shootPoint").objectReferenceValue = shootPointGO.transform;
            towerCtrlSO.FindProperty("enemyLayerMask").intValue = 1 << LayerMask.NameToLayer(ENEMY_LAYER);
            towerCtrlSO.ApplyModifiedProperties();

            GameObject towerPrefab = PrefabUtility.SaveAsPrefabAsset(towerTemp, "Assets/Prefabs/Tower.prefab");
            DestroyImmediate(towerTemp);
            Debug.Log("[Setup] Created Tower Prefab.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Reload and force re-link the Tower prefab to use persistent asset to fix type: 2 issue
            towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
            if (towerPrefab != null && persistentTowerData != null)
            {
                GameObject loadedTower = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Tower.prefab");
                TowerController loadedTowerCtrl = loadedTower.GetComponent<TowerController>();
                SerializedObject loadedTowerCtrlSO = new SerializedObject(loadedTowerCtrl);
                loadedTowerCtrlSO.FindProperty("towerData").objectReferenceValue = persistentTowerData;
                loadedTowerCtrlSO.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(loadedTower, "Assets/Prefabs/Tower.prefab");
                PrefabUtility.UnloadPrefabContents(loadedTower);

                // Reload again to ensure the variable has the latest disk content
                towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
            }

            // 5. Create Scene (Level1.unity)
            UnityEngine.SceneManagement.Scene demoScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Configure Camera
            GameObject cameraGO = GameObject.Find("Main Camera");
            if (cameraGO != null)
            {
                Camera cam = cameraGO.GetComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.backgroundColor = new Color(0.15f, 0.2f, 0.15f); // Deep forest green
                cam.clearFlags = CameraClearFlags.SolidColor;
                
                // Set camera position to Z = -10f to see gameplay elements at Z = 0f
                Vector3 pos = cameraGO.transform.position;
                pos.z = -10f;
                cameraGO.transform.position = pos;
            }

            // Create Waypoints Path
            GameObject pathGO = new GameObject("WaypointPath");
            WaypointPath pathComp = pathGO.AddComponent<WaypointPath>();
            
            GameObject wp0 = new GameObject("WP_Start"); wp0.transform.SetParent(pathGO.transform); wp0.transform.position = new Vector3(-8f, 3f, 0f);
            GameObject wp1 = new GameObject("WP_Mid1");  wp1.transform.SetParent(pathGO.transform); wp1.transform.position = new Vector3(-2f, 3f, 0f);
            GameObject wp2 = new GameObject("WP_Mid2");  wp2.transform.SetParent(pathGO.transform); wp2.transform.position = new Vector3(2f, -3f, 0f);
            GameObject wp3 = new GameObject("WP_End");   wp3.transform.SetParent(pathGO.transform); wp3.transform.position = new Vector3(8f, -3f, 0f);
            
            pathComp.PopulateFromChildren();

            // Create ObjectPooler
            GameObject poolerGO = new GameObject("ObjectPooler");
            ObjectPooler poolerComp = poolerGO.AddComponent<ObjectPooler>();
            SerializedObject poolerSO = new SerializedObject(poolerComp);
            SerializedProperty prewarmConfigsProp = poolerSO.FindProperty("prewarmConfigs");
            prewarmConfigsProp.ClearArray();
            
            GameObject basicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Basic.prefab");
            GameObject fastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Fast.prefab");
            GameObject tankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Tank.prefab");
            GameObject armorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Armor.prefab");

            prewarmConfigsProp.InsertArrayElementAtIndex(0);
            prewarmConfigsProp.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = basicPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(0).FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(1);
            prewarmConfigsProp.GetArrayElementAtIndex(1).FindPropertyRelative("prefab").objectReferenceValue = fastPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(1).FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(2);
            prewarmConfigsProp.GetArrayElementAtIndex(2).FindPropertyRelative("prefab").objectReferenceValue = tankPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(2).FindPropertyRelative("size").intValue = 5;

            prewarmConfigsProp.InsertArrayElementAtIndex(3);
            prewarmConfigsProp.GetArrayElementAtIndex(3).FindPropertyRelative("prefab").objectReferenceValue = armorPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(3).FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(4);
            prewarmConfigsProp.GetArrayElementAtIndex(4).FindPropertyRelative("prefab").objectReferenceValue = projectilePrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(4).FindPropertyRelative("size").intValue = 20;
            poolerSO.ApplyModifiedProperties();

            // Create WaveManager
            GameObject waveManagerGO = new GameObject("WaveManager");
            WaveManager waveManagerComp = waveManagerGO.AddComponent<WaveManager>();
            SerializedObject waveManagerSO = new SerializedObject(waveManagerComp);
            waveManagerSO.FindProperty("waypointPath").objectReferenceValue = pathComp;
            waveManagerSO.FindProperty("autoStartNextWave").boolValue = true;
            waveManagerSO.FindProperty("waveInterval").floatValue = 4.0f;
            waveManagerSO.FindProperty("basicEnemyPrefab").objectReferenceValue = basicPrefab;
            waveManagerSO.FindProperty("fastEnemyPrefab").objectReferenceValue = fastPrefab;
            waveManagerSO.FindProperty("tankEnemyPrefab").objectReferenceValue = tankPrefab;
            waveManagerSO.FindProperty("armorEnemyPrefab").objectReferenceValue = armorPrefab;
            waveManagerSO.FindProperty("basicEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Basic.asset");
            waveManagerSO.FindProperty("fastEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Fast.asset");
            waveManagerSO.FindProperty("tankEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Tank.asset");
            waveManagerSO.FindProperty("armorEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Armor.asset");
            waveManagerSO.ApplyModifiedProperties();

            // Create GameManager
            GameObject gameManagerGO = new GameObject("GameManager");
            GameManager gameManagerComp = gameManagerGO.AddComponent<GameManager>();
            gameManagerComp.DefaultLevelData = levelData;
            EditorUtility.SetDirty(gameManagerComp);
            Debug.Log($"[Setup] Assigning defaultLevelData. levelData is {(levelData == null ? "NULL" : "not null")}");

            // Create UI Manager GO
            GameObject uiManagerGO = new GameObject("UIManager");
            UIManager uiManagerComp = uiManagerGO.AddComponent<UIManager>();

            // Create Canvas Hierarchy
            GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create EventSystem if missing
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Create Panels
            GameObject mainMenu = CreatePanel("MainMenuPanel", canvasGO.transform, new Color(0, 0, 0, 0.9f), true);
            GameObject gameplayHUD = CreatePanel("GameplayHUDPanel", canvasGO.transform, Color.clear, false);
            GameObject pauseOverlay = CreatePanel("PauseOverlayPanel", canvasGO.transform, new Color(0, 0, 0, 0.75f), false);
            GameObject victoryOverlay = CreatePanel("VictoryOverlayPanel", canvasGO.transform, new Color(0, 0.5f, 0, 0.85f), false);
            GameObject defeatOverlay = CreatePanel("DefeatOverlayPanel", canvasGO.transform, new Color(0.5f, 0, 0, 0.85f), false);

            // Populate Main Menu
            CreateText("TitleText", mainMenu.transform, "TOWER DEFENSE GAME", new Vector2(0, 150), 48, Color.white);
            Button playBtn = CreateButton("PlayButton", mainMenu.transform, "PLAY LEVEL", new Vector2(0, -50), new Vector2(250, 60), Color.green, null);
            UnityEventTools.AddVoidPersistentListener(playBtn.onClick, new UnityAction(uiManagerComp.OnPlayButtonClicked));

            // Populate Gameplay HUD
            TextMeshProUGUI healthText = CreateText("HealthText", gameplayHUD.transform, "HP: 10/10", new Vector2(-700, 480), 28, Color.red, TextAlignmentOptions.Left);
            TextMeshProUGUI goldText = CreateText("GoldText", gameplayHUD.transform, "Gold: 100", new Vector2(0, 480), 28, Color.yellow, TextAlignmentOptions.Center);
            TextMeshProUGUI waveText = CreateText("WaveText", gameplayHUD.transform, "Wave: 1/1", new Vector2(700, 480), 28, Color.cyan, TextAlignmentOptions.Right);

            Button startWaveBtn = CreateButton("StartWaveButton", gameplayHUD.transform, "START WAVE", new Vector2(0, -480), new Vector2(220, 50), Color.green, null);
            UnityEventTools.AddVoidPersistentListener(startWaveBtn.onClick, new UnityAction(waveManagerComp.StartNextWave));

            Button pauseBtn = CreateButton("PauseButton", gameplayHUD.transform, "PAUSE", new Vector2(850, 480), new Vector2(100, 40), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(pauseBtn.onClick, new UnityAction(uiManagerComp.OnPauseButtonClicked));

            // Populate Pause Overlay
            CreateText("PauseTitleText", pauseOverlay.transform, "GAME PAUSED", new Vector2(0, 150), 38, Color.white);
            
            Button resumeBtn = CreateButton("ResumeButton", pauseOverlay.transform, "RESUME", new Vector2(0, 50), new Vector2(200, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(resumeBtn.onClick, new UnityAction(uiManagerComp.OnResumeButtonClicked));

            Button restartBtnP = CreateButton("RestartButton", pauseOverlay.transform, "RESTART", new Vector2(0, -20), new Vector2(200, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(restartBtnP.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

            Button quitBtnP = CreateButton("QuitToMenuButton", pauseOverlay.transform, "MAIN MENU", new Vector2(0, -90), new Vector2(200, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(quitBtnP.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

            // Populate Victory Overlay
            CreateText("VicTitleText", victoryOverlay.transform, "VICTORY!", new Vector2(0, 150), 48, Color.white);
            
            Button restartBtnV = CreateButton("RestartButton", victoryOverlay.transform, "RESTART LEVEL", new Vector2(0, 0), new Vector2(240, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(restartBtnV.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

            Button quitBtnV = CreateButton("QuitToMenuButton", victoryOverlay.transform, "MAIN MENU", new Vector2(0, -70), new Vector2(240, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(quitBtnV.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

            // Populate Defeat Overlay
            CreateText("DefTitleText", defeatOverlay.transform, "GAME OVER", new Vector2(0, 150), 48, Color.white);

            Button restartBtnD = CreateButton("RestartButton", defeatOverlay.transform, "TRY AGAIN", new Vector2(0, 0), new Vector2(240, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(restartBtnD.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

            Button quitBtnD = CreateButton("QuitToMenuButton", defeatOverlay.transform, "MAIN MENU", new Vector2(0, -70), new Vector2(240, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(quitBtnD.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

            // Link UIManager references
            SerializedObject uiManagerSO = new SerializedObject(uiManagerComp);
            uiManagerSO.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
            uiManagerSO.FindProperty("gameplayHUDPanel").objectReferenceValue = gameplayHUD;
            uiManagerSO.FindProperty("pauseOverlayPanel").objectReferenceValue = pauseOverlay;
            uiManagerSO.FindProperty("victoryOverlayPanel").objectReferenceValue = victoryOverlay;
            uiManagerSO.FindProperty("defeatOverlayPanel").objectReferenceValue = defeatOverlay;
            uiManagerSO.FindProperty("healthText").objectReferenceValue = healthText;
            uiManagerSO.FindProperty("goldText").objectReferenceValue = goldText;
            uiManagerSO.FindProperty("waveText").objectReferenceValue = waveText;
            uiManagerSO.ApplyModifiedProperties();

            // Set ScriptableObject levelData directly to bypass type-casting bug
            uiManagerComp.LevelDataToPlay = persistentLevelData;
            uiManagerComp.Levels = new List<LevelData> { persistentLevelData, persistentLevelData2, persistentLevelData3 };
            EditorUtility.SetDirty(uiManagerComp);
            SetupTowerPlacementUI(gameplayHUD, towerData, towerPrefab);
            Debug.Log($"[Setup] Assigning levelDataToPlay. levelData is {(levelData == null ? "NULL" : "not null")}");

            // Place Build Sites and pre-placed Towers
            GameObject buildSitePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BuildSite.prefab");
            Vector3[] sitePositions = new Vector3[]
            {
                new Vector3(-5f, 1.5f, 0f),
                new Vector3(4f, -1.5f, 0f),
                new Vector3(-2f, 1.5f, 0f),
                new Vector3(1f, -1.5f, 0f),
                new Vector3(0f, 0.5f, 0f),
                new Vector3(-5f, -0.5f, 0f),
                new Vector3(5f, 1.5f, 0f)
            };

            for (int i = 0; i < sitePositions.Length; i++)
            {
                if (buildSitePrefab != null)
                {
                    GameObject siteGO = (GameObject)PrefabUtility.InstantiatePrefab(buildSitePrefab);
                    siteGO.transform.position = sitePositions[i];
                    siteGO.name = $"BuildSite_{i + 1}";

                    BuildSite site = siteGO.GetComponent<BuildSite>();
                    
                    // First two sites have towers pre-placed
                    if (i == 0)
                    {
                        GameObject tower1 = (GameObject)PrefabUtility.InstantiatePrefab(towerPrefab);
                        tower1.transform.position = sitePositions[i];
                        tower1.name = "DefensiveTower_1";
                        if (site != null)
                        {
                            site.SetOccupied(tower1);
                        }
                    }
                    else if (i == 1)
                    {
                        GameObject tower2 = (GameObject)PrefabUtility.InstantiatePrefab(towerPrefab);
                        tower2.transform.position = sitePositions[i];
                        tower2.name = "DefensiveTower_2";
                        if (site != null)
                        {
                            site.SetOccupied(tower2);
                        }
                    }
                }
            }

            // Save Scene
            EditorSceneManager.SaveScene(demoScene, "Assets/Scenes/Level1.unity");
            Debug.Log("[Setup] Saved Scene 'Assets/Scenes/Level1.unity'.");

            // Add Scene to build settings if not already there
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool sceneExistsInBuild = false;
            foreach (var bScene in buildScenes)
            {
                if (bScene.path == "Assets/Scenes/Level1.unity")
                {
                    sceneExistsInBuild = true;
                    break;
                }
            }
            if (!sceneExistsInBuild)
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Level1.unity", true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
                Debug.Log("[Setup] Added scene to Build Settings.");
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Demo Setup Complete", "All test ScriptableObjects, Prefabs, and the 'Level1' scene have been created successfully!\n\nOpen 'Assets/Scenes/Level1' and click Play to test.", "OK");
        }

        private static void SetupLevelDemo()
        {
            EnsureFolderExists("Assets/Prefabs");
            EnsureFolderExists("Assets/ScriptableObjects");
            EnsureFolderExists("Assets/Scenes");

            // Load required prefabs and scriptable objects (load ScriptableObjects directly as their strong types)
            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            GameObject towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
            EnemyData enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData.asset");
            TowerData towerData = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/ScriptableObjects/TestTowerData.asset");
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/TestLevelData.asset");
            LevelData levelData2 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/TestLevelData2.asset");
            LevelData levelData3 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/TestLevelData3.asset");

            // If any is missing, request to run SetupDemo first
            if (projectilePrefab == null || enemyPrefab == null || towerPrefab == null || enemyData == null || towerData == null || levelData == null || levelData2 == null || levelData3 == null)
            {
                if (EditorUtility.DisplayDialog(
                    "Missing Assets",
                    "Required prefabs or ScriptableObjects are missing. We need to run the base demo setup first to generate them.\n\nDo you want to run Setup Playable Demo now?",
                    "Yes, Setup Demo",
                    "Cancel"))
                {
                    SetupDemo();

                    projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
                    enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
                    towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
                    enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData.asset");
                    towerData = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/ScriptableObjects/TestTowerData.asset");
                    levelData = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/TestLevelData.asset");
                    levelData2 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/TestLevelData2.asset");
                    levelData3 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/TestLevelData3.asset");
                }
                else
                {
                    return;
                }
            }

            Debug.Log($"[SetupLevelDemo] Loaded projectilePrefab: {(projectilePrefab == null ? "NULL" : "OK")}");
            Debug.Log($"[SetupLevelDemo] Loaded enemyPrefab: {(enemyPrefab == null ? "NULL" : "OK")}");
            Debug.Log($"[SetupLevelDemo] Loaded towerPrefab: {(towerPrefab == null ? "NULL" : "OK")}");
            Debug.Log($"[SetupLevelDemo] Loaded enemyData: {(enemyData == null ? "NULL" : "OK")}");
            Debug.Log($"[SetupLevelDemo] Loaded towerData: {(towerData == null ? "NULL" : "OK")}");
            Debug.Log($"[SetupLevelDemo] Loaded levelData: {(levelData == null ? "NULL" : "OK")}");

            // Create new scene
            UnityEngine.SceneManagement.Scene demoScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Configure Camera
            GameObject cameraGO = GameObject.Find("Main Camera");
            if (cameraGO != null)
            {
                Camera cam = cameraGO.GetComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                
                // Set camera position to Z = -10f to see gameplay elements at Z = 0f
                Vector3 pos = cameraGO.transform.position;
                pos.z = -10f;
                cameraGO.transform.position = pos;
            }

            // Create Waypoints Path
            GameObject pathGO = new GameObject("WaypointPath");
            WaypointPath pathComp = pathGO.AddComponent<WaypointPath>();
            
            GameObject wp0 = new GameObject("WP_Start"); wp0.transform.SetParent(pathGO.transform); wp0.transform.position = new Vector3(-8f, 2f, 0f);
            GameObject wp1 = new GameObject("WP_Mid1");  wp1.transform.SetParent(pathGO.transform); wp1.transform.position = new Vector3(0f, 2f, 0f);
            GameObject wp2 = new GameObject("WP_Mid2");  wp2.transform.SetParent(pathGO.transform); wp2.transform.position = new Vector3(0f, -2f, 0f);
            GameObject wp3 = new GameObject("WP_End");   wp3.transform.SetParent(pathGO.transform); wp3.transform.position = new Vector3(8f, -2f, 0f);
            
            pathComp.PopulateFromChildren();

            // Create ObjectPooler
            GameObject poolerGO = new GameObject("ObjectPooler");
            ObjectPooler poolerComp = poolerGO.AddComponent<ObjectPooler>();
            SerializedObject poolerSO = new SerializedObject(poolerComp);
            SerializedProperty prewarmConfigsProp = poolerSO.FindProperty("prewarmConfigs");
            prewarmConfigsProp.ClearArray();

            GameObject basicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Basic.prefab");
            GameObject fastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Fast.prefab");
            GameObject tankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Tank.prefab");
            GameObject armorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Armor.prefab");

            prewarmConfigsProp.InsertArrayElementAtIndex(0);
            prewarmConfigsProp.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = basicPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(0).FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(1);
            prewarmConfigsProp.GetArrayElementAtIndex(1).FindPropertyRelative("prefab").objectReferenceValue = fastPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(1).FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(2);
            prewarmConfigsProp.GetArrayElementAtIndex(2).FindPropertyRelative("prefab").objectReferenceValue = tankPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(2).FindPropertyRelative("size").intValue = 5;

            prewarmConfigsProp.InsertArrayElementAtIndex(3);
            prewarmConfigsProp.GetArrayElementAtIndex(3).FindPropertyRelative("prefab").objectReferenceValue = armorPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(3).FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(4);
            prewarmConfigsProp.GetArrayElementAtIndex(4).FindPropertyRelative("prefab").objectReferenceValue = projectilePrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(4).FindPropertyRelative("size").intValue = 20;
            poolerSO.ApplyModifiedProperties();

            // Create WaveManager
            GameObject waveManagerGO = new GameObject("WaveManager");
            WaveManager waveManagerComp = waveManagerGO.AddComponent<WaveManager>();
            SerializedObject waveManagerSO = new SerializedObject(waveManagerComp);
            waveManagerSO.FindProperty("waypointPath").objectReferenceValue = pathComp;
            waveManagerSO.FindProperty("autoStartNextWave").boolValue = false;
            waveManagerSO.FindProperty("basicEnemyPrefab").objectReferenceValue = basicPrefab;
            waveManagerSO.FindProperty("fastEnemyPrefab").objectReferenceValue = fastPrefab;
            waveManagerSO.FindProperty("tankEnemyPrefab").objectReferenceValue = tankPrefab;
            waveManagerSO.FindProperty("armorEnemyPrefab").objectReferenceValue = armorPrefab;
            waveManagerSO.FindProperty("basicEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Basic.asset");
            waveManagerSO.FindProperty("fastEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Fast.asset");
            waveManagerSO.FindProperty("tankEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Tank.asset");
            waveManagerSO.FindProperty("armorEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Armor.asset");
            waveManagerSO.ApplyModifiedProperties();

            // Create GameManager
            GameObject gameManagerGO = new GameObject("GameManager");
            GameManager gameManagerComp = gameManagerGO.AddComponent<GameManager>();
            gameManagerComp.DefaultLevelData = levelData;
            EditorUtility.SetDirty(gameManagerComp);

            // Create DemoTestController
            GameObject testControllerGO = new GameObject("DemoTestController");
            DemoTestController testControllerComp = testControllerGO.AddComponent<DemoTestController>();
            testControllerComp.EnemyPrefab = enemyPrefab;
            testControllerComp.EnemyData = enemyData;
            testControllerComp.TowerPrefab = towerPrefab;
            testControllerComp.TowerData = towerData;
            testControllerComp.WaypointPath = pathComp;
            EditorUtility.SetDirty(testControllerComp);

            // Create UI Manager GO
            GameObject uiManagerGO = new GameObject("UIManager");
            UIManager uiManagerComp = uiManagerGO.AddComponent<UIManager>();

            // Create Canvas Hierarchy
            GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create EventSystem
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Create Panels
            GameObject mainMenu = CreatePanel("MainMenuPanel", canvasGO.transform, new Color(0, 0, 0, 0.9f), true);
            GameObject gameplayHUD = CreatePanel("GameplayHUDPanel", canvasGO.transform, Color.clear, false);
            GameObject pauseOverlay = CreatePanel("PauseOverlayPanel", canvasGO.transform, new Color(0, 0, 0, 0.75f), false);
            GameObject victoryOverlay = CreatePanel("VictoryOverlayPanel", canvasGO.transform, new Color(0, 0.5f, 0, 0.85f), false);
            GameObject defeatOverlay = CreatePanel("DefeatOverlayPanel", canvasGO.transform, new Color(0.5f, 0, 0, 0.85f), false);

            // Populate Main Menu
            CreateText("TitleText", mainMenu.transform, "TOWER DEFENSE SANDBOX", new Vector2(0, 150), 48, Color.white);
            Button playBtn = CreateButton("PlayButton", mainMenu.transform, "START SANDBOX", new Vector2(0, -50), new Vector2(250, 60), Color.green, null);
            UnityEventTools.AddVoidPersistentListener(playBtn.onClick, new UnityAction(uiManagerComp.OnPlayButtonClicked));

            // Populate Gameplay HUD
            TextMeshProUGUI healthText = CreateText("HealthText", gameplayHUD.transform, "HP: 10/10", new Vector2(-700, 480), 28, Color.red, TextAlignmentOptions.Left);
            TextMeshProUGUI goldText = CreateText("GoldText", gameplayHUD.transform, "Gold: 100", new Vector2(0, 480), 28, Color.yellow, TextAlignmentOptions.Center);
            TextMeshProUGUI waveText = CreateText("WaveText", gameplayHUD.transform, "Wave: 1/1", new Vector2(700, 480), 28, Color.cyan, TextAlignmentOptions.Right);

            Button startWaveBtn = CreateButton("StartWaveButton", gameplayHUD.transform, "START WAVE", new Vector2(0, -480), new Vector2(220, 50), Color.green, null);
            UnityEventTools.AddVoidPersistentListener(startWaveBtn.onClick, new UnityAction(waveManagerComp.StartNextWave));

            Button pauseBtn = CreateButton("PauseButton", gameplayHUD.transform, "PAUSE", new Vector2(850, 480), new Vector2(100, 40), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(pauseBtn.onClick, new UnityAction(uiManagerComp.OnPauseButtonClicked));

            // Populate Sandbox Buttons (Appear on demand!)
            Button spawnEnemyBtn = CreateButton("SpawnEnemyBtn", gameplayHUD.transform, "Spawn Enemy", new Vector2(-220, -480), new Vector2(180, 50), Color.green, null);
            UnityEventTools.AddVoidPersistentListener(spawnEnemyBtn.onClick, new UnityAction(testControllerComp.SpawnEnemy));

            Button spawnTowerBtn = CreateButton("SpawnTowerBtn", gameplayHUD.transform, "Spawn Tower", new Vector2(220, -480), new Vector2(180, 50), Color.cyan, null);
            UnityEventTools.AddVoidPersistentListener(spawnTowerBtn.onClick, new UnityAction(testControllerComp.StartTowerPlacement));

            // Populate Pause Overlay
            CreateText("PauseTitleText", pauseOverlay.transform, "GAME PAUSED", new Vector2(0, 150), 38, Color.white);
            
            Button resumeBtn = CreateButton("ResumeButton", pauseOverlay.transform, "RESUME", new Vector2(0, 50), new Vector2(200, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(resumeBtn.onClick, new UnityAction(uiManagerComp.OnResumeButtonClicked));

            Button restartBtnP = CreateButton("RestartButton", pauseOverlay.transform, "RESTART", new Vector2(0, -20), new Vector2(200, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(restartBtnP.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

            Button quitBtnP = CreateButton("QuitToMenuButton", pauseOverlay.transform, "MAIN MENU", new Vector2(0, -90), new Vector2(200, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(quitBtnP.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

            // Populate Victory Overlay
            CreateText("VicTitleText", victoryOverlay.transform, "VICTORY!", new Vector2(0, 150), 48, Color.white);
            
            Button restartBtnV = CreateButton("RestartButton", victoryOverlay.transform, "RESTART LEVEL", new Vector2(0, 0), new Vector2(240, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(restartBtnV.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

            Button quitBtnV = CreateButton("QuitToMenuButton", victoryOverlay.transform, "MAIN MENU", new Vector2(0, -70), new Vector2(240, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(quitBtnV.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

            // Populate Defeat Overlay
            CreateText("DefTitleText", defeatOverlay.transform, "GAME OVER", new Vector2(0, 150), 48, Color.white);

            Button restartBtnD = CreateButton("RestartButton", defeatOverlay.transform, "TRY AGAIN", new Vector2(0, 0), new Vector2(240, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(restartBtnD.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

            Button quitBtnD = CreateButton("QuitToMenuButton", defeatOverlay.transform, "MAIN MENU", new Vector2(0, -70), new Vector2(240, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(quitBtnD.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

            // Link UIManager references
            SerializedObject uiManagerSO = new SerializedObject(uiManagerComp);
            uiManagerSO.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
            uiManagerSO.FindProperty("gameplayHUDPanel").objectReferenceValue = gameplayHUD;
            uiManagerSO.FindProperty("pauseOverlayPanel").objectReferenceValue = pauseOverlay;
            uiManagerSO.FindProperty("victoryOverlayPanel").objectReferenceValue = victoryOverlay;
            uiManagerSO.FindProperty("defeatOverlayPanel").objectReferenceValue = defeatOverlay;
            uiManagerSO.FindProperty("healthText").objectReferenceValue = healthText;
            uiManagerSO.FindProperty("goldText").objectReferenceValue = goldText;
            uiManagerSO.FindProperty("waveText").objectReferenceValue = waveText;
            uiManagerSO.ApplyModifiedProperties();

            // Set ScriptableObject levelData directly to bypass type-casting bug
            uiManagerComp.LevelDataToPlay = levelData;
            uiManagerComp.Levels = new List<LevelData> { levelData, levelData2, levelData3 };
            EditorUtility.SetDirty(uiManagerComp);
            SetupTowerPlacementUI(gameplayHUD, towerData, towerPrefab);

            // Create Build Sites in LevelDemo
            GameObject buildSitePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BuildSite.prefab");
            Vector3[] sitePositions = new Vector3[]
            {
                new Vector3(-4f, 0.5f, 0f),
                new Vector3(-2f, 3.5f, 0f),
                new Vector3(2f, 3.5f, 0f),
                new Vector3(-2f, -3.5f, 0f),
                new Vector3(2f, -3.5f, 0f),
                new Vector3(4f, -0.5f, 0f)
            };

            if (buildSitePrefab != null)
            {
                for (int i = 0; i < sitePositions.Length; i++)
                {
                    GameObject siteGO = (GameObject)PrefabUtility.InstantiatePrefab(buildSitePrefab);
                    siteGO.transform.position = sitePositions[i];
                    siteGO.name = $"BuildSite_{i + 1}";
                }
            }

            // Create instructions overlay
            GameObject instructionsPanel = CreatePanel("InstructionsPanel", gameplayHUD.transform, Color.clear, true);
            RectTransform instRect = instructionsPanel.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(0.5f, 0f);
            instRect.anchorMax = new Vector2(0.5f, 0f);
            instRect.pivot = new Vector2(0.5f, 0f);
            instRect.anchoredPosition = new Vector2(0f, 40f);
            instRect.sizeDelta = new Vector2(800f, 100f);

            CreateText("InstructionsText", instructionsPanel.transform, 
                "Left-Click to place a tower. Right-Click to cancel tower placement.", 
                Vector2.zero, 20, Color.yellow);

            // Save Scene
            EditorSceneManager.SaveScene(demoScene, "Assets/Scenes/LevelDemo.unity");
            Debug.Log("[Setup] Saved Scene 'Assets/Scenes/LevelDemo.unity'.");

            // Add Scene to build settings if not already there
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool sceneExistsInBuild = false;
            foreach (var bScene in buildScenes)
            {
                if (bScene.path == "Assets/Scenes/LevelDemo.unity")
                {
                    sceneExistsInBuild = true;
                    break;
                }
            }
            if (!sceneExistsInBuild)
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/LevelDemo.unity", true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
                Debug.Log("[Setup] Added LevelDemo scene to Build Settings.");
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("LevelDemo Setup Complete", "The interactive 'LevelDemo' scene has been created successfully!\n\nOpen 'Assets/Scenes/LevelDemo' and click Play to test.", "OK");
        }

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void CreateLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            bool found = false;
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                if (sp.stringValue == layerName)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                for (int i = 8; i < layersProp.arraySize; i++)
                {
                    SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(sp.stringValue))
                    {
                        sp.stringValue = layerName;
                        tagManager.ApplyModifiedProperties();
                        Debug.Log($"Created layer: {layerName}");
                        break;
                    }
                }
            }
        }

        private static void CreateTag(string tagName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                tagsProp.GetArrayElementAtIndex(0).stringValue = tagName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"Created tag: {tagName}");
            }
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, Vector2 anchoredPosition, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            
            TextMeshProUGUI textComp = go.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.color = color;
            textComp.alignment = alignment;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (fontGuids.Length > 0)
                {
                    font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
                }
            }
            if (font != null)
            {
                textComp.font = font;
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(400, 100);

            return textComp;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color buttonColor, UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.color = buttonColor;
            img.type = Image.Type.Sliced;

            Button btn = go.AddComponent<Button>();
            if (action != null)
            {
                UnityEventTools.AddPersistentListener(btn.onClick, action);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            CreateText(name + "_Label", go.transform, label, Vector2.zero, 24, Color.black);

            return btn;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color bgColor, bool active)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            img.color = bgColor;
            img.type = Image.Type.Sliced;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            go.SetActive(active);
            return go;
        }

        private static void SetupTowerPlacementUI(GameObject gameplayHUD, TowerData towerData, GameObject towerPrefab)
        {
            // 1. Create or find TowerPlacementManager in the scene
            TowerPlacementManager placementManager = GameObject.FindObjectOfType<TowerPlacementManager>();
            if (placementManager == null)
            {
                GameObject managerGO = new GameObject("TowerPlacementManager");
                placementManager = managerGO.AddComponent<TowerPlacementManager>();
            }

            SerializedObject managerSO = new SerializedObject(placementManager);
            managerSO.FindProperty("defaultTowerData").objectReferenceValue = towerData;
            managerSO.FindProperty("defaultTowerPrefab").objectReferenceValue = towerPrefab;
            managerSO.ApplyModifiedProperties();

            // 2. Create Left-Hand Side Shop Panel
            Transform existingShop = gameplayHUD.transform.Find("ShopPanel");
            if (existingShop != null)
            {
                DestroyImmediate(existingShop.gameObject);
            }

            GameObject shopPanel = CreatePanel("ShopPanel", gameplayHUD.transform, new Color(0.12f, 0.12f, 0.16f, 0.85f), true);
            RectTransform rect = shopPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(30f, 0f);
            rect.sizeDelta = new Vector2(220f, 600f);

            // Add Shop Title
            CreateText("ShopTitle", shopPanel.transform, "TOWERS", new Vector2(0f, 250f), 24, Color.white);

            // Create Slot for Basic Tower
            GameObject slotGO = new GameObject("TowerSlot_Basic", typeof(RectTransform), typeof(CanvasRenderer));
            slotGO.transform.SetParent(shopPanel.transform, false);

            Image slotImg = slotGO.AddComponent<Image>();
            slotImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            slotImg.color = Color.white;
            slotImg.type = Image.Type.Sliced;

            RectTransform slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.anchoredPosition = new Vector2(0f, 130f);
            slotRect.sizeDelta = new Vector2(180f, 130f);

            // Add TowerSlot script
            TowerSlot towerSlot = slotGO.AddComponent<TowerSlot>();

            // Icon Image
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer));
            iconGO.transform.SetParent(slotGO.transform, false);
            Image iconImg = iconGO.AddComponent<Image>();
            
            SpriteRenderer prefabSR = towerPrefab.GetComponent<SpriteRenderer>();
            if (prefabSR != null && prefabSR.sprite != null)
            {
                iconImg.sprite = prefabSR.sprite;
                iconImg.color = prefabSR.color;
            }
            else
            {
                iconImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                iconImg.color = Color.cyan;
            }

            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0f, 20f);
            iconRect.sizeDelta = new Vector2(60f, 60f);

            // Name text
            TextMeshProUGUI nameText = CreateText("NameText", slotGO.transform, towerData.TowerName, new Vector2(0f, -25f), 18, Color.white);
            RectTransform nameRect = nameText.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(160f, 30f);

            // Cost text
            TextMeshProUGUI costText = CreateText("CostText", slotGO.transform, $"{towerData.Cost} G", new Vector2(0f, -50f), 16, Color.yellow);
            RectTransform costRect = costText.GetComponent<RectTransform>();
            costRect.sizeDelta = new Vector2(160f, 30f);

            // Hook up fields
            towerSlot.TowerData = towerData;
            towerSlot.TowerPrefab = towerPrefab;
            towerSlot.TowerNameText = nameText;
            towerSlot.TowerCostText = costText;
            towerSlot.TowerIcon = iconImg;
            towerSlot.SlotImage = slotImg;

            EditorUtility.SetDirty(slotGO);
        }

        [MenuItem("Tower Defense/Setup Main Menu and All 6 Levels")]
        public static void SetupMainMenuAndLevels()
        {
            EnsureFolderExists("Assets/Prefabs");
            EnsureFolderExists("Assets/ScriptableObjects");
            EnsureFolderExists("Assets/Scenes");

            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            GameObject towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
            EnemyData enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData.asset");
            TowerData towerData = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/ScriptableObjects/TestTowerData.asset");
            WaveData waveData = AssetDatabase.LoadAssetAtPath<WaveData>("Assets/ScriptableObjects/TestWaveData.asset");

            if (projectilePrefab == null || enemyPrefab == null || towerPrefab == null || enemyData == null || towerData == null || waveData == null)
            {
                SetupDemo();
                projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
                enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
                towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
                enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData.asset");
                towerData = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/ScriptableObjects/TestTowerData.asset");
                waveData = AssetDatabase.LoadAssetAtPath<WaveData>("Assets/ScriptableObjects/TestWaveData.asset");
            }

            List<LevelData> allLevels = new List<LevelData>();
            int[] startingGolds = { 100, 200, 200, 300, 300, 400 };
            int[] baseHealths = { 10, 12, 15, 18, 20, 25 };

            // Force w1 to w4 wave references on all 6 levels
            WaveData w1_all = CreateWaveAsset("TestWaveData_W1", 3, 4.0f, 4, 3.0f, 2, 6.0f, 2, 5.0f);
            WaveData w2_all = CreateWaveAsset("TestWaveData_W2", 4, 4.0f, 5, 3.0f, 3, 6.0f, 0, 5.0f);
            WaveData w3_all = CreateWaveAsset("TestWaveData_W3", 5, 3.5f, 6, 2.5f, 4, 5.5f, 1, 4.5f);
            WaveData w4_all = CreateWaveAsset("TestWaveData_W4", 6, 3.0f, 8, 2.0f, 5, 5.0f, 3, 4.0f);

            for (int i = 1; i <= 6; i++)
            {
                string assetPath = $"Assets/ScriptableObjects/TestLevelData{i}.asset";
                if (i == 1) assetPath = "Assets/ScriptableObjects/TestLevelData.asset";
 
                LevelData lvl = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                if (lvl == null)
                {
                    lvl = ScriptableObject.CreateInstance<LevelData>();
                    AssetDatabase.CreateAsset(lvl, assetPath);
                }
                
                SerializedObject lvlSO = new SerializedObject(lvl);
                lvlSO.FindProperty("levelName").stringValue = $"Level {i}";
                lvlSO.FindProperty("startingGold").intValue = startingGolds[i - 1];
                lvlSO.FindProperty("baseMaxHealth").intValue = baseHealths[i - 1];
                
                SerializedProperty wavesProp = lvlSO.FindProperty("waves");
                wavesProp.ClearArray();
                wavesProp.InsertArrayElementAtIndex(0); wavesProp.GetArrayElementAtIndex(0).objectReferenceValue = w1_all;
                wavesProp.InsertArrayElementAtIndex(1); wavesProp.GetArrayElementAtIndex(1).objectReferenceValue = w2_all;
                wavesProp.InsertArrayElementAtIndex(2); wavesProp.GetArrayElementAtIndex(2).objectReferenceValue = w3_all;
                wavesProp.InsertArrayElementAtIndex(3); wavesProp.GetArrayElementAtIndex(3).objectReferenceValue = w4_all;
                
                lvlSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(lvl);
                allLevels.Add(lvl);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            for (int i = 0; i < 6; i++)
            {
                string assetPath = i == 0 ? "Assets/ScriptableObjects/TestLevelData.asset" : $"Assets/ScriptableObjects/TestLevelData{i + 1}.asset";
                allLevels[i] = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            }

            for (int i = 1; i <= 6; i++)
            {
                string scenePath = $"Assets/Scenes/Level {i}.unity";
                ConfigureLevelScene(scenePath, allLevels[i - 1], i, projectilePrefab, enemyPrefab, towerPrefab, enemyData, towerData, allLevels);
            }

            CreateMainMenuScene(allLevels);

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true));
            for (int i = 1; i <= 6; i++)
            {
                buildScenes.Add(new EditorBuildSettingsScene($"Assets/Scenes/Level {i}.unity", true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Main Menu & 6 Levels Setup Complete", 
                "Successfully configured the 'MainMenu' scene and all 6 playable level scenes in the project!\n\nBuild settings have been updated. Load 'Assets/Scenes/MainMenu' and play!", "OK");
        }

        private static void ConfigureLevelScene(string scenePath, LevelData levelData, int levelNum, 
            GameObject projectilePrefab, GameObject enemyPrefab, GameObject towerPrefab, 
            EnemyData enemyData, TowerData towerData, List<LevelData> allLevels)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError($"[Setup] Scene file not found: {scenePath}");
                return;
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject cameraGO = GameObject.Find("Main Camera");
            if (cameraGO != null)
            {
                Camera cam = cameraGO.GetComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.backgroundColor = new Color(0.15f, 0.2f, 0.15f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                
                // Set camera position to Z = -10f to see gameplay elements at Z = 0f
                Vector3 pos = cameraGO.transform.position;
                pos.z = -10f;
                cameraGO.transform.position = pos;
            }

            WaypointPath pathComp = GameObject.FindFirstObjectByType<WaypointPath>();
            if (pathComp == null)
            {
                GameObject pathGO = new GameObject("WaypointPath");
                pathComp = pathGO.AddComponent<WaypointPath>();
                GameObject wpStart = new GameObject("WP_Start"); wpStart.transform.SetParent(pathGO.transform); wpStart.transform.position = new Vector3(-8f, 3f, 0f);
                GameObject wpMid1 = new GameObject("WP_Mid1"); wpMid1.transform.SetParent(pathGO.transform); wpMid1.transform.position = new Vector3(-2f, 3f, 0f);
                GameObject wpMid2 = new GameObject("WP_Mid2"); wpMid2.transform.SetParent(pathGO.transform); wpMid2.transform.position = new Vector3(2f, -3f, 0f);
                GameObject wpEnd = new GameObject("WP_End"); wpEnd.transform.SetParent(pathGO.transform); wpEnd.transform.position = new Vector3(8f, -3f, 0f);
                pathComp.PopulateFromChildren();
            }

            ObjectPooler poolerComp = GameObject.FindFirstObjectByType<ObjectPooler>();
            if (poolerComp == null)
            {
                GameObject poolerGO = new GameObject("ObjectPooler");
                poolerComp = poolerGO.AddComponent<ObjectPooler>();
            }
            SerializedObject poolerSO = new SerializedObject(poolerComp);
            SerializedProperty prewarmConfigsProp = poolerSO.FindProperty("prewarmConfigs");
            prewarmConfigsProp.ClearArray();

            GameObject basicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Basic.prefab");
            GameObject fastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Fast.prefab");
            GameObject tankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Tank.prefab");
            GameObject armorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Armor.prefab");

            prewarmConfigsProp.InsertArrayElementAtIndex(0);
            prewarmConfigsProp.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = basicPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(0).FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(1);
            prewarmConfigsProp.GetArrayElementAtIndex(1).FindPropertyRelative("prefab").objectReferenceValue = fastPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(1).FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(2);
            prewarmConfigsProp.GetArrayElementAtIndex(2).FindPropertyRelative("prefab").objectReferenceValue = tankPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(2).FindPropertyRelative("size").intValue = 5;

            prewarmConfigsProp.InsertArrayElementAtIndex(3);
            prewarmConfigsProp.GetArrayElementAtIndex(3).FindPropertyRelative("prefab").objectReferenceValue = armorPrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(3).FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(4);
            prewarmConfigsProp.GetArrayElementAtIndex(4).FindPropertyRelative("prefab").objectReferenceValue = projectilePrefab;
            prewarmConfigsProp.GetArrayElementAtIndex(4).FindPropertyRelative("size").intValue = 20;
            poolerSO.ApplyModifiedProperties();

            WaveManager waveManagerComp = GameObject.FindFirstObjectByType<WaveManager>();
            if (waveManagerComp == null)
            {
                GameObject waveManagerGO = new GameObject("WaveManager");
                waveManagerComp = waveManagerGO.AddComponent<WaveManager>();
            }
            SerializedObject waveManagerSO = new SerializedObject(waveManagerComp);
            waveManagerSO.FindProperty("waypointPath").objectReferenceValue = pathComp;
            waveManagerSO.FindProperty("autoStartNextWave").boolValue = true;
            waveManagerSO.FindProperty("waveInterval").floatValue = 4.0f;

            waveManagerSO.FindProperty("basicEnemyPrefab").objectReferenceValue = basicPrefab;
            waveManagerSO.FindProperty("fastEnemyPrefab").objectReferenceValue = fastPrefab;
            waveManagerSO.FindProperty("tankEnemyPrefab").objectReferenceValue = tankPrefab;
            waveManagerSO.FindProperty("armorEnemyPrefab").objectReferenceValue = armorPrefab;

            waveManagerSO.FindProperty("basicEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Basic.asset");
            waveManagerSO.FindProperty("fastEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Fast.asset");
            waveManagerSO.FindProperty("tankEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Tank.asset");
            waveManagerSO.FindProperty("armorEnemyData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData_Armor.asset");
            waveManagerSO.ApplyModifiedProperties();

            GameManager gameManagerComp = GameObject.FindFirstObjectByType<GameManager>();
            if (gameManagerComp == null)
            {
                GameObject gameManagerGO = new GameObject("GameManager");
                gameManagerComp = gameManagerGO.AddComponent<GameManager>();
            }
            gameManagerComp.DefaultLevelData = levelData;
            EditorUtility.SetDirty(gameManagerComp);

            Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
            UIManager uiManagerComp = GameObject.FindFirstObjectByType<UIManager>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                canvasGO.AddComponent<GraphicRaycaster>();

                if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject es = new GameObject("EventSystem");
                    es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }

                if (uiManagerComp == null)
                {
                    GameObject uiManagerGO = new GameObject("UIManager");
                    uiManagerComp = uiManagerGO.AddComponent<UIManager>();
                }

                GameObject mainMenu = CreatePanel("MainMenuPanel", canvasGO.transform, new Color(0, 0, 0, 0.9f), false);
                GameObject gameplayHUD = CreatePanel("GameplayHUDPanel", canvasGO.transform, Color.clear, false);
                GameObject pauseOverlay = CreatePanel("PauseOverlayPanel", canvasGO.transform, new Color(0, 0, 0, 0.75f), false);
                GameObject victoryOverlay = CreatePanel("VictoryOverlayPanel", canvasGO.transform, new Color(0, 0.5f, 0, 0.85f), false);
                GameObject defeatOverlay = CreatePanel("DefeatOverlayPanel", canvasGO.transform, new Color(0.5f, 0, 0, 0.85f), false);

                CreateText("TitleText", mainMenu.transform, "TOWER DEFENSE GAME", new Vector2(0, 150), 48, Color.white);
                Button playBtn = CreateButton("PlayButton", mainMenu.transform, "PLAY LEVEL", new Vector2(0, -50), new Vector2(250, 60), Color.green, null);
                UnityEventTools.AddVoidPersistentListener(playBtn.onClick, new UnityAction(uiManagerComp.OnPlayButtonClicked));

                TextMeshProUGUI healthText = CreateText("HealthText", gameplayHUD.transform, $"HP: {levelData.BaseMaxHealth}/{levelData.BaseMaxHealth}", new Vector2(-700, 480), 28, Color.red, TextAlignmentOptions.Left);
                TextMeshProUGUI goldText = CreateText("GoldText", gameplayHUD.transform, $"Gold: {levelData.StartingGold}", new Vector2(0, 480), 28, Color.yellow, TextAlignmentOptions.Center);
                TextMeshProUGUI waveText = CreateText("WaveText", gameplayHUD.transform, "Wave: 1/1", new Vector2(700, 480), 28, Color.cyan, TextAlignmentOptions.Right);

                Button startWaveBtn = CreateButton("StartWaveButton", gameplayHUD.transform, "START WAVE", new Vector2(0, -480), new Vector2(220, 50), Color.green, null);
                UnityEventTools.AddVoidPersistentListener(startWaveBtn.onClick, new UnityAction(waveManagerComp.StartNextWave));

                Button pauseBtn = CreateButton("PauseButton", gameplayHUD.transform, "PAUSE", new Vector2(850, 480), new Vector2(100, 40), Color.white, null);
                UnityEventTools.AddVoidPersistentListener(pauseBtn.onClick, new UnityAction(uiManagerComp.OnPauseButtonClicked));

                CreateText("PauseTitleText", pauseOverlay.transform, "GAME PAUSED", new Vector2(0, 150), 38, Color.white);
                
                Button resumeBtn = CreateButton("ResumeButton", pauseOverlay.transform, "RESUME", new Vector2(0, 50), new Vector2(200, 50), Color.white, null);
                UnityEventTools.AddVoidPersistentListener(resumeBtn.onClick, new UnityAction(uiManagerComp.OnResumeButtonClicked));

                Button restartBtnP = CreateButton("RestartButton", pauseOverlay.transform, "RESTART", new Vector2(0, -20), new Vector2(200, 50), Color.white, null);
                UnityEventTools.AddVoidPersistentListener(restartBtnP.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

                Button quitBtnP = CreateButton("QuitToMenuButton", pauseOverlay.transform, "MAIN MENU", new Vector2(0, -90), new Vector2(200, 50), Color.white, null);
                UnityEventTools.AddVoidPersistentListener(quitBtnP.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

                CreateText("VicTitleText", victoryOverlay.transform, "VICTORY!", new Vector2(0, 150), 48, Color.white);
                
                Button restartBtnV = CreateButton("RestartButton", victoryOverlay.transform, "RESTART LEVEL", new Vector2(0, 0), new Vector2(240, 50), Color.white, null);
                UnityEventTools.AddVoidPersistentListener(restartBtnV.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

                Button quitBtnV = CreateButton("QuitToMenuButton", victoryOverlay.transform, "MAIN MENU", new Vector2(0, -70), new Vector2(240, 50), Color.white, null);
                UnityEventTools.AddVoidPersistentListener(quitBtnV.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

                CreateText("DefTitleText", defeatOverlay.transform, "GAME OVER", new Vector2(0, 150), 48, Color.white);

                Button restartBtnD = CreateButton("RestartButton", defeatOverlay.transform, "TRY AGAIN", new Vector2(0, 0), new Vector2(240, 50), Color.white, null);
                UnityEventTools.AddVoidPersistentListener(restartBtnD.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

                Button quitBtnD = CreateButton("QuitToMenuButton", defeatOverlay.transform, "MAIN MENU", new Vector2(0, -70), new Vector2(240, 50), Color.white, null);
                UnityEventTools.AddVoidPersistentListener(quitBtnD.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

                SerializedObject uiManagerSO = new SerializedObject(uiManagerComp);
                uiManagerSO.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
                uiManagerSO.FindProperty("gameplayHUDPanel").objectReferenceValue = gameplayHUD;
                uiManagerSO.FindProperty("pauseOverlayPanel").objectReferenceValue = pauseOverlay;
                uiManagerSO.FindProperty("victoryOverlayPanel").objectReferenceValue = victoryOverlay;
                uiManagerSO.FindProperty("defeatOverlayPanel").objectReferenceValue = defeatOverlay;
                uiManagerSO.FindProperty("healthText").objectReferenceValue = healthText;
                uiManagerSO.FindProperty("goldText").objectReferenceValue = goldText;
                uiManagerSO.FindProperty("waveText").objectReferenceValue = waveText;
                uiManagerSO.ApplyModifiedProperties();

                uiManagerComp.LevelDataToPlay = levelData;
                uiManagerComp.Levels = allLevels;
                EditorUtility.SetDirty(uiManagerComp);

                SetupTowerPlacementUI(gameplayHUD, towerData, towerPrefab);
            }

            BuildSite[] sites = GameObject.FindObjectsByType<BuildSite>(FindObjectsSortMode.None);
            if (sites == null || sites.Length == 0)
            {
                GameObject buildSitePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BuildSite.prefab");
                if (buildSitePrefab != null)
                {
                    Vector3[] sitePositions = new Vector3[]
                    {
                        new Vector3(-5f, 1.5f, 0f),
                        new Vector3(4f, -1.5f, 0f),
                        new Vector3(-2f, 1.5f, 0f),
                        new Vector3(1f, -1.5f, 0f),
                        new Vector3(0f, 0.5f, 0f),
                        new Vector3(-5f, -0.5f, 0f),
                        new Vector3(5f, 1.5f, 0f)
                    };
                    for (int s = 0; s < sitePositions.Length; s++)
                    {
                        GameObject siteGO = (GameObject)PrefabUtility.InstantiatePrefab(buildSitePrefab);
                        siteGO.transform.position = sitePositions[s];
                        siteGO.name = $"BuildSite_{s + 1}";
                    }
                }
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Setup] Configured scene: {scenePath}");
        }

        private static void CreateMainMenuScene(List<LevelData> allLevels)
        {
            UnityEngine.SceneManagement.Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject cameraGO = GameObject.Find("Main Camera");
            if (cameraGO != null)
            {
                Camera cam = cameraGO.GetComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.backgroundColor = new Color(0.12f, 0.12f, 0.16f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                
                // Set camera position to Z = -10f to see gameplay elements at Z = 0f
                Vector3 pos = cameraGO.transform.position;
                pos.z = -10f;
                cameraGO.transform.position = pos;
            }

            GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            GameObject uiManagerGO = new GameObject("UIManager");
            UIManager uiManagerComp = uiManagerGO.AddComponent<UIManager>();

            GameObject mainMenuPanel = CreatePanel("MainMenuPanel", canvasGO.transform, new Color(0, 0, 0, 0.85f), true);

            CreateText("TitleText", mainMenuPanel.transform, "TOWER DEFENSE GAME", new Vector2(0, 150), 52, Color.white);
            
            Button playBtn = CreateButton("PlayButton", mainMenuPanel.transform, "PLAY GAME", new Vector2(0, -50), new Vector2(280, 70), Color.green, null);
            UnityEventTools.AddVoidPersistentListener(playBtn.onClick, new UnityAction(uiManagerComp.OnPlayButtonClicked));

            SerializedObject uiManagerSO = new SerializedObject(uiManagerComp);
            uiManagerSO.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            uiManagerSO.ApplyModifiedProperties();

            uiManagerComp.LevelDataToPlay = allLevels[0];
            uiManagerComp.Levels = allLevels;
            EditorUtility.SetDirty(uiManagerComp);

            EditorSceneManager.SaveScene(mainMenuScene, "Assets/Scenes/MainMenu.unity");
            Debug.Log("[Setup] Created and saved Main Menu scene.");
        }

        private static WaveData CreateWaveAsset(string name, int basic, float basicInt, int fast, float fastInt, int tank, float tankInt, int armor, float armorInt)
        {
            string path = $"Assets/ScriptableObjects/{name}.asset";
            WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (wave == null)
            {
                wave = ScriptableObject.CreateInstance<WaveData>();
            }

            wave.BasicCount = basic;
            wave.BasicSpawnInterval = basicInt;
            wave.FastCount = fast;
            wave.FastSpawnInterval = fastInt;
            wave.TankCount = tank;
            wave.TankSpawnInterval = tankInt;
            wave.ArmorCount = armor;
            wave.ArmorSpawnInterval = armorInt;

            EditorUtility.SetDirty(wave);

            if (!AssetDatabase.Contains(wave))
            {
                AssetDatabase.CreateAsset(wave, path);
            }
            return wave;
        }

        private static GameObject CreateEnemyPrefab(string name, System.Type healthComponentType, Color color, Vector3 scale, Sprite knobSprite)
        {
            string path = $"Assets/Prefabs/{name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject enemyTemp = new GameObject(name);
            enemyTemp.layer = LayerMask.NameToLayer(ENEMY_LAYER);
            enemyTemp.tag = "Enemy";
            SpriteRenderer enemySR = enemyTemp.AddComponent<SpriteRenderer>();
            enemySR.sprite = knobSprite;
            enemySR.color = color;
            enemyTemp.transform.localScale = scale;

            CircleCollider2D enemyCollider = enemyTemp.AddComponent<CircleCollider2D>();
            enemyCollider.radius = 0.5f;
            enemyCollider.isTrigger = true;

            Rigidbody2D enemyRB = enemyTemp.AddComponent<Rigidbody2D>();
            enemyRB.bodyType = RigidbodyType2D.Kinematic;

            enemyTemp.AddComponent<EnemyMovement>();
            enemyTemp.AddComponent(healthComponentType);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemyTemp, path);
            DestroyImmediate(enemyTemp);
            Debug.Log($"[Setup] Created Prefab: {path}");
            return prefab;
        }

        private static EnemyData CreateEnemyDataAsset(string name, EnemyType type, int goldReward)
        {
            string path = $"Assets/ScriptableObjects/{name}.asset";
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                SerializedObject so = new SerializedObject(data);
                so.FindProperty("enemyType").enumValueIndex = (int)type;
                so.FindProperty("goldReward").intValue = goldReward;
                so.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(data, path);
            }
            return data;
        }
    }
}
