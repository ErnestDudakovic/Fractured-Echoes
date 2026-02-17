// ============================================================================
// TestRoomBuilder.cs — Editor utility
// Creates a multi-room test scene with all game mechanics wired up:
//   Room 1: Movement & Camera test (open area with ramps/pillars)
//   Room 2: Interaction & Inventory test (pickups, inspectables)
//   Room 3: Puzzle test (lever puzzle with door unlock)
//   Room 4: Horror test (lighting, scripted events, environment phases)
//
// Run via: Fractured Echoes → Build Test Scene
// ============================================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using FracturedEchoes.Player;
using FracturedEchoes.Interaction;
using FracturedEchoes.InventorySystem;
using FracturedEchoes.Puzzle;
using FracturedEchoes.Environment;
using FracturedEchoes.Audio;
using FracturedEchoes.Lighting;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using FracturedEchoes.Core;
using FracturedEchoes.Core.Events;
using FracturedEchoes.Core.SaveLoad;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.UI;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.Editor
{
    public static class TestRoomBuilder
    {
        // Layout constants
        private const float ROOM_SIZE = 12f;
        private const float WALL_HEIGHT = 5f;
        private const float WALL_THICKNESS = 0.3f;
        private const float CORRIDOR_WIDTH = 3f;
        private const float CORRIDOR_LENGTH = 6f;

        // Material cache (created at runtime)
        private static Material _floorMat;
        private static Material _wallMat;
        private static Material _ceilingMat;
        private static Material _accentMat;
        private static Material _darkMat;
        private static Material _interactMat;
        private static Material _puzzleMat;

        // SO asset folder 
        private const string SO_ROOT = "Assets/Resources/TestData";

        [MenuItem("Fractured Echoes/Build Test Scene", false, 102)]
        public static void BuildTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMaterials();
            EnsureFolder(SO_ROOT);

            // --- Root containers ---
            GameObject geometry = new GameObject("--- GEOMETRY ---");
            GameObject systems = new GameObject("--- SYSTEMS ---");
            GameObject lighting = new GameObject("--- LIGHTING ---");
            GameObject interactables = new GameObject("--- INTERACTABLES ---");
            GameObject puzzles = new GameObject("--- PUZZLES ---");
            GameObject audio = new GameObject("--- AUDIO ---");
            GameObject events = new GameObject("--- EVENTS ---");

            // =================================================================
            // SCRIPTABLE OBJECTS (created as assets)
            // =================================================================
            var soAssets = CreateScriptableObjects();

            // =================================================================
            // ROOM GEOMETRY
            // =================================================================
            Vector3 room1Pos = Vector3.zero;
            Vector3 room2Pos = new Vector3(ROOM_SIZE + CORRIDOR_LENGTH, 0, 0);
            Vector3 room3Pos = new Vector3(0, 0, -(ROOM_SIZE + CORRIDOR_LENGTH));
            Vector3 room4Pos = new Vector3(ROOM_SIZE + CORRIDOR_LENGTH, 0, -(ROOM_SIZE + CORRIDOR_LENGTH));

            BuildRoom(geometry.transform, "Room1_Movement", room1Pos, "Movement & Camera",
                openEast: true, openSouth: true);
            BuildRoom(geometry.transform, "Room2_Interaction", room2Pos, "Interaction & Inventory",
                openWest: true, openSouth: true);
            BuildRoom(geometry.transform, "Room3_Puzzle", room3Pos, "Puzzle Room",
                openNorth: true, openEast: true);
            BuildRoom(geometry.transform, "Room4_Horror", room4Pos, "Horror Room",
                openWest: true, openNorth: true);

            // Corridors connecting rooms
            BuildCorridor(geometry.transform, "Corridor_1to2",
                room1Pos + new Vector3(ROOM_SIZE / 2f, 0, 0),
                Vector3.right, CORRIDOR_LENGTH);
            BuildCorridor(geometry.transform, "Corridor_1to3",
                room1Pos + new Vector3(0, 0, -ROOM_SIZE / 2f),
                Vector3.back, CORRIDOR_LENGTH);
            BuildCorridor(geometry.transform, "Corridor_2to4",
                room2Pos + new Vector3(0, 0, -ROOM_SIZE / 2f),
                Vector3.back, CORRIDOR_LENGTH);
            BuildCorridor(geometry.transform, "Corridor_3to4",
                room3Pos + new Vector3(ROOM_SIZE / 2f, 0, 0),
                Vector3.right, CORRIDOR_LENGTH);

            // Room 1 extras: ramp + pillars for movement testing
            BuildRamp(geometry.transform, room1Pos + new Vector3(-3f, 0, -3f));
            BuildPillar(geometry.transform, room1Pos + new Vector3(3f, 0, 2f));
            BuildPillar(geometry.transform, room1Pos + new Vector3(-2f, 0, 3f));
            BuildStairs(geometry.transform, room1Pos + new Vector3(3f, 0, -3f));

            // =================================================================
            // PLAYER
            // =================================================================
            GameObject player = BuildPlayer(room1Pos + new Vector3(0, 1f, 0));

            // =================================================================
            // CAMERA (scene view helper — HDRP needs a camera)
            // =================================================================
            // Player has its own camera, but we keep the scene camera reference

            // =================================================================
            // GLOBAL LIGHTING
            // =================================================================
            BuildGlobalLighting(lighting.transform);

            // Room lights
            Light room1Light = BuildRoomLight(lighting.transform, "Light_Room1",
                room1Pos + new Vector3(0, WALL_HEIGHT - 0.3f, 0), 1.5f);
            Light room2Light = BuildRoomLight(lighting.transform, "Light_Room2",
                room2Pos + new Vector3(0, WALL_HEIGHT - 0.3f, 0), 1.2f);
            Light room3Light = BuildRoomLight(lighting.transform, "Light_Room3",
                room3Pos + new Vector3(0, WALL_HEIGHT - 0.3f, 0), 1.0f);
            Light room4Light = BuildRoomLight(lighting.transform, "Light_Room4",
                room4Pos + new Vector3(0, WALL_HEIGHT - 0.3f, 0), 0.6f);

            // Flickering light in Room 4
            var flickerLight = BuildRoomLight(lighting.transform, "Light_Room4_Flicker",
                room4Pos + new Vector3(3f, WALL_HEIGHT - 0.5f, -2f), 0.8f);
            flickerLight.color = new Color(1f, 0.85f, 0.6f);
            var lightFlicker = flickerLight.gameObject.AddComponent<LightFlicker>();
            // Set via SO
            SerializedObject flickerSO = new SerializedObject(lightFlicker);
            flickerSO.FindProperty("_flickerOnEnable").boolValue = true;
            flickerSO.FindProperty("_loop").boolValue = true;
            flickerSO.FindProperty("_minIntensity").floatValue = 0.2f;
            flickerSO.FindProperty("_maxIntensity").floatValue = 1.3f;
            flickerSO.ApplyModifiedPropertiesWithoutUndo();

            // Corridor lights
            BuildCorridorLight(lighting.transform, room1Pos + new Vector3(ROOM_SIZE / 2f + CORRIDOR_LENGTH / 2f, WALL_HEIGHT - 0.5f, 0));
            BuildCorridorLight(lighting.transform, room1Pos + new Vector3(0, WALL_HEIGHT - 0.5f, -(ROOM_SIZE / 2f + CORRIDOR_LENGTH / 2f)));
            BuildCorridorLight(lighting.transform, room2Pos + new Vector3(0, WALL_HEIGHT - 0.5f, -(ROOM_SIZE / 2f + CORRIDOR_LENGTH / 2f)));
            BuildCorridorLight(lighting.transform, room3Pos + new Vector3(ROOM_SIZE / 2f + CORRIDOR_LENGTH / 2f, WALL_HEIGHT - 0.5f, 0));

            // =================================================================
            // INTERACTION SYSTEM + UI (on player)
            // =================================================================
            // Already built inside BuildPlayer

            // =================================================================
            // ROOM 2: INTERACTABLE OBJECTS (pickups, inspectables)
            // =================================================================
            var pickup1 = BuildPickup(interactables.transform, "Pickup_Key",
                room2Pos + new Vector3(-3f, 0.7f, 2f), soAssets.keyItem, "Key");
            var pickup2 = BuildPickup(interactables.transform, "Pickup_Note",
                room2Pos + new Vector3(2f, 0.8f, -2f), soAssets.noteItem, "Note");
            var pickup3 = BuildPickup(interactables.transform, "Pickup_Photo",
                room2Pos + new Vector3(0f, 0.6f, 3f), soAssets.photoItem, "Photo");

            // Tables for items to sit on
            BuildTable(interactables.transform, room2Pos + new Vector3(-3f, 0, 2f));
            BuildTable(interactables.transform, room2Pos + new Vector3(2f, 0, -2f));
            BuildTable(interactables.transform, room2Pos + new Vector3(0f, 0, 3f));

            // A generic interactable (button on wall)
            BuildWallButton(interactables.transform, "WallButton_Test",
                room2Pos + new Vector3(-ROOM_SIZE / 2f + 0.2f, 1.5f, 0),
                Quaternion.Euler(0, 90, 0));

            // =================================================================
            // ROOM 3: PUZZLE (3-lever sequence)
            // =================================================================
            BuildLeverPuzzle(puzzles.transform, room3Pos, soAssets);

            // Door that opens on puzzle solve
            BuildDoor(geometry.transform, "PuzzleDoor",
                room3Pos + new Vector3(ROOM_SIZE / 2f, 0, 0));

            // Locked door in Room 2 (requires Rusty Key)
            BuildLockedDoor(geometry.transform, "LockedDoor_Room2",
                room2Pos + new Vector3(0, 0, -ROOM_SIZE / 2f + 0.5f),
                soAssets.keyItem);

            // =================================================================
            // ROOM 4: HORROR — scripted events, environment phases, audio
            // =================================================================
            // Appearing object (ghost object)
            GameObject ghostObj = BuildGhostObject(events.transform,
                room4Pos + new Vector3(3f, 0, 3f));
            ghostObj.SetActive(false);

            // Alternate room variant (for environment rearrange)
            GameObject altObject = BuildAlternateDecor(events.transform,
                room4Pos + new Vector3(-3f, 0, -3f));
            altObject.SetActive(false);

            // Event trigger zone at room entrance
            BuildEventTriggerZone(events.transform, "TriggerZone_Room4",
                room4Pos + new Vector3(0, WALL_HEIGHT / 2f, ROOM_SIZE / 2f - 1f),
                new Vector3(CORRIDOR_WIDTH, WALL_HEIGHT, 1f));

            // =================================================================
            // CORE SYSTEMS
            // =================================================================
            var coreGO = BuildCoreSystems(systems.transform, soAssets);

            // =================================================================
            // AUDIO
            // =================================================================
            BuildAudioSystem(audio.transform, room4Pos, soAssets);

            // =================================================================
            // HUD CANVAS (interaction prompts, crosshair)
            // =================================================================
            BuildHUD(player.transform);

            // =================================================================
            // SAVE STATION (Room 1 — glowing crystal save point)
            // =================================================================
            BuildSaveStation(interactables.transform,
                room1Pos + new Vector3(4f, 0, 4f),
                coreGO.GetComponent<SaveSystem>());

            // =================================================================
            // DEBUG HELP TEXT
            // =================================================================
            BuildDebugUI();

            // =================================================================
            // SAVE & REGISTER SCENE
            // =================================================================
            string scenePath = "Assets/Scenes/Testing/TestRoom.unity";
            EnsureFolder("Assets/Scenes/Testing");
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            AddSceneToBuildSettings(scenePath);

            Debug.Log("[TestRoomBuilder] Test scene built and saved to " + scenePath);
        }

        // =====================================================================
        // SCRIPTABLE OBJECT CREATION
        // =====================================================================

        private struct SOAssets
        {
            public ItemData keyItem, noteItem, photoItem;
            public PuzzleData leverPuzzleData;
            public EnvironmentPhaseData phaseNormal, phaseUnsettling, phaseDark;
            public GameEvent onPuzzleSolved, onPhaseChanged, onGenericInteract;
            public GameEventString onItemPickup, onFocusChanged;
            public GameEventInt onPhaseInt;
            public ScriptedEventData flickerEvent, appearEvent;
        }

        private static SOAssets CreateScriptableObjects()
        {
            SOAssets a = new SOAssets();

            // --- Events ---
            a.onPuzzleSolved = GetOrCreateSO<GameEvent>("OnPuzzleSolved");
            a.onPhaseChanged = GetOrCreateSO<GameEvent>("OnPhaseChanged");
            a.onGenericInteract = GetOrCreateSO<GameEvent>("OnGenericInteract");
            a.onItemPickup = GetOrCreateSO<GameEventString>("OnItemPickup");
            a.onFocusChanged = GetOrCreateSO<GameEventString>("OnFocusChanged");
            a.onPhaseInt = GetOrCreateSO<GameEventInt>("OnPhaseInt");

            // --- Items ---
            a.keyItem = GetOrCreateSO<ItemData>("Item_Key");
            a.keyItem.itemID = "key_room3";
            a.keyItem.displayName = "Rusty Key";
            a.keyItem.description = "An old key. The teeth are worn but might still work.";
            a.keyItem.itemType = ItemType.Key;
            a.keyItem.canUseOnEnvironment = true;
            EditorUtility.SetDirty(a.keyItem);

            a.noteItem = GetOrCreateSO<ItemData>("Item_Note");
            a.noteItem.itemID = "note_1";
            a.noteItem.displayName = "Torn Note";
            a.noteItem.description = "A crumpled piece of paper. The handwriting is frantic.";
            a.noteItem.itemType = ItemType.Note;
            EditorUtility.SetDirty(a.noteItem);

            a.photoItem = GetOrCreateSO<ItemData>("Item_Photo");
            a.photoItem.itemID = "photo_family";
            a.photoItem.displayName = "Family Photo";
            a.photoItem.description = "A faded photograph. Some faces are scratched out.";
            a.photoItem.itemType = ItemType.Photo;
            EditorUtility.SetDirty(a.photoItem);

            // --- Puzzle ---
            a.leverPuzzleData = GetOrCreateSO<PuzzleData>("Puzzle_Levers");
            a.leverPuzzleData.puzzleID = "lever_puzzle_01";
            a.leverPuzzleData.displayName = "Three Lever Puzzle";
            a.leverPuzzleData.hintText = "The correct order is engraved somewhere...";
            a.leverPuzzleData.solutionSequence = new string[] { "lever_A", "lever_C", "lever_B" };
            a.leverPuzzleData.resetOnFailure = true;
            a.leverPuzzleData.maxAttempts = 0; // unlimited
            a.leverPuzzleData.onCompletedEvent = a.onPuzzleSolved;
            EditorUtility.SetDirty(a.leverPuzzleData);

            // --- Environment Phases ---
            a.phaseNormal = GetOrCreateSO<EnvironmentPhaseData>("Phase_Normal");
            a.phaseNormal.phaseName = "Normal";
            a.phaseNormal.phaseIndex = 0;
            a.phaseNormal.ambientColor = new Color(0.3f, 0.3f, 0.35f);
            a.phaseNormal.ambientIntensity = 0.8f;
            a.phaseNormal.fogColor = new Color(0.15f, 0.15f, 0.18f);
            a.phaseNormal.fogDensity = 0.01f;
            a.phaseNormal.transitionDuration = 3f;
            EditorUtility.SetDirty(a.phaseNormal);

            a.phaseUnsettling = GetOrCreateSO<EnvironmentPhaseData>("Phase_Unsettling");
            a.phaseUnsettling.phaseName = "Unsettling";
            a.phaseUnsettling.phaseIndex = 1;
            a.phaseUnsettling.ambientColor = new Color(0.2f, 0.18f, 0.22f);
            a.phaseUnsettling.ambientIntensity = 0.5f;
            a.phaseUnsettling.fogColor = new Color(0.1f, 0.08f, 0.12f);
            a.phaseUnsettling.fogDensity = 0.025f;
            a.phaseUnsettling.transitionDuration = 5f;
            EditorUtility.SetDirty(a.phaseUnsettling);

            a.phaseDark = GetOrCreateSO<EnvironmentPhaseData>("Phase_Dark");
            a.phaseDark.phaseName = "Dark";
            a.phaseDark.phaseIndex = 2;
            a.phaseDark.ambientColor = new Color(0.05f, 0.03f, 0.08f);
            a.phaseDark.ambientIntensity = 0.2f;
            a.phaseDark.fogColor = new Color(0.02f, 0.01f, 0.04f);
            a.phaseDark.fogDensity = 0.05f;
            a.phaseDark.transitionDuration = 8f;
            EditorUtility.SetDirty(a.phaseDark);

            // --- Scripted Events ---
            a.flickerEvent = GetOrCreateSO<ScriptedEventData>("Event_Flicker");
            a.flickerEvent.eventID = "flicker_room4";
            a.flickerEvent.editorDescription = "Room 4 lights flicker when player enters";
            a.flickerEvent.triggerType = TriggerType.EnterArea;
            a.flickerEvent.eventType = ScriptedEventType.LightFlicker;
            a.flickerEvent.effectDuration = 3f;
            a.flickerEvent.oneTimeOnly = false;
            EditorUtility.SetDirty(a.flickerEvent);

            a.appearEvent = GetOrCreateSO<ScriptedEventData>("Event_GhostAppear");
            a.appearEvent.eventID = "ghost_appear";
            a.appearEvent.editorDescription = "Ghost figure appears in Room 4 corner";
            a.appearEvent.triggerType = TriggerType.EnterArea;
            a.appearEvent.eventType = ScriptedEventType.ObjectAppear;
            a.appearEvent.effectDuration = 2f;
            a.appearEvent.oneTimeOnly = true;
            a.appearEvent.chainedEventID = "flicker_room4";
            EditorUtility.SetDirty(a.appearEvent);

            AssetDatabase.SaveAssets();
            return a;
        }

        // =====================================================================
        // PLAYER
        // =====================================================================

        private static GameObject BuildPlayer(Vector3 spawnPos)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");
            player.transform.position = spawnPos;

            // CharacterController (required by FirstPersonMotor)
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);

            // Camera holder
            GameObject camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(player.transform, false);
            camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            // Main Camera
            GameObject camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(camHolder.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();

            // Player scripts
            player.AddComponent<FirstPersonMotor>();
            var fpCam = player.AddComponent<FirstPersonCamera>();
            player.AddComponent<FirstPersonController>();

            // Wire camera holder
            SerializedObject camSO = new SerializedObject(fpCam);
            camSO.FindProperty("_cameraHolder").objectReferenceValue = camHolder.transform;
            camSO.ApplyModifiedPropertiesWithoutUndo();

            // Inspection point (child of camera for inspect mode)
            GameObject inspectPoint = new GameObject("InspectionPoint");
            inspectPoint.transform.SetParent(camGo.transform, false);
            inspectPoint.transform.localPosition = new Vector3(0, 0, 0.5f);

            // InspectItemController
            var inspectCtrl = player.AddComponent<InspectItemController>();
            SerializedObject inspSO = new SerializedObject(inspectCtrl);
            inspSO.FindProperty("_inspectionPoint").objectReferenceValue = inspectPoint.transform;
            inspSO.FindProperty("_inspectionCamera").objectReferenceValue = cam;
            inspSO.ApplyModifiedPropertiesWithoutUndo();

            // InventoryManager
            var invMgr = player.AddComponent<InventoryManager>();
            SerializedObject invSO = new SerializedObject(invMgr);
            invSO.FindProperty("_inspectController").objectReferenceValue = inspectCtrl;
            invSO.ApplyModifiedPropertiesWithoutUndo();

            // InventoryUI (self-building grid interface)
            player.AddComponent<InventoryUI>();

            // InteractionSystem
            var interSys = player.AddComponent<InteractionSystem>();
            SerializedObject interSO = new SerializedObject(interSys);
            interSO.FindProperty("_cameraTransform").objectReferenceValue = camGo.transform;
            interSO.FindProperty("_interactionRange").floatValue = 3f;
            interSO.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        // =====================================================================
        // HUD
        // =====================================================================

        private static void BuildHUD(Transform playerTransform)
        {
            GameObject canvasGo = new GameObject("HUDCanvas");
            canvasGo.transform.SetParent(playerTransform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Crosshair
            GameObject crosshairGo = new GameObject("Crosshair");
            crosshairGo.transform.SetParent(canvasGo.transform, false);
            var crossRT = crosshairGo.AddComponent<RectTransform>();
            crossRT.anchorMin = new Vector2(0.5f, 0.5f);
            crossRT.anchorMax = new Vector2(0.5f, 0.5f);
            crossRT.sizeDelta = new Vector2(8, 8);
            var crossImg = crosshairGo.AddComponent<Image>();
            crossImg.color = new Color(1f, 1f, 1f, 0.6f);

            // Prompt text
            GameObject promptGo = new GameObject("PromptText");
            promptGo.transform.SetParent(canvasGo.transform, false);
            var promptRT = promptGo.AddComponent<RectTransform>();
            promptRT.anchorMin = new Vector2(0.5f, 0.35f);
            promptRT.anchorMax = new Vector2(0.5f, 0.35f);
            promptRT.sizeDelta = new Vector2(400, 40);
            // Use Unity UI Text (InteractionUI expects Text not TMP)
            var promptText = promptGo.AddComponent<Text>();
            promptText.text = "";
            promptText.fontSize = 20;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            // Need a font
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // InteractionUI component
            var interUI = canvasGo.AddComponent<InteractionUI>();
            SerializedObject uiSO = new SerializedObject(interUI);
            uiSO.FindProperty("_promptText").objectReferenceValue = promptText;
            uiSO.FindProperty("_crosshair").objectReferenceValue = crossImg;
            uiSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // CORE SYSTEMS (GameStateManager, SaveSystem)
        // =====================================================================

        private static GameObject BuildCoreSystems(Transform parent, SOAssets so)
        {
            GameObject go = new GameObject("CoreSystems");
            go.transform.SetParent(parent, false);

            // SaveSystem
            var saveSys = go.AddComponent<SaveSystem>();

            // GameStateManager
            var gsm = go.AddComponent<GameStateManager>();
            SerializedObject gsmSO = new SerializedObject(gsm);
            gsmSO.FindProperty("_saveSystem").objectReferenceValue = saveSys;
            gsmSO.FindProperty("_onGameStateChanged").objectReferenceValue = so.onPhaseChanged;
            var locNames = gsmSO.FindProperty("_locationSceneNames");
            locNames.arraySize = 1;
            locNames.GetArrayElementAtIndex(0).stringValue = "TestRoom";
            gsmSO.ApplyModifiedPropertiesWithoutUndo();

            // EnvironmentStateManager
            var envMgr = go.AddComponent<EnvironmentStateManager>();
            SerializedObject envSO = new SerializedObject(envMgr);
            var phases = envSO.FindProperty("_phases");
            phases.arraySize = 3;
            phases.GetArrayElementAtIndex(0).objectReferenceValue = so.phaseNormal;
            phases.GetArrayElementAtIndex(1).objectReferenceValue = so.phaseUnsettling;
            phases.GetArrayElementAtIndex(2).objectReferenceValue = so.phaseDark;
            envSO.FindProperty("_onPhaseChanged").objectReferenceValue = so.onPhaseInt;
            envSO.ApplyModifiedPropertiesWithoutUndo();

            // ScriptedEventController (for Room 4 events)
            var sec = go.AddComponent<ScriptedEventController>();

            // PauseMenuController (in-game pause menu + settings)
            go.AddComponent<PauseMenuController>();

            // EventSystem (required for UI button clicks)
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.transform.SetParent(parent, false);
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<InputSystemUIInputModule>();

            // Firebase: Initializer → Auth → CloudSave (chain auto-connects)
            go.AddComponent<FirebaseInitializer>();
            go.AddComponent<FirebaseAuthManager>();

            var cloudSave = go.AddComponent<CloudSaveManager>();
            SerializedObject cloudSO = new SerializedObject(cloudSave);
            cloudSO.FindProperty("_saveSystem").objectReferenceValue = saveSys;
            cloudSO.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        // =====================================================================
        // AUDIO SYSTEM
        // =====================================================================

        private static void BuildAudioSystem(Transform parent, Vector3 room4Pos, SOAssets so)
        {
            GameObject go = new GameObject("AudioManager");
            go.transform.SetParent(parent, false);
            go.AddComponent<AudioManager>();

            // Ambient zone for Room 4
            GameObject zoneGo = new GameObject("AmbientZone_Room4");
            zoneGo.transform.SetParent(parent, false);
            zoneGo.transform.position = room4Pos + new Vector3(0, WALL_HEIGHT / 2f, 0);

            var zoneCol = zoneGo.AddComponent<BoxCollider>();
            zoneCol.isTrigger = true;
            zoneCol.size = new Vector3(ROOM_SIZE, WALL_HEIGHT, ROOM_SIZE);

            var ambZone = zoneGo.AddComponent<AmbientZone>();
            SerializedObject azSO = new SerializedObject(ambZone);
            azSO.FindProperty("_audioManager").objectReferenceValue =
                go.GetComponent<AudioManager>();
            azSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // ROOM 3: LEVER PUZZLE
        // =====================================================================

        private static void BuildLeverPuzzle(Transform parent, Vector3 roomPos, SOAssets so)
        {
            GameObject puzzleRoot = new GameObject("LeverPuzzle");
            puzzleRoot.transform.SetParent(parent, false);
            puzzleRoot.transform.position = roomPos;

            // PuzzleController
            var pc = puzzleRoot.AddComponent<PuzzleController>();
            SerializedObject pcSO = new SerializedObject(pc);
            pcSO.FindProperty("_puzzleData").objectReferenceValue = so.leverPuzzleData;
            pcSO.FindProperty("_onCompleted").objectReferenceValue = so.onPuzzleSolved;
            pcSO.FindProperty("_saveID").stringValue = "lever_puzzle_01";
            pcSO.ApplyModifiedPropertiesWithoutUndo();

            // SimpleLeverPuzzle visualiser
            var slp = puzzleRoot.AddComponent<SimpleLeverPuzzle>();

            // Create 3 levers on the back wall
            Transform[] leverHandles = new Transform[3];
            string[] leverIDs = { "lever_A", "lever_B", "lever_C" };
            float[] xOffsets = { -3f, 0f, 3f };

            for (int i = 0; i < 3; i++)
            {
                Vector3 basePos = roomPos + new Vector3(xOffsets[i], 1.2f, -ROOM_SIZE / 2f + 0.5f);

                // Lever base
                GameObject leverBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leverBase.name = $"LeverBase_{leverIDs[i]}";
                leverBase.transform.SetParent(puzzleRoot.transform, false);
                leverBase.transform.position = basePos;
                leverBase.transform.localScale = new Vector3(0.3f, 0.3f, 0.15f);
                leverBase.GetComponent<Renderer>().sharedMaterial = _accentMat;

                // Lever handle (the part that rotates)
                GameObject leverHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leverHandle.name = $"LeverHandle_{leverIDs[i]}";
                leverHandle.transform.SetParent(leverBase.transform, false);
                leverHandle.transform.localPosition = new Vector3(0, 1.5f, 0);
                leverHandle.transform.localScale = new Vector3(0.3f, 2f, 0.3f);
                leverHandle.GetComponent<Renderer>().sharedMaterial = _puzzleMat;

                leverHandles[i] = leverHandle.transform;

                // PuzzleInteractable on the lever
                var pi = leverBase.AddComponent<PuzzleInteractable>();
                // Need to set via serialized fields
                SerializedObject piSO = new SerializedObject(pi);
                piSO.FindProperty("_puzzleController").objectReferenceValue = pc;
                piSO.FindProperty("_puzzleInput").stringValue = leverIDs[i];
                piSO.FindProperty("_promptText").stringValue = $"Pull {leverIDs[i].Replace("lever_", "Lever ")}";
                piSO.FindProperty("_interactionType").enumValueIndex = 4; // Puzzle
                piSO.FindProperty("_autoUnlock").boolValue = true;
                piSO.ApplyModifiedPropertiesWithoutUndo();

                // Add label above lever
                BuildFloatingLabel(parent, $"Label_{leverIDs[i]}",
                    basePos + new Vector3(0, 1f, -0.3f), leverIDs[i].Replace("lever_", ""));
            }

            // Wire lever handles to SimpleLeverPuzzle  
            SerializedObject slpSO = new SerializedObject(slp);
            var handlesProp = slpSO.FindProperty("_leverHandles");
            handlesProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                handlesProp.GetArrayElementAtIndex(i).objectReferenceValue = leverHandles[i];
            }
            slpSO.FindProperty("_onSolved").objectReferenceValue = so.onPuzzleSolved;
            slpSO.ApplyModifiedPropertiesWithoutUndo();

            // Hint text on wall — correct order: A, C, B
            BuildFloatingLabel(parent, "PuzzleHint",
                roomPos + new Vector3(ROOM_SIZE / 2f - 0.5f, 2f, 0),
                "I → III → II",
                Quaternion.Euler(0, -90, 0));
        }

        // =====================================================================
        // ROOM GEOMETRY BUILDERS
        // =====================================================================

        private static void BuildRoom(Transform parent, string name, Vector3 center, string label,
            bool openNorth = false, bool openSouth = false, bool openEast = false, bool openWest = false)
        {
            GameObject room = new GameObject(name);
            room.transform.SetParent(parent, false);
            room.transform.position = center;

            float halfSize = ROOM_SIZE / 2f;
            float halfCorridor = CORRIDOR_WIDTH / 2f;

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(room.transform, false);
            floor.transform.localPosition = new Vector3(0, -WALL_THICKNESS / 2f, 0);
            floor.transform.localScale = new Vector3(ROOM_SIZE, WALL_THICKNESS, ROOM_SIZE);
            floor.GetComponent<Renderer>().sharedMaterial = _floorMat;
            floor.isStatic = true;

            // Ceiling
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(room.transform, false);
            ceiling.transform.localPosition = new Vector3(0, WALL_HEIGHT + WALL_THICKNESS / 2f, 0);
            ceiling.transform.localScale = new Vector3(ROOM_SIZE, WALL_THICKNESS, ROOM_SIZE);
            ceiling.GetComponent<Renderer>().sharedMaterial = _ceilingMat;
            ceiling.isStatic = true;

            // --- NORTH wall (positive Z) ---
            if (openNorth)
                BuildWallWithOpening(room.transform, "WallNorth",
                    new Vector3(0, WALL_HEIGHT / 2f, halfSize),
                    ROOM_SIZE, WALL_HEIGHT, WALL_THICKNESS,
                    halfCorridor, true);
            else
                BuildWall(room.transform, "WallNorth",
                    new Vector3(0, WALL_HEIGHT / 2f, halfSize),
                    new Vector3(ROOM_SIZE, WALL_HEIGHT, WALL_THICKNESS));

            // --- SOUTH wall (negative Z) ---
            if (openSouth)
                BuildWallWithOpening(room.transform, "WallSouth",
                    new Vector3(0, WALL_HEIGHT / 2f, -halfSize),
                    ROOM_SIZE, WALL_HEIGHT, WALL_THICKNESS,
                    halfCorridor, true);
            else
                BuildWall(room.transform, "WallSouth",
                    new Vector3(0, WALL_HEIGHT / 2f, -halfSize),
                    new Vector3(ROOM_SIZE, WALL_HEIGHT, WALL_THICKNESS));

            // --- EAST wall (positive X) ---
            if (openEast)
                BuildWallWithOpening(room.transform, "WallEast",
                    new Vector3(halfSize, WALL_HEIGHT / 2f, 0),
                    ROOM_SIZE, WALL_HEIGHT, WALL_THICKNESS,
                    halfCorridor, false);
            else
                BuildWall(room.transform, "WallEast",
                    new Vector3(halfSize, WALL_HEIGHT / 2f, 0),
                    new Vector3(WALL_THICKNESS, WALL_HEIGHT, ROOM_SIZE));

            // --- WEST wall (negative X) ---
            if (openWest)
                BuildWallWithOpening(room.transform, "WallWest",
                    new Vector3(-halfSize, WALL_HEIGHT / 2f, 0),
                    ROOM_SIZE, WALL_HEIGHT, WALL_THICKNESS,
                    halfCorridor, false);
            else
                BuildWall(room.transform, "WallWest",
                    new Vector3(-halfSize, WALL_HEIGHT / 2f, 0),
                    new Vector3(WALL_THICKNESS, WALL_HEIGHT, ROOM_SIZE));

            // Room label on floor
            BuildFloatingLabel(parent, name + "_Label", center + new Vector3(0, 0.01f, 0),
                label, Quaternion.Euler(90, 0, 0));
        }

        /// <summary>
        /// Builds a wall with a corridor-sized opening in the center.
        /// <paramref name="isNorthSouth"/> true = wall runs along X axis (North/South),
        /// false = wall runs along Z axis (East/West).
        /// </summary>
        private static void BuildWallWithOpening(Transform parent, string name,
            Vector3 center, float wallLength, float wallHeight, float wallThickness,
            float halfOpening, bool isNorthSouth)
        {
            // Two wall segments on either side of the opening, plus a header above

            float segmentLength = (wallLength / 2f) - halfOpening;

            if (isNorthSouth)
            {
                // Wall runs along X — opening is in X center
                // Left segment
                float leftX = -(halfOpening + segmentLength / 2f);
                BuildWall(parent, name + "_L",
                    center + new Vector3(leftX, 0, 0),
                    new Vector3(segmentLength, wallHeight, wallThickness));

                // Right segment
                float rightX = halfOpening + segmentLength / 2f;
                BuildWall(parent, name + "_R",
                    center + new Vector3(rightX, 0, 0),
                    new Vector3(segmentLength, wallHeight, wallThickness));
            }
            else
            {
                // Wall runs along Z — opening is in Z center
                // Front segment
                float frontZ = -(halfOpening + segmentLength / 2f);
                BuildWall(parent, name + "_F",
                    center + new Vector3(0, 0, frontZ),
                    new Vector3(wallThickness, wallHeight, segmentLength));

                // Back segment
                float backZ = halfOpening + segmentLength / 2f;
                BuildWall(parent, name + "_B",
                    center + new Vector3(0, 0, backZ),
                    new Vector3(wallThickness, wallHeight, segmentLength));
            }

            // Header above the opening
            float headerHeight = wallHeight * 0.15f;
            float openingWidth = halfOpening * 2f;
            if (isNorthSouth)
            {
                BuildWall(parent, name + "_Header",
                    center + new Vector3(0, (wallHeight - headerHeight) / 2f, 0),
                    new Vector3(openingWidth, headerHeight, wallThickness));
            }
            else
            {
                BuildWall(parent, name + "_Header",
                    center + new Vector3(0, (wallHeight - headerHeight) / 2f, 0),
                    new Vector3(wallThickness, headerHeight, openingWidth));
            }
        }

        private static void BuildWall(Transform parent, string name, Vector3 localPos, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = _wallMat;
            wall.isStatic = true;
        }

        private static void BuildCorridor(Transform parent, string name,
            Vector3 startPos, Vector3 direction, float length)
        {
            GameObject corridor = new GameObject(name);
            corridor.transform.SetParent(parent, false);
            corridor.transform.position = startPos;

            Vector3 midpoint = direction * (length / 2f);
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            float halfWidth = CORRIDOR_WIDTH / 2f;

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(corridor.transform, false);
            floor.transform.localPosition = midpoint + new Vector3(0, -WALL_THICKNESS / 2f, 0);
            floor.transform.localScale = new Vector3(
                Mathf.Abs(direction.x) > 0.5f ? length : CORRIDOR_WIDTH,
                WALL_THICKNESS,
                Mathf.Abs(direction.z) > 0.5f ? length : CORRIDOR_WIDTH);
            floor.GetComponent<Renderer>().sharedMaterial = _floorMat;
            floor.isStatic = true;

            // Ceiling
            var ceil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceil.name = "Ceiling";
            ceil.transform.SetParent(corridor.transform, false);
            ceil.transform.localPosition = midpoint + new Vector3(0, WALL_HEIGHT + WALL_THICKNESS / 2f, 0);
            ceil.transform.localScale = floor.transform.localScale;
            ceil.GetComponent<Renderer>().sharedMaterial = _ceilingMat;
            ceil.isStatic = true;

            // Side walls
            for (int side = -1; side <= 1; side += 2)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = side < 0 ? "WallLeft" : "WallRight";
                wall.transform.SetParent(corridor.transform, false);
                wall.transform.localPosition = midpoint + right * (halfWidth * side) +
                    new Vector3(0, WALL_HEIGHT / 2f, 0);
                wall.transform.localScale = new Vector3(
                    Mathf.Abs(direction.x) > 0.5f ? length : WALL_THICKNESS,
                    WALL_HEIGHT,
                    Mathf.Abs(direction.z) > 0.5f ? length : WALL_THICKNESS);
                wall.GetComponent<Renderer>().sharedMaterial = _wallMat;
                wall.isStatic = true;
            }
        }

        // =====================================================================
        // ROOM DRESSING
        // =====================================================================

        private static void BuildRamp(Transform parent, Vector3 position)
        {
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.transform.SetParent(parent, false);
            ramp.transform.position = position + new Vector3(0, 0.5f, 0);
            ramp.transform.localScale = new Vector3(2f, 1f, 3f);
            ramp.transform.rotation = Quaternion.Euler(20f, 0, 0);
            ramp.GetComponent<Renderer>().sharedMaterial = _accentMat;
            ramp.isStatic = true;
        }

        private static void BuildPillar(Transform parent, Vector3 position)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Pillar";
            pillar.transform.SetParent(parent, false);
            pillar.transform.position = position + new Vector3(0, WALL_HEIGHT / 2f, 0);
            pillar.transform.localScale = new Vector3(0.5f, WALL_HEIGHT / 2f, 0.5f);
            pillar.GetComponent<Renderer>().sharedMaterial = _accentMat;
            pillar.isStatic = true;
        }

        private static void BuildStairs(Transform parent, Vector3 position)
        {
            for (int i = 0; i < 5; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i}";
                step.transform.SetParent(parent, false);
                step.transform.position = position + new Vector3(0, 0.15f + i * 0.3f, i * 0.4f);
                step.transform.localScale = new Vector3(1.5f, 0.3f, 0.4f);
                step.GetComponent<Renderer>().sharedMaterial = _accentMat;
                step.isStatic = true;
            }
        }

        private static void BuildTable(Transform parent, Vector3 position)
        {
            // Simple table: flat top + 4 legs
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Table";
            top.transform.SetParent(parent, false);
            top.transform.position = position + new Vector3(0, 0.55f, 0);
            top.transform.localScale = new Vector3(0.8f, 0.05f, 0.6f);
            top.GetComponent<Renderer>().sharedMaterial = _darkMat;
            top.isStatic = true;

            float lx = 0.3f, lz = 0.2f;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leg.name = "Leg";
                    leg.transform.SetParent(top.transform, false);
                    leg.transform.localPosition = new Vector3(x * lx, -5.5f, z * lz);
                    leg.transform.localScale = new Vector3(0.1f / 0.8f, 11f, 0.1f / 0.6f);
                    leg.GetComponent<Renderer>().sharedMaterial = _darkMat;
                    leg.isStatic = true;
                }
            }
        }

        /// <summary>
        /// Builds a glowing hexagonal pillar (save station) with a point light.
        /// When the player interacts (E), the SaveStationUI opens.
        /// </summary>
        private static void BuildSaveStation(Transform parent, Vector3 position, SaveSystem saveSystem)
        {
            // ── Root object ─────────────────────────────────────────────────
            GameObject root = new GameObject("SaveStation");
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            // ── Base pedestal (dark cube) ───────────────────────────────────
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(root.transform, false);
            pedestal.transform.localPosition = new Vector3(0, 0.2f, 0);
            pedestal.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
            pedestal.GetComponent<Renderer>().sharedMaterial = _darkMat;
            pedestal.isStatic = true;

            // ── Crystal (cylinder with emissive material) ───────────────────
            var crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crystal.name = "Crystal";
            crystal.transform.SetParent(root.transform, false);
            crystal.transform.localPosition = new Vector3(0, 1.0f, 0);
            crystal.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);

            // Create emissive crystal material
            var crystalMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            crystalMat.name = "CrystalMaterial";
            crystalMat.color = new Color(0.1f, 0.3f, 0.8f, 1f);
            crystalMat.EnableKeyword("_EMISSION");
            crystalMat.SetColor("_EmissionColor", new Color(0.2f, 0.6f, 1f) * 2f);
            crystal.GetComponent<Renderer>().sharedMaterial = crystalMat;

            // ── Small top cap (sphere) ──────────────────────────────────────
            var topCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topCap.name = "TopCap";
            topCap.transform.SetParent(root.transform, false);
            topCap.transform.localPosition = new Vector3(0, 1.65f, 0);
            topCap.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            topCap.GetComponent<Renderer>().sharedMaterial = crystalMat;

            // Remove collider from decorative parts
            Object.DestroyImmediate(topCap.GetComponent<Collider>());

            // ── Point light (blue glow) ─────────────────────────────────────
            var lightGO = new GameObject("SaveStationLight");
            lightGO.transform.SetParent(root.transform, false);
            lightGO.transform.localPosition = new Vector3(0, 1.5f, 0);
            var pointLight = lightGO.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(0.2f, 0.6f, 1f);
            pointLight.intensity = 1.2f;
            pointLight.range = 5f;
            pointLight.shadows = LightShadows.Soft;

            // ── Interaction collider (on root — covers the whole pillar) ────
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.8f, 0);
            col.size = new Vector3(0.8f, 1.8f, 0.8f);

            // ── SaveStation component ───────────────────────────────────────
            var station = root.AddComponent<SaveStation>();
            SerializedObject stationSO = new SerializedObject(station);
            stationSO.FindProperty("_promptText").stringValue = "Save / Load Game";
            stationSO.FindProperty("_type").enumValueIndex = (int)InteractionType.Activate;
            stationSO.FindProperty("_glowColor").colorValue = new Color(0.2f, 0.6f, 1f, 1f);
            stationSO.ApplyModifiedPropertiesWithoutUndo();

            // ── SaveStationUI (on root, references SaveSystem) ──────────────
            var stationUI = root.AddComponent<SaveStationUI>();
            SerializedObject uiSO = new SerializedObject(stationUI);
            uiSO.FindProperty("_saveSystem").objectReferenceValue = saveSystem;
            uiSO.ApplyModifiedPropertiesWithoutUndo();

            // ── Floating label ──────────────────────────────────────────────
            BuildFloatingLabel(parent, "SaveStation_Label",
                position + new Vector3(0, 2.1f, 0), "SAVE STATION");
        }

        private static void BuildDoor(Transform parent, string name, Vector3 position)
        {
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = name;
            door.transform.SetParent(parent, false);
            door.transform.position = position + new Vector3(0, WALL_HEIGHT / 2f, 0);
            door.transform.localScale = new Vector3(WALL_THICKNESS * 2f, WALL_HEIGHT, 1.2f);
            door.GetComponent<Renderer>().sharedMaterial = _darkMat;
        }

        private static void BuildLockedDoor(Transform parent, string name,
            Vector3 position, ItemData requiredItem)
        {
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = name;
            door.transform.SetParent(parent, false);
            door.transform.position = position + new Vector3(0, WALL_HEIGHT / 2f, 0);
            door.transform.localScale = new Vector3(1.5f, WALL_HEIGHT * 0.85f, WALL_THICKNESS * 2f);
            door.GetComponent<Renderer>().sharedMaterial = _darkMat;
            door.layer = LayerMask.NameToLayer("Default");

            var lockedDoor = door.AddComponent<LockedDoor>();
            SerializedObject so = new SerializedObject(lockedDoor);
            so.FindProperty("_requiredItem").objectReferenceValue = requiredItem;
            so.FindProperty("_lockedPrompt").stringValue = $"Locked — Requires {requiredItem.displayName}";
            so.FindProperty("_unlockedPrompt").stringValue = "Open Door";
            so.FindProperty("_saveID").stringValue = name.ToLower();
            so.ApplyModifiedPropertiesWithoutUndo();

            // Label above door
            BuildFloatingLabel(parent, name + "_Label",
                position + new Vector3(0, WALL_HEIGHT * 0.85f + 0.3f, -0.5f),
                "LOCKED");
        }

        // =====================================================================
        // INTERACTABLE BUILDERS
        // =====================================================================

        private static GameObject BuildPickup(Transform parent, string name,
            Vector3 position, ItemData item, string displayName)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            go.GetComponent<Renderer>().sharedMaterial = _interactMat;

            var pickup = go.AddComponent<PickupInteractable>();
            SerializedObject so = new SerializedObject(pickup);
            so.FindProperty("_itemData").objectReferenceValue = item;
            so.FindProperty("_promptText").stringValue = $"Pick up {displayName}";
            so.FindProperty("_interactionType").enumValueIndex = 1; // Pickup
            so.FindProperty("_destroyOnPickup").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static void BuildWallButton(Transform parent, string name,
            Vector3 position, Quaternion rotation)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = new Vector3(0.15f, 0.15f, 0.05f);
            go.GetComponent<Renderer>().sharedMaterial = _puzzleMat;

            var interactable = go.AddComponent<InteractableObject>();
            SerializedObject so = new SerializedObject(interactable);
            so.FindProperty("_promptText").stringValue = "Press Button";
            so.FindProperty("_interactionType").enumValueIndex = 3; // Activate
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // HORROR ROOM OBJECTS
        // =====================================================================

        private static GameObject BuildGhostObject(Transform parent, Vector3 position)
        {
            // A tall dark figure (cylinder + sphere head)
            GameObject ghost = new GameObject("GhostFigure");
            ghost.transform.SetParent(parent, false);
            ghost.transform.position = position;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(ghost.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.4f, 0.9f, 0.4f);
            body.GetComponent<Renderer>().sharedMaterial = _darkMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(ghost.transform, false);
            head.transform.localPosition = new Vector3(0, 2f, 0);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            head.GetComponent<Renderer>().sharedMaterial = _darkMat;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            return ghost;
        }

        private static GameObject BuildAlternateDecor(Transform parent, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "AlternateDecor";
            go.transform.SetParent(parent, false);
            go.transform.position = position + new Vector3(0, 0.5f, 0);
            go.transform.localScale = new Vector3(1f, 1f, 1f);
            go.GetComponent<Renderer>().sharedMaterial = _darkMat;
            return go;
        }

        private static void BuildEventTriggerZone(Transform parent, string name,
            Vector3 position, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;

            var zone = go.AddComponent<EventTriggerZone>();
            SerializedObject so = new SerializedObject(zone);
            so.FindProperty("_specificEventID").stringValue = "ghost_appear";
            so.FindProperty("_triggerTag").stringValue = "Player";
            so.FindProperty("_oneTimeOnly").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // LIGHTING
        // =====================================================================

        private static void BuildGlobalLighting(Transform parent)
        {
            // Directional light (very dim — it's a horror game)
            GameObject dirLight = new GameObject("DirectionalLight");
            dirLight.transform.SetParent(parent, false);
            dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            var dl = dirLight.AddComponent<Light>();
            dl.type = LightType.Directional;
            dl.intensity = 0.15f;
            dl.color = new Color(0.6f, 0.6f, 0.7f);
            dl.shadows = LightShadows.Soft;

            // Ambient
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.1f);

            // Fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.06f);
            RenderSettings.fogDensity = 0.015f;
        }

        private static Light BuildRoomLight(Transform parent, string name,
            Vector3 position, float intensity)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(90, 0, 0);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = ROOM_SIZE * 1.2f;
            light.color = new Color(0.9f, 0.85f, 0.75f);
            light.shadows = LightShadows.Soft;

            return light;
        }

        private static void BuildCorridorLight(Transform parent, Vector3 position)
        {
            GameObject go = new GameObject("CorridorLight");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 0.4f;
            light.range = CORRIDOR_LENGTH;
            light.color = new Color(0.7f, 0.65f, 0.6f);
            light.shadows = LightShadows.Soft;
        }

        // =====================================================================
        // UI HELPERS
        // =====================================================================

        private static void BuildFloatingLabel(Transform parent, string name,
            Vector3 worldPos, string text,
            Quaternion? rotation = null)
        {
            // 3D text using TextMesh (lightweight, no canvas needed)
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            if (rotation.HasValue) go.transform.rotation = rotation.Value;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.7f, 0.7f, 0.75f, 0.8f);
        }

        private static void BuildDebugUI()
        {
            GameObject canvasGo = new GameObject("DebugCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Help text (top-left)
            GameObject helpGo = new GameObject("HelpText");
            helpGo.transform.SetParent(canvasGo.transform, false);
            var rt = helpGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(15, -15);
            rt.sizeDelta = new Vector2(500, 220);

            var tmp = helpGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 16;
            tmp.color = new Color(0.6f, 0.8f, 0.6f, 0.7f);
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.text =
                "<b>TEST ROOM — Controls</b>\n" +
                "WASD — Move | Shift — Sprint | C — Crouch\n" +
                "Mouse — Look | E — Interact\n" +
                "Tab/I — Inventory | Esc — Pause Menu\n" +
                "Scroll — Zoom (Inspect)\n\n" +
                "<b>Rooms:</b>\n" +
                "1 (Start) — Movement, head bob, <color=#5599FF>Save Station</color>\n" +
                "2 (Right) — Pickups, locked door, inventory\n" +
                "3 (Behind) — 3-lever puzzle (order: A→C→B)\n" +
                "4 (Diagonal) — Horror: flicker, ghost, phases\n\n" +
                "<b>Quick:</b> F5 Save | F9 Load";
        }

        // =====================================================================
        // MATERIALS
        // =====================================================================

        private static void CreateMaterials()
        {
            // Use Lit shader for HDRP, fallback to Standard
            string shaderName = Shader.Find("HDRP/Lit") != null ? "HDRP/Lit" : "Standard";

            _floorMat = MakeMat("TestFloor", shaderName, new Color(0.15f, 0.15f, 0.18f));
            _wallMat = MakeMat("TestWall", shaderName, new Color(0.25f, 0.24f, 0.26f));
            _ceilingMat = MakeMat("TestCeiling", shaderName, new Color(0.1f, 0.1f, 0.12f));
            _accentMat = MakeMat("TestAccent", shaderName, new Color(0.35f, 0.28f, 0.22f));
            _darkMat = MakeMat("TestDark", shaderName, new Color(0.03f, 0.03f, 0.04f));
            _interactMat = MakeMat("TestInteract", shaderName, new Color(0.4f, 0.55f, 0.35f));
            _puzzleMat = MakeMat("TestPuzzle", shaderName, new Color(0.5f, 0.35f, 0.25f));
        }

        private static Material MakeMat(string name, string shaderName, Color color)
        {
            string path = $"Assets/Art/Materials/{name}.mat";
            EnsureFolder("Assets/Art/Materials");

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find(shaderName));
            mat.color = color;
            mat.name = name;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // =====================================================================
        // SCRIPTABLE OBJECT HELPER
        // =====================================================================

        private static T GetOrCreateSO<T>(string name) where T : ScriptableObject
        {
            string path = $"{SO_ROOT}/{name}.asset";
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // =====================================================================
        // UTILITY
        // =====================================================================

        private static void EnsureFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string[] parts = folderPath.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
