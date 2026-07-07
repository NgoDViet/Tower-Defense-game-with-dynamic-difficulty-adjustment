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

        [MenuItem("Tower Defense/Setup Level3 Spawning")]
        public static void ShowLevel3Window()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Setup Error",
                    "You cannot setup the Level3 spawning while the editor is in Play Mode. Please exit Play Mode and try again.",
                    "OK");
                return;
            }

            if (EditorUtility.DisplayDialog(
                "Setup Level3 Spawning",
                "This script will add the ObjectPooler, WaveManager, GameManager, UIManager, Canvas UI, and EventSystem to the Level3 scene so that enemies can spawn and follow the path.\n\n" +
                "Do you want to proceed?",
                "Yes, Setup Level3 Spawning",
                "Cancel"))
            {
                SetupLevel3();
            }
        }

        public static void SetupLevel3()
        {
            // Open Level3 scene
            UnityEngine.SceneManagement.Scene level3Scene = EditorSceneManager.OpenScene("Assets/Scenes/Level3.unity", OpenSceneMode.Single);

            // Find existing WaypointPath
            WaypointPath pathComp = GameObject.FindObjectOfType<WaypointPath>();
            if (pathComp == null)
            {
                Debug.LogError("[Setup] Could not find a WaypointPath component in Level3.unity.");
                return;
            }

            // Load required prefabs and scriptable objects
            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            GameObject towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tower.prefab");
            EnemyData enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/TestEnemyData.asset");
            TowerData towerData = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/ScriptableObjects/TestTowerData.asset");
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/TestLevelData.asset");

            if (projectilePrefab == null || enemyPrefab == null || towerPrefab == null || enemyData == null || towerData == null || levelData == null)
            {
                Debug.LogError("[Setup] Required prefabs or ScriptableObjects (TestEnemyData, TestTowerData, TestLevelData) are missing.");
                return;
            }

            // Create ObjectPooler if missing
            ObjectPooler poolerComp = GameObject.FindObjectOfType<ObjectPooler>();
            if (poolerComp == null)
            {
                GameObject poolerGO = new GameObject("ObjectPooler");
                poolerComp = poolerGO.AddComponent<ObjectPooler>();
            }
            SerializedObject poolerSO = new SerializedObject(poolerComp);
            SerializedProperty prewarmConfigsProp = poolerSO.FindProperty("prewarmConfigs");
            prewarmConfigsProp.ClearArray();
            
            prewarmConfigsProp.InsertArrayElementAtIndex(0);
            SerializedProperty configEnemy = prewarmConfigsProp.GetArrayElementAtIndex(0);
            configEnemy.FindPropertyRelative("prefab").objectReferenceValue = enemyPrefab;
            configEnemy.FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(1);
            SerializedProperty configProj = prewarmConfigsProp.GetArrayElementAtIndex(1);
            configProj.FindPropertyRelative("prefab").objectReferenceValue = projectilePrefab;
            configProj.FindPropertyRelative("size").intValue = 20;
            poolerSO.ApplyModifiedProperties();

            // Create WaveManager if missing
            WaveManager waveManagerComp = GameObject.FindObjectOfType<WaveManager>();
            if (waveManagerComp == null)
            {
                GameObject waveManagerGO = new GameObject("WaveManager");
                waveManagerComp = waveManagerGO.AddComponent<WaveManager>();
            }
            SerializedObject waveManagerSO = new SerializedObject(waveManagerComp);
            waveManagerSO.FindProperty("waypointPath").objectReferenceValue = pathComp;
            waveManagerSO.FindProperty("autoStartNextWave").boolValue = true;
            waveManagerSO.FindProperty("waveInterval").floatValue = 4.0f;
            waveManagerSO.ApplyModifiedProperties();

            // Create GameManager if missing
            GameManager gameManagerComp = GameObject.FindObjectOfType<GameManager>();
            if (gameManagerComp == null)
            {
                GameObject gameManagerGO = new GameObject("GameManager");
                gameManagerComp = gameManagerGO.AddComponent<GameManager>();
            }
            gameManagerComp.DefaultLevelData = levelData;
            EditorUtility.SetDirty(gameManagerComp);

            // Create UIManager if missing
            UIManager uiManagerComp = GameObject.FindObjectOfType<UIManager>();
            if (uiManagerComp == null)
            {
                GameObject uiManagerGO = new GameObject("UIManager");
                uiManagerComp = uiManagerGO.AddComponent<UIManager>();
            }

            // Create Canvas Hierarchy if missing
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
            GameObject canvasGO;
            if (canvas == null)
            {
                canvasGO = new GameObject("Canvas", typeof(RectTransform));
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasGO = canvas.gameObject;
            }

            // Create EventSystem if missing
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Find or create panels
            Transform canvasTransform = canvasGO.transform;
            GameObject mainMenu = FindOrCreatePanel("MainMenuPanel", canvasTransform, new Color(0, 0, 0, 0.9f), true);
            GameObject gameplayHUD = FindOrCreatePanel("GameplayHUDPanel", canvasTransform, Color.clear, false);
            GameObject pauseOverlay = FindOrCreatePanel("PauseOverlayPanel", canvasTransform, new Color(0, 0, 0, 0.75f), false);
            GameObject victoryOverlay = FindOrCreatePanel("VictoryOverlayPanel", canvasTransform, new Color(0, 0.5f, 0, 0.85f), false);
            GameObject defeatOverlay = FindOrCreatePanel("DefeatOverlayPanel", canvasTransform, new Color(0.5f, 0, 0, 0.85f), false);

            // Populate Main Menu buttons and text
            DestroyChildren(mainMenu.transform);
            CreateText("TitleText", mainMenu.transform, "TOWER DEFENSE GAME", new Vector2(0, 150), 48, Color.white);
            Button playBtn = CreateButton("PlayButton", mainMenu.transform, "PLAY LEVEL", new Vector2(0, -50), new Vector2(250, 60), Color.green, null);
            UnityEventTools.AddVoidPersistentListener(playBtn.onClick, new UnityAction(uiManagerComp.OnPlayButtonClicked));

            // Populate Gameplay HUD buttons and text
            DestroyChildren(gameplayHUD.transform);
            TextMeshProUGUI healthText = CreateText("HealthText", gameplayHUD.transform, "HP: 10/10", new Vector2(-700, 480), 28, Color.red, TextAlignmentOptions.Left);
            TextMeshProUGUI goldText = CreateText("GoldText", gameplayHUD.transform, "Gold: 100", new Vector2(0, 480), 28, Color.yellow, TextAlignmentOptions.Center);
            TextMeshProUGUI waveText = CreateText("WaveText", gameplayHUD.transform, "Wave: 1/1", new Vector2(700, 480), 28, Color.cyan, TextAlignmentOptions.Right);

            Button startWaveBtn = CreateButton("StartWaveButton", gameplayHUD.transform, "START WAVE", new Vector2(0, -480), new Vector2(220, 50), Color.green, null);
            UnityEventTools.AddVoidPersistentListener(startWaveBtn.onClick, new UnityAction(waveManagerComp.StartNextWave));

            Button pauseBtn = CreateButton("PauseButton", gameplayHUD.transform, "PAUSE", new Vector2(850, 480), new Vector2(100, 40), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(pauseBtn.onClick, new UnityAction(uiManagerComp.OnPauseButtonClicked));

            // Populate Pause Overlay buttons and text
            DestroyChildren(pauseOverlay.transform);
            CreateText("PauseTitleText", pauseOverlay.transform, "GAME PAUSED", new Vector2(0, 150), 38, Color.white);
            
            Button resumeBtn = CreateButton("ResumeButton", pauseOverlay.transform, "RESUME", new Vector2(0, 50), new Vector2(200, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(resumeBtn.onClick, new UnityAction(uiManagerComp.OnResumeButtonClicked));

            Button restartBtnP = CreateButton("RestartButton", pauseOverlay.transform, "RESTART", new Vector2(0, -20), new Vector2(200, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(restartBtnP.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

            Button quitBtnP = CreateButton("QuitToMenuButton", pauseOverlay.transform, "MAIN MENU", new Vector2(0, -90), new Vector2(200, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(quitBtnP.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

            // Populate Victory Overlay
            DestroyChildren(victoryOverlay.transform);
            CreateText("VicTitleText", victoryOverlay.transform, "VICTORY!", new Vector2(0, 150), 48, Color.white);
            
            Button restartBtnV = CreateButton("RestartButton", victoryOverlay.transform, "RESTART LEVEL", new Vector2(0, 0), new Vector2(240, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(restartBtnV.onClick, new UnityAction(uiManagerComp.OnRestartButtonClicked));

            Button quitBtnV = CreateButton("QuitToMenuButton", victoryOverlay.transform, "MAIN MENU", new Vector2(0, -70), new Vector2(240, 50), Color.white, null);
            UnityEventTools.AddVoidPersistentListener(quitBtnV.onClick, new UnityAction(uiManagerComp.OnReturnToMainMenuButtonClicked));

            // Populate Defeat Overlay
            DestroyChildren(defeatOverlay.transform);
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

            // Set LevelData to play
            uiManagerComp.LevelDataToPlay = levelData;
            EditorUtility.SetDirty(uiManagerComp);

            // Save Scene
            EditorSceneManager.SaveScene(level3Scene, "Assets/Scenes/Level3.unity");
            Debug.Log("[Setup] Saved Scene 'Assets/Scenes/Level3.unity'.");
        }

        private static GameObject FindOrCreatePanel(string name, Transform parent, Color bgColor, bool active)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                existing.gameObject.SetActive(active);
                return existing.gameObject;
            }
            return CreatePanel(name, parent, bgColor, active);
        }

        private static void DestroyChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
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

            // 2. Create Enemy Prefab
            GameObject enemyTemp = new GameObject("EnemyTemp");
            enemyTemp.layer = LayerMask.NameToLayer(ENEMY_LAYER);
            enemyTemp.tag = "Enemy";
            SpriteRenderer enemySR = enemyTemp.AddComponent<SpriteRenderer>();
            enemySR.sprite = knobSprite;
            enemySR.color = Color.red;
            
            CircleCollider2D enemyCollider = enemyTemp.AddComponent<CircleCollider2D>();
            enemyCollider.radius = 0.5f;
            enemyCollider.isTrigger = true;

            Rigidbody2D enemyRB = enemyTemp.AddComponent<Rigidbody2D>();
            enemyRB.bodyType = RigidbodyType2D.Kinematic;

            EnemyMovement enemyMovement = enemyTemp.AddComponent<EnemyMovement>();
            EnemyHealth enemyHealth = enemyTemp.AddComponent<EnemyHealth>();

            GameObject enemyPrefab = PrefabUtility.SaveAsPrefabAsset(enemyTemp, "Assets/Prefabs/Enemy.prefab");
            DestroyImmediate(enemyTemp);
            Debug.Log("[Setup] Created Enemy Prefab.");

            // 3. Create ScriptableObjects
            // Create EnemyData
            EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
            SerializedObject enemyDataSO = new SerializedObject(enemyData);
            enemyDataSO.FindProperty("enemyName").stringValue = "Basic Enemy";
            enemyDataSO.FindProperty("moveSpeed").floatValue = 2.0f;
            enemyDataSO.FindProperty("maxHealth").intValue = 10;
            enemyDataSO.FindProperty("goldReward").intValue = 15;
            enemyDataSO.FindProperty("damageToBase").intValue = 1;
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

            // Create WaveData
            WaveData waveData = ScriptableObject.CreateInstance<WaveData>();
            SerializedObject waveDataSO = new SerializedObject(waveData);
            SerializedProperty spawnGroupsProp = waveDataSO.FindProperty("spawnGroups");
            spawnGroupsProp.ClearArray();
            spawnGroupsProp.InsertArrayElementAtIndex(0);
            SerializedProperty groupProp = spawnGroupsProp.GetArrayElementAtIndex(0);
            groupProp.FindPropertyRelative("enemyData").objectReferenceValue = enemyData;
            groupProp.FindPropertyRelative("enemyPrefab").objectReferenceValue = enemyPrefab;
            groupProp.FindPropertyRelative("count").intValue = 5;
            groupProp.FindPropertyRelative("spawnInterval").floatValue = 1.5f;
            groupProp.FindPropertyRelative("delayBeforeGroup").floatValue = 0f;
            waveDataSO.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(waveData, "Assets/ScriptableObjects/TestWaveData.asset");

            // Create LevelData
            LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
            SerializedObject levelDataSO = new SerializedObject(levelData);
            levelDataSO.FindProperty("levelName").stringValue = "Level 1";
            levelDataSO.FindProperty("startingGold").intValue = 100;
            levelDataSO.FindProperty("baseMaxHealth").intValue = 10;
            SerializedProperty wavesProp = levelDataSO.FindProperty("waves");
            wavesProp.ClearArray();
            wavesProp.InsertArrayElementAtIndex(0);
            wavesProp.GetArrayElementAtIndex(0).objectReferenceValue = waveData;
            levelDataSO.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(levelData, "Assets/ScriptableObjects/TestLevelData.asset");

            AssetDatabase.SaveAssets();

            // Update prefabs with correct references directly using memory references
            // to bypass any assembly type casting issues after asset import/reload.
            GameObject loadedEnemy = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Enemy.prefab");
            SerializedObject loadedEnemySO = new SerializedObject(loadedEnemy.GetComponent<EnemyMovement>());
            loadedEnemySO.FindProperty("enemyData").objectReferenceValue = enemyData;
            loadedEnemySO.ApplyModifiedProperties();
            SerializedObject loadedEnemyHealthSO = new SerializedObject(loadedEnemy.GetComponent<EnemyHealth>());
            loadedEnemyHealthSO.FindProperty("enemyData").objectReferenceValue = enemyData;
            loadedEnemyHealthSO.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAsset(loadedEnemy, "Assets/Prefabs/Enemy.prefab");
            PrefabUtility.UnloadPrefabContents(loadedEnemy);

            // 4. Create Tower Prefab (needs reference to TestTowerData and Projectile Prefab)
            GameObject towerTemp = new GameObject("TowerTemp");
            SpriteRenderer towerSR = towerTemp.AddComponent<SpriteRenderer>();
            towerSR.sprite = uiSprite;
            towerSR.color = Color.cyan;

            TowerController towerCtrl = towerTemp.AddComponent<TowerController>();
            GameObject shootPointGO = new GameObject("ShootPoint");
            shootPointGO.transform.SetParent(towerTemp.transform, false);

            SerializedObject towerCtrlSO = new SerializedObject(towerCtrl);
            towerCtrlSO.FindProperty("towerData").objectReferenceValue = towerData;
            towerCtrlSO.FindProperty("shootPoint").objectReferenceValue = shootPointGO.transform;
            towerCtrlSO.FindProperty("enemyLayerMask").intValue = 1 << LayerMask.NameToLayer(ENEMY_LAYER);
            towerCtrlSO.ApplyModifiedProperties();

            GameObject towerPrefab = PrefabUtility.SaveAsPrefabAsset(towerTemp, "Assets/Prefabs/Tower.prefab");
            DestroyImmediate(towerTemp);
            Debug.Log("[Setup] Created Tower Prefab.");

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
            
            prewarmConfigsProp.InsertArrayElementAtIndex(0);
            SerializedProperty configEnemy = prewarmConfigsProp.GetArrayElementAtIndex(0);
            configEnemy.FindPropertyRelative("prefab").objectReferenceValue = enemyPrefab;
            configEnemy.FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(1);
            SerializedProperty configProj = prewarmConfigsProp.GetArrayElementAtIndex(1);
            configProj.FindPropertyRelative("prefab").objectReferenceValue = projectilePrefab;
            configProj.FindPropertyRelative("size").intValue = 20;
            poolerSO.ApplyModifiedProperties();

            // Create WaveManager
            GameObject waveManagerGO = new GameObject("WaveManager");
            WaveManager waveManagerComp = waveManagerGO.AddComponent<WaveManager>();
            SerializedObject waveManagerSO = new SerializedObject(waveManagerComp);
            waveManagerSO.FindProperty("waypointPath").objectReferenceValue = pathComp;
            waveManagerSO.FindProperty("autoStartNextWave").boolValue = true;
            waveManagerSO.FindProperty("waveInterval").floatValue = 4.0f;
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
            uiManagerComp.LevelDataToPlay = levelData;
            EditorUtility.SetDirty(uiManagerComp);
            Debug.Log($"[Setup] Assigning levelDataToPlay. levelData is {(levelData == null ? "NULL" : "not null")}");

            // Place 2 Defensive Towers in the scene near the path
            GameObject tower1 = (GameObject)PrefabUtility.InstantiatePrefab(towerPrefab);
            tower1.transform.position = new Vector3(-5f, 1.5f, 0f);
            tower1.name = "DefensiveTower_1";

            GameObject tower2 = (GameObject)PrefabUtility.InstantiatePrefab(towerPrefab);
            tower2.transform.position = new Vector3(4f, -1.5f, 0f);
            tower2.name = "DefensiveTower_2";

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

            // If any is missing, request to run SetupDemo first
            if (projectilePrefab == null || enemyPrefab == null || towerPrefab == null || enemyData == null || towerData == null || levelData == null)
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
            
            prewarmConfigsProp.InsertArrayElementAtIndex(0);
            SerializedProperty configEnemy = prewarmConfigsProp.GetArrayElementAtIndex(0);
            configEnemy.FindPropertyRelative("prefab").objectReferenceValue = enemyPrefab;
            configEnemy.FindPropertyRelative("size").intValue = 10;

            prewarmConfigsProp.InsertArrayElementAtIndex(1);
            SerializedProperty configProj = prewarmConfigsProp.GetArrayElementAtIndex(1);
            configProj.FindPropertyRelative("prefab").objectReferenceValue = projectilePrefab;
            configProj.FindPropertyRelative("size").intValue = 20;
            poolerSO.ApplyModifiedProperties();

            // Create WaveManager
            GameObject waveManagerGO = new GameObject("WaveManager");
            WaveManager waveManagerComp = waveManagerGO.AddComponent<WaveManager>();
            SerializedObject waveManagerSO = new SerializedObject(waveManagerComp);
            waveManagerSO.FindProperty("waypointPath").objectReferenceValue = pathComp;
            waveManagerSO.FindProperty("autoStartNextWave").boolValue = false;
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
            EditorUtility.SetDirty(uiManagerComp);

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
    }
}
