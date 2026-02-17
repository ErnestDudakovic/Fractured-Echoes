// ============================================================================
// MainMenuSceneBuilder.cs — Editor utility
// Creates the MainMenu scene with all UI elements, panels, and wiring
// plus a SceneLoader prefab.  Run via:
//   Fractured Echoes → Build Main Menu Scene
//   Fractured Echoes → Build SceneLoader Prefab
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace FracturedEchoes.Editor
{
    public static class MainMenuSceneBuilder
    {
        // =====================================================================
        // MENU ITEMS
        // =====================================================================

        [MenuItem("Fractured Echoes/Build Main Menu Scene", false, 100)]
        public static void BuildMainMenuScene()
        {
            // Create a new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // -----------------------------------------------------------------
            // CAMERA SETUP — dark background
            // -----------------------------------------------------------------
            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            // -----------------------------------------------------------------
            // EVENT SYSTEM (already created by DefaultGameObjects, but verify)
            // -----------------------------------------------------------------
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // -----------------------------------------------------------------
            // ROOT CANVAS
            // -----------------------------------------------------------------
            GameObject canvasGo = CreateCanvas("MainMenuCanvas");
            Canvas canvas = canvasGo.GetComponent<Canvas>();

            // Attach MainMenuController
            var menuCtrl = canvasGo.AddComponent<UI.MainMenuController>();

            // -----------------------------------------------------------------
            // BACKGROUND IMAGE (dark overlay)
            // -----------------------------------------------------------------
            GameObject bgGo = CreatePanel(canvasGo.transform, "Background",
                new Color(0.01f, 0.01f, 0.02f, 1f));
            Stretch(bgGo.GetComponent<RectTransform>());

            // =================================================================
            // MAIN PANEL
            // =================================================================
            GameObject mainPanel = CreatePanel(canvasGo.transform, "MainPanel",
                new Color(0f, 0f, 0f, 0f)); // transparent
            Stretch(mainPanel.GetComponent<RectTransform>());

            // Title
            var titleTMP = CreateTMP(mainPanel.transform, "TitleText",
                "FRACTURED ECHOES", 64, TextAlignmentOptions.Center,
                new Color(0.85f, 0.85f, 0.9f));
            SetAnchored(titleTMP.rectTransform, new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0, -80f), new Vector2(600, 80));

            // Button container (vertical layout)
            GameObject btnContainer = CreateVerticalGroup(mainPanel.transform,
                "ButtonContainer", 16f);
            SetAnchored(btnContainer.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -20f), new Vector2(320, 300));

            // Buttons
            Button btnNew = CreateMenuButton(btnContainer.transform, "NewGameButton", "NEW GAME");
            Button btnLoad = CreateMenuButton(btnContainer.transform, "LoadGameButton", "LOAD GAME");
            Button btnSettings = CreateMenuButton(btnContainer.transform, "SettingsButton", "SETTINGS");
            Button btnQuit = CreateMenuButton(btnContainer.transform, "QuitButton", "QUIT");

            // Version text
            var versionTMP = CreateTMP(mainPanel.transform, "VersionText",
                "v0.1.0", 16, TextAlignmentOptions.BottomRight,
                new Color(0.4f, 0.4f, 0.45f));
            SetAnchored(versionTMP.rectTransform, new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(200, 30));

            // =================================================================
            // LOAD GAME PANEL
            // =================================================================
            GameObject loadPanel = CreatePanel(canvasGo.transform, "LoadGamePanel",
                new Color(0.02f, 0.02f, 0.04f, 0.95f));
            Stretch(loadPanel.GetComponent<RectTransform>());

            // Header
            CreateTMP(loadPanel.transform, "LoadHeader", "LOAD GAME", 42,
                TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f))
                .rectTransform.anchoredPosition = new Vector2(0, 260f);

            // Slot container (SaveSlotUI will populate this)
            GameObject slotContainer = new GameObject("SlotContainer");
            slotContainer.transform.SetParent(loadPanel.transform, false);
            var slotContainerRT = slotContainer.AddComponent<RectTransform>();
            SetAnchored(slotContainerRT, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 350));
            var slotLayout = slotContainer.AddComponent<VerticalLayoutGroup>();
            slotLayout.spacing = 12f;
            slotLayout.childForceExpandWidth = true;
            slotLayout.childForceExpandHeight = false;
            slotLayout.childControlHeight = true;
            slotLayout.childControlWidth = true;

            // Attach SaveSlotUI
            var saveSlotUI = loadPanel.AddComponent<UI.SaveSlotUI>();

            // Back button
            Button loadBack = CreateMenuButton(loadPanel.transform, "LoadBackButton", "BACK");
            SetAnchored(loadBack.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 60f), new Vector2(200, 50));

            // Build the save slot prefab inline and assign
            GameObject slotPrefab = BuildSaveSlotPrefab();

            loadPanel.SetActive(false);

            // =================================================================
            // SETTINGS PANEL
            // =================================================================
            GameObject settingsPanel = CreatePanel(canvasGo.transform, "SettingsPanel",
                new Color(0.02f, 0.02f, 0.04f, 0.95f));
            Stretch(settingsPanel.GetComponent<RectTransform>());

            CreateTMP(settingsPanel.transform, "SettingsHeader", "SETTINGS", 42,
                TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f))
                .rectTransform.anchoredPosition = new Vector2(0, 280f);

            var settingsCtrl = settingsPanel.AddComponent<UI.SettingsMenuController>();

            // --- Settings content container ---
            GameObject settingsContent = CreateVerticalGroup(settingsPanel.transform,
                "SettingsContent", 14f);
            SetAnchored(settingsContent.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 20f), new Vector2(500, 400));

            // Volume sliders
            var (masterRow, masterSlider, masterLabel) =
                CreateLabeledSlider(settingsContent.transform, "MasterVolume", "Master Volume", 0f, 1f, 1f);
            var (musicRow, musicSlider, musicLabel) =
                CreateLabeledSlider(settingsContent.transform, "MusicVolume", "Music Volume", 0f, 1f, 0.8f);
            var (sfxRow, sfxSlider, sfxLabel) =
                CreateLabeledSlider(settingsContent.transform, "SFXVolume", "SFX Volume", 0f, 1f, 1f);

            // Sensitivity slider
            var (sensRow, sensSlider, sensLabel) =
                CreateLabeledSlider(settingsContent.transform, "Sensitivity", "Mouse Sensitivity", 0.1f, 10f, 2f);

            // Fullscreen toggle
            var (fsRow, fsToggle) = CreateLabeledToggle(settingsContent.transform,
                "FullscreenToggle", "Fullscreen", true);

            // VSync toggle
            var (vsRow, vsToggle) = CreateLabeledToggle(settingsContent.transform,
                "VSyncToggle", "VSync", true);

            // Resolution dropdown
            var (resRow, resDropdown) = CreateLabeledDropdown(settingsContent.transform,
                "ResolutionDropdown", "Resolution");

            // Quality dropdown
            var (qualRow, qualDropdown) = CreateLabeledDropdown(settingsContent.transform,
                "QualityDropdown", "Quality");

            // Buttons row
            GameObject settingsBtnRow = CreateHorizontalGroup(settingsPanel.transform,
                "SettingsButtons", 20f);
            SetAnchored(settingsBtnRow.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 60f), new Vector2(420, 50));

            Button applyBtn = CreateMenuButton(settingsBtnRow.transform, "ApplyButton", "APPLY");
            Button resetBtn = CreateMenuButton(settingsBtnRow.transform, "ResetButton", "RESET");
            Button settingsBack = CreateMenuButton(settingsBtnRow.transform, "SettingsBackButton", "BACK");

            settingsPanel.SetActive(false);

            // =================================================================
            // CONFIRM DIALOG
            // =================================================================
            GameObject confirmDialog = CreatePanel(canvasGo.transform, "ConfirmDialog",
                new Color(0f, 0f, 0f, 0.8f));
            Stretch(confirmDialog.GetComponent<RectTransform>());

            // Dialog box
            GameObject dialogBox = CreatePanel(confirmDialog.transform, "DialogBox",
                new Color(0.08f, 0.08f, 0.1f, 1f));
            SetAnchored(dialogBox.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400, 200));
            // Add outline
            var dialogOutline = dialogBox.AddComponent<Outline>();
            dialogOutline.effectColor = new Color(0.3f, 0.3f, 0.4f, 0.6f);
            dialogOutline.effectDistance = new Vector2(2, -2);

            var confirmTMP = CreateTMP(dialogBox.transform, "ConfirmText",
                "Are you sure?", 24, TextAlignmentOptions.Center,
                new Color(0.85f, 0.85f, 0.9f));
            SetAnchored(confirmTMP.rectTransform, new Vector2(0.5f, 0.7f),
                new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(350, 60));

            GameObject confirmBtnRow = CreateHorizontalGroup(dialogBox.transform,
                "ConfirmButtons", 30f);
            SetAnchored(confirmBtnRow.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f),
                Vector2.zero, new Vector2(280, 50));

            Button yesBtn = CreateMenuButton(confirmBtnRow.transform, "YesButton", "YES");
            Button noBtn = CreateMenuButton(confirmBtnRow.transform, "NoButton", "NO");

            confirmDialog.SetActive(false);

            // =================================================================
            // WIRE MainMenuController via SerializedObject
            // =================================================================
            SerializedObject menuSO = new SerializedObject(menuCtrl);

            menuSO.FindProperty("_mainPanel").objectReferenceValue = mainPanel;
            menuSO.FindProperty("_loadGamePanel").objectReferenceValue = loadPanel;
            menuSO.FindProperty("_settingsPanel").objectReferenceValue = settingsPanel;
            menuSO.FindProperty("_confirmDialog").objectReferenceValue = confirmDialog;

            menuSO.FindProperty("_newGameButton").objectReferenceValue = btnNew;
            menuSO.FindProperty("_loadGameButton").objectReferenceValue = btnLoad;
            menuSO.FindProperty("_settingsButton").objectReferenceValue = btnSettings;
            menuSO.FindProperty("_quitButton").objectReferenceValue = btnQuit;

            menuSO.FindProperty("_loadBackButton").objectReferenceValue = loadBack;
            menuSO.FindProperty("_settingsBackButton").objectReferenceValue = settingsBack;

            menuSO.FindProperty("_confirmText").objectReferenceValue = confirmTMP;
            menuSO.FindProperty("_confirmYesButton").objectReferenceValue = yesBtn;
            menuSO.FindProperty("_confirmNoButton").objectReferenceValue = noBtn;

            menuSO.FindProperty("_titleText").objectReferenceValue = titleTMP;
            menuSO.FindProperty("_versionText").objectReferenceValue = versionTMP;
            menuSO.FindProperty("_firstGameScene").stringValue = "TestRoom";

            menuSO.ApplyModifiedPropertiesWithoutUndo();

            // =================================================================
            // SAVE SYSTEM (for reading save metadata on main menu)
            // =================================================================
            var menuSaveSysGO = new GameObject("SaveSystem");
            var menuSaveSystem = menuSaveSysGO.AddComponent<Core.SaveLoad.SaveSystem>();
            // Disable auto-save on the main menu
            SerializedObject saveSysSO = new SerializedObject(menuSaveSystem);
            saveSysSO.FindProperty("_autoSaveEnabled").boolValue = false;
            saveSysSO.ApplyModifiedPropertiesWithoutUndo();

            // =================================================================
            // WIRE SaveSlotUI
            // =================================================================
            SerializedObject slotSO = new SerializedObject(saveSlotUI);
            slotSO.FindProperty("_slotContainer").objectReferenceValue = slotContainer.transform;
            slotSO.FindProperty("_slotPrefab").objectReferenceValue = slotPrefab;
            slotSO.FindProperty("_saveSystem").objectReferenceValue = menuSaveSystem;
            slotSO.FindProperty("_gameSceneName").stringValue = "TestRoom";
            slotSO.ApplyModifiedPropertiesWithoutUndo();

            // =================================================================
            // WIRE SettingsMenuController
            // =================================================================
            SerializedObject settSO = new SerializedObject(settingsCtrl);

            settSO.FindProperty("_masterVolumeSlider").objectReferenceValue = masterSlider;
            settSO.FindProperty("_musicVolumeSlider").objectReferenceValue = musicSlider;
            settSO.FindProperty("_sfxVolumeSlider").objectReferenceValue = sfxSlider;
            settSO.FindProperty("_masterVolumeLabel").objectReferenceValue = masterLabel;
            settSO.FindProperty("_musicVolumeLabel").objectReferenceValue = musicLabel;
            settSO.FindProperty("_sfxVolumeLabel").objectReferenceValue = sfxLabel;

            settSO.FindProperty("_sensitivitySlider").objectReferenceValue = sensSlider;
            settSO.FindProperty("_sensitivityLabel").objectReferenceValue = sensLabel;

            settSO.FindProperty("_resolutionDropdown").objectReferenceValue = resDropdown;
            settSO.FindProperty("_fullscreenToggle").objectReferenceValue = fsToggle;
            settSO.FindProperty("_vSyncToggle").objectReferenceValue = vsToggle;
            settSO.FindProperty("_qualityDropdown").objectReferenceValue = qualDropdown;

            settSO.FindProperty("_applyButton").objectReferenceValue = applyBtn;
            settSO.FindProperty("_resetButton").objectReferenceValue = resetBtn;

            settSO.ApplyModifiedPropertiesWithoutUndo();

            // =================================================================
            // SAVE SCENE
            // =================================================================
            string scenePath = "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            // Add to build settings if not present
            AddSceneToBuildSettings(scenePath, 0);

            Debug.Log($"[MainMenuBuilder] Scene saved to {scenePath} and added to Build Settings at index 0.");
        }

        // =====================================================================
        // BUILD SCENE LOADER PREFAB
        // =====================================================================

        [MenuItem("Fractured Echoes/Build SceneLoader Prefab", false, 101)]
        public static void BuildSceneLoaderPrefab()
        {
            // Root GO
            GameObject root = new GameObject("SceneLoader");
            var loader = root.AddComponent<UI.SceneLoader>();

            // Canvas for loading screen overlay
            GameObject canvasGo = new GameObject("LoadingCanvas");
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // on top of everything
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution =
                new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Loading screen CanvasGroup
            GameObject loadingScreen = CreatePanel(canvasGo.transform, "LoadingScreen",
                new Color(0.01f, 0.01f, 0.02f, 1f));
            Stretch(loadingScreen.GetComponent<RectTransform>());
            var canvasGroup = loadingScreen.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Title label
            CreateTMP(loadingScreen.transform, "LoadingLabel", "LOADING...", 36,
                TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.85f))
                .rectTransform.anchoredPosition = new Vector2(0, 60f);

            // Progress bar background
            GameObject barBg = CreatePanel(loadingScreen.transform, "ProgressBarBG",
                new Color(0.15f, 0.15f, 0.18f, 1f));
            SetAnchored(barBg.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f),
                Vector2.zero, new Vector2(500, 12));

            // Progress bar fill
            GameObject barFill = new GameObject("ProgressBarFill");
            barFill.transform.SetParent(barBg.transform, false);
            var fillRT = barFill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            var fillImage = barFill.AddComponent<Image>();
            fillImage.color = new Color(0.6f, 0.6f, 0.75f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;

            // Percentage text
            var pctTMP = CreateTMP(loadingScreen.transform, "PercentageText",
                "0%", 20, TextAlignmentOptions.Center,
                new Color(0.6f, 0.6f, 0.65f));
            pctTMP.rectTransform.anchoredPosition = new Vector2(0, -10f);

            // Tip text
            var tipTMP = CreateTMP(loadingScreen.transform, "TipText",
                "", 18, TextAlignmentOptions.Center,
                new Color(0.45f, 0.45f, 0.5f));
            tipTMP.rectTransform.anchoredPosition = new Vector2(0, -60f);

            // Wire SceneLoader
            SerializedObject loaderSO = new SerializedObject(loader);
            loaderSO.FindProperty("_loadingScreenGroup").objectReferenceValue = canvasGroup;
            loaderSO.FindProperty("_progressBar").objectReferenceValue = fillImage;
            loaderSO.FindProperty("_progressText").objectReferenceValue = pctTMP;
            loaderSO.FindProperty("_tipText").objectReferenceValue = tipTMP;

            // Default tips
            var tipsProp = loaderSO.FindProperty("_tips");
            tipsProp.arraySize = 4;
            tipsProp.GetArrayElementAtIndex(0).stringValue = "Not everything that disappears is gone...";
            tipsProp.GetArrayElementAtIndex(1).stringValue = "Listen carefully. The silence speaks.";
            tipsProp.GetArrayElementAtIndex(2).stringValue = "Memory is a fragile thing.";
            tipsProp.GetArrayElementAtIndex(3).stringValue = "Some doors only open from the inside.";

            loaderSO.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            string prefabPath = "Assets/Prefabs/UI/SceneLoader.prefab";
            EnsureFolderExists("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[MainMenuBuilder] SceneLoader prefab saved to {prefabPath}");
        }

        // =====================================================================
        // SAVE SLOT PREFAB BUILDER
        // =====================================================================

        private static GameObject BuildSaveSlotPrefab()
        {
            string prefabPath = "Assets/Prefabs/UI/SaveSlotEntry.prefab";
            EnsureFolderExists("Assets/Prefabs/UI");

            GameObject root = new GameObject("SaveSlotEntry");
            var rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(480, 90);

            var rootLayout = root.AddComponent<HorizontalLayoutGroup>();
            rootLayout.spacing = 10f;
            rootLayout.padding = new RectOffset(12, 12, 8, 8);
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;

            var rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);

            var layoutElem = root.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 90;

            // Slot label
            var slotLabel = CreateTMP(root.transform, "SlotLabel", "Slot 1", 20,
                TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.7f, 0.75f));
            slotLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 70;

            // Info label
            var infoLabel = CreateTMP(root.transform, "InfoLabel", "— Empty —", 16,
                TextAlignmentOptions.MidlineLeft, new Color(0.55f, 0.55f, 0.6f));
            infoLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Load button
            Button loadBtn = CreateMenuButton(root.transform, "LoadButton", "LOAD");
            loadBtn.GetComponent<LayoutElement>().preferredWidth = 80;

            // Delete button
            Button deleteBtn = CreateMenuButton(root.transform, "DeleteButton", "DEL");
            deleteBtn.GetComponent<LayoutElement>().preferredWidth = 60;
            // Red tint for delete
            var delColors = deleteBtn.colors;
            delColors.normalColor = new Color(0.35f, 0.12f, 0.12f);
            delColors.highlightedColor = new Color(0.5f, 0.15f, 0.15f);
            deleteBtn.colors = delColors;

            // Save prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[MainMenuBuilder] SaveSlotEntry prefab saved to {prefabPath}");
            return prefab;
        }

        // =====================================================================
        // UI HELPER FACTORIES
        // =====================================================================

        private static GameObject CreateCanvas(string name)
        {
            GameObject go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name,
            string text, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 60);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.enableAutoSizing = false;
            return tmp;
        }

        private static Button CreateMenuButton(Transform parent, string name, string label)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 50);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.15f, 1f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.12f, 0.12f, 0.15f);
            colors.highlightedColor = new Color(0.2f, 0.2f, 0.28f);
            colors.pressedColor = new Color(0.08f, 0.08f, 0.1f);
            colors.selectedColor = new Color(0.18f, 0.18f, 0.24f);
            colors.disabledColor = new Color(0.06f, 0.06f, 0.08f);
            btn.colors = colors;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            le.flexibleWidth = 1;

            // Text child
            var tmp = CreateTMP(go.transform, "Text", label, 22,
                TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.85f));
            Stretch(tmp.rectTransform);

            return btn;
        }

        private static GameObject CreateVerticalGroup(Transform parent, string name, float spacing)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            return go;
        }

        private static GameObject CreateHorizontalGroup(Transform parent, string name, float spacing)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            return go;
        }

        private static (GameObject row, Slider slider, TextMeshProUGUI valueLabel)
            CreateLabeledSlider(Transform parent, string name, string label,
                float min, float max, float defaultVal)
        {
            GameObject row = new GameObject(name + "Row");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            row.AddComponent<LayoutElement>().preferredHeight = 36;

            // Label
            var labelTMP = CreateTMP(row.transform, name + "Label", label, 18,
                TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.7f, 0.75f));
            labelTMP.gameObject.AddComponent<LayoutElement>().preferredWidth = 180;

            // Slider
            GameObject sliderGo = CreateSliderGO(row.transform, name + "Slider", min, max, defaultVal);
            sliderGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Value label
            var valTMP = CreateTMP(row.transform, name + "Value",
                $"{Mathf.RoundToInt(defaultVal * 100)}%", 18,
                TextAlignmentOptions.MidlineRight, new Color(0.6f, 0.6f, 0.65f));
            valTMP.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;

            return (row, sliderGo.GetComponent<Slider>(), valTMP);
        }

        private static (GameObject row, Toggle toggle)
            CreateLabeledToggle(Transform parent, string name, string label, bool defaultVal)
        {
            GameObject row = new GameObject(name + "Row");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            row.AddComponent<LayoutElement>().preferredHeight = 36;

            // Label
            var labelTMP = CreateTMP(row.transform, name + "Label", label, 18,
                TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.7f, 0.75f));
            labelTMP.gameObject.AddComponent<LayoutElement>().preferredWidth = 180;

            // Toggle
            GameObject toggleGo = new GameObject(name);
            toggleGo.transform.SetParent(row.transform, false);
            toggleGo.AddComponent<RectTransform>();
            toggleGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Background
            GameObject bg = CreatePanel(toggleGo.transform, "Background",
                new Color(0.15f, 0.15f, 0.18f));
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.sizeDelta = new Vector2(26, 26);
            bgRT.anchorMin = new Vector2(0, 0.5f);
            bgRT.anchorMax = new Vector2(0, 0.5f);
            bgRT.anchoredPosition = new Vector2(13, 0);

            // Checkmark
            GameObject check = CreatePanel(bg.transform, "Checkmark",
                new Color(0.6f, 0.6f, 0.75f));
            var checkRT = check.GetComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.15f, 0.15f);
            checkRT.anchorMax = new Vector2(0.85f, 0.85f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;

            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.isOn = defaultVal;
            toggle.graphic = check.GetComponent<Image>();
            toggle.targetGraphic = bg.GetComponent<Image>();

            return (row, toggle);
        }

        private static (GameObject row, TMP_Dropdown dropdown)
            CreateLabeledDropdown(Transform parent, string name, string label)
        {
            GameObject row = new GameObject(name + "Row");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            row.AddComponent<LayoutElement>().preferredHeight = 36;

            // Label
            var labelTMP = CreateTMP(row.transform, name + "Label", label, 18,
                TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.7f, 0.75f));
            labelTMP.gameObject.AddComponent<LayoutElement>().preferredWidth = 180;

            // Dropdown
            GameObject ddGo = new GameObject(name);
            ddGo.transform.SetParent(row.transform, false);
            var ddRT = ddGo.AddComponent<RectTransform>();
            ddGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            var ddImg = ddGo.AddComponent<Image>();
            ddImg.color = new Color(0.15f, 0.15f, 0.18f);

            // Caption text
            var captionTMP = CreateTMP(ddGo.transform, "Label", "--", 16,
                TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.7f, 0.75f));
            var captionRT = captionTMP.rectTransform;
            captionRT.anchorMin = Vector2.zero;
            captionRT.anchorMax = Vector2.one;
            captionRT.offsetMin = new Vector2(10, 0);
            captionRT.offsetMax = new Vector2(-25, 0);

            // Template
            GameObject template = CreatePanel(ddGo.transform, "Template",
                new Color(0.1f, 0.1f, 0.13f));
            var templateRT = template.GetComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.pivot = new Vector2(0.5f, 1f);
            templateRT.sizeDelta = new Vector2(0, 150);
            template.AddComponent<ScrollRect>();

            GameObject viewport = CreatePanel(template.transform, "Viewport",
                new Color(0.1f, 0.1f, 0.13f, 1f));
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = new Vector2(0, 28);

            GameObject itemTpl = new GameObject("Item");
            itemTpl.transform.SetParent(content.transform, false);
            var itemRT = itemTpl.AddComponent<RectTransform>();
            itemRT.anchorMin = new Vector2(0, 0.5f);
            itemRT.anchorMax = new Vector2(1, 0.5f);
            itemRT.sizeDelta = new Vector2(0, 28);
            itemTpl.AddComponent<Toggle>();

            var itemLabel = CreateTMP(itemTpl.transform, "Item Label", "", 16,
                TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.7f, 0.75f));
            Stretch(itemLabel.rectTransform);

            // Wire ScrollRect
            var scroll = template.GetComponent<ScrollRect>();
            scroll.content = contentRT;
            scroll.viewport = viewport.GetComponent<RectTransform>();

            template.SetActive(false);

            // TMP_Dropdown
            var dropdown = ddGo.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = ddImg;
            dropdown.template = templateRT;
            dropdown.captionText = captionTMP;
            dropdown.itemText = itemLabel;

            return (row, dropdown);
        }

        private static GameObject CreateSliderGO(Transform parent, string name,
            float min, float max, float defaultVal)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 20);

            // Background
            GameObject bg = CreatePanel(go.transform, "Background",
                new Color(0.12f, 0.12f, 0.15f));
            Stretch(bg.GetComponent<RectTransform>());

            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5, 0);
            fillAreaRT.offsetMax = new Vector2(-5, 0);

            GameObject fill = CreatePanel(fillArea.transform, "Fill",
                new Color(0.5f, 0.5f, 0.65f));
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.sizeDelta = Vector2.zero;

            // Handle slide area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRT = handleArea.AddComponent<RectTransform>();
            handleAreaRT.anchorMin = new Vector2(0, 0);
            handleAreaRT.anchorMax = new Vector2(1, 1);
            handleAreaRT.offsetMin = new Vector2(10, 0);
            handleAreaRT.offsetMax = new Vector2(-10, 0);

            GameObject handle = CreatePanel(handleArea.transform, "Handle",
                new Color(0.7f, 0.7f, 0.8f));
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(16, 0);

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRT;
            slider.handleRect = handleRT;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultVal;
            slider.targetGraphic = handle.GetComponent<Image>();

            return go;
        }

        // =====================================================================
        // RECT TRANSFORM HELPERS
        // =====================================================================

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(RectTransform rt, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        // =====================================================================
        // BUILD SETTINGS HELPER
        // =====================================================================

        private static void AddSceneToBuildSettings(string scenePath, int targetIndex)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            // Remove if already present
            scenes.RemoveAll(s => s.path == scenePath);

            // Insert at target index
            var entry = new EditorBuildSettingsScene(scenePath, true);
            if (targetIndex >= scenes.Count)
                scenes.Add(entry);
            else
                scenes.Insert(targetIndex, entry);

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // =====================================================================
        // FOLDER HELPER
        // =====================================================================

        private static void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string[] parts = folderPath.Split('/');
                string current = parts[0]; // "Assets"
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }
    }
}
