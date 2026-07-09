// ============================================================================
// DemoLevelBuilder.cs — Editor utility: builds the playable demo level
// A linear "abandoned apartment building" walkthrough (~10 minutes):
//
//   Room A — Entrance Hall : spawn, intro note, save station
//   Room B — Living Quarters: story note, Brass Key + Sedative pickups,
//                             locked door (needs the key)
//   Corridor B→C           : light-flicker scripted event (small sanity hit)
//   Room C — Study          : candle light-pattern puzzle (hint note),
//                             solving unlocks the sealed door + phase shift
//   Corridor C→D            : ghost-appear scripted event (sanity hit)
//   Room D — Cellar          : final note, save station, END OF DEMO
//
// Run via: Fractured Echoes → Build Demo Level
// ============================================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using FracturedEchoes.Player;
using FracturedEchoes.Interaction;
using FracturedEchoes.InventorySystem;
using FracturedEchoes.Puzzle;
using FracturedEchoes.Environment;
using FracturedEchoes.Audio;
using FracturedEchoes.Lighting;
using FracturedEchoes.Core;
using FracturedEchoes.Core.Events;
using FracturedEchoes.Core.SaveLoad;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.UI;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.Editor
{
    public static class DemoLevelBuilder
    {
        // Layout constants
        private const float ROOM = 10f;          // room size
        private const float H = 4.5f;            // wall height
        private const float T = 0.3f;            // wall thickness
        private const float CW = 2.5f;           // corridor width
        private const float CL = 5f;             // corridor length
        private const float SPACING = ROOM + CL; // distance between room centers

        private const string SO_ROOT = "Assets/Resources/DemoData";
        private const string SCENE_PATH = "Assets/Scenes/DemoLevel.unity";

        // Materials
        private static Material _floorMat, _wallMat, _ceilMat, _darkMat,
                                _paperMat, _interactMat, _puzzleMat, _dangerMat;

        // =====================================================================
        // MENU ENTRY
        // =====================================================================

        [MenuItem("Fractured Echoes/Build Demo Level", false, 103)]
        public static void BuildDemoLevel()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                CreateMaterials();
                EnsureFolder(SO_ROOT);
                var so = CreateAssets();

                // Root containers
                var geometry = new GameObject("--- GEOMETRY ---");
                var systems = new GameObject("--- SYSTEMS ---");
                var lighting = new GameObject("--- LIGHTING ---");
                var interactables = new GameObject("--- INTERACTABLES ---");
                var puzzles = new GameObject("--- PUZZLES ---");
                var events = new GameObject("--- EVENTS ---");

                // Room centers (linear layout along +X)
                Vector3 roomA = Vector3.zero;
                Vector3 roomB = new Vector3(SPACING, 0, 0);
                Vector3 roomC = new Vector3(SPACING * 2, 0, 0);
                Vector3 roomD = new Vector3(SPACING * 3, 0, 0);

                // ------------------------------------------------ GEOMETRY
                BuildRoom(geometry.transform, "RoomA_Entrance", roomA, openEast: true);
                BuildRoom(geometry.transform, "RoomB_Living", roomB, openWest: true, openEast: true);
                BuildRoom(geometry.transform, "RoomC_Study", roomC, openWest: true, openEast: true);
                BuildRoom(geometry.transform, "RoomD_Cellar", roomD, openWest: true);

                BuildCorridor(geometry.transform, "Corridor_AB", roomA + new Vector3(ROOM / 2f, 0, 0));
                BuildCorridor(geometry.transform, "Corridor_BC", roomB + new Vector3(ROOM / 2f, 0, 0));
                BuildCorridor(geometry.transform, "Corridor_CD", roomC + new Vector3(ROOM / 2f, 0, 0));

                // ------------------------------------------------ PLAYER
                var player = BuildPlayer(roomA + new Vector3(-3f, 1f, 0f), so);

                // ------------------------------------------------ LIGHTING
                BuildGlobalLighting();
                BuildRoomLight(lighting.transform, "Light_A", roomA + new Vector3(0, H - 0.3f, 0), 1.4f);
                BuildRoomLight(lighting.transform, "Light_B", roomB + new Vector3(0, H - 0.3f, 0), 1.1f);
                BuildRoomLight(lighting.transform, "Light_C", roomC + new Vector3(0, H - 0.3f, 0), 0.9f);
                var cellarLight = BuildRoomLight(lighting.transform, "Light_D",
                    roomD + new Vector3(0, H - 0.3f, 0), 0.5f);
                cellarLight.color = new Color(0.8f, 0.75f, 0.65f);

                var corrABLight = BuildCorridorLight(lighting.transform, roomA + new Vector3(ROOM / 2f + CL / 2f, H - 0.4f, 0));
                var corrBCLight = BuildCorridorLight(lighting.transform, roomB + new Vector3(ROOM / 2f + CL / 2f, H - 0.4f, 0));
                var corrCDLight = BuildCorridorLight(lighting.transform, roomC + new Vector3(ROOM / 2f + CL / 2f, H - 0.4f, 0));
                corrCDLight.intensity = 0.25f; // darker approach to the cellar

                // ------------------------------------------------ ROOM A: intro
                BuildCabinet(interactables.transform, roomA + new Vector3(2f, 0, 2.5f));
                BuildNote(interactables.transform, "Note_Intro",
                    roomA + new Vector3(2f, 0.62f, 2.5f),
                    "Read the manager's letter",
                    "Building Manager's Letter",
                    "Mr. Kovač,\n\n" +
                    "The keys are yours now. Nobody has lived on this floor since " +
                    "your family... since that winter. I kept the power on, though " +
                    "half the lights do what they want.\n\n" +
                    "The bedroom is locked — your sister always hid the brass key " +
                    "somewhere in the living quarters. The cellar I never go into. " +
                    "You'll understand why.\n\n" +
                    "Take what you came for and leave before dark.\n\n" +
                    "— D.");
                BuildSaveStation(interactables.transform, roomA + new Vector3(3.5f, 0, -3.5f), null); // saveSystem wired later
                BuildLabel(geometry.transform, "LabelA", roomA + new Vector3(0, 2.8f, -4.5f), "ENTRANCE HALL");

                // ------------------------------------------------ ROOM B: key + locked door
                BuildCabinet(interactables.transform, roomB + new Vector3(-2f, 0, 3f));
                BuildNote(interactables.transform, "Note_Sister",
                    roomB + new Vector3(-2f, 0.62f, 3f),
                    "Read the diary page",
                    "Torn Diary Page",
                    "...Marta keeps hiding the bedroom key again. She says the " +
                    "room \"hums\" at night and no one should go in.\n\n" +
                    "I found it last time behind the old dresser, by the far wall. " +
                    "She never hides it anywhere new.\n\n" +
                    "The pills the doctor gave her are on the shelf. She stopped " +
                    "taking them. She says they make the humming louder...",
                    sanityOnFirstRead: -3f);

                BuildCabinet(interactables.transform, roomB + new Vector3(3f, 0, -3f));
                BuildPickup(interactables.transform, "Pickup_BrassKey",
                    roomB + new Vector3(3f, 0.72f, -3f), so.keyItem, "Brass Key");

                BuildCabinet(interactables.transform, roomB + new Vector3(-3f, 0, -2f));
                BuildPickup(interactables.transform, "Pickup_Sedative",
                    roomB + new Vector3(-3f, 0.72f, -2f), so.medicineItem, "Sedative Pills");

                BuildLockedDoor(geometry.transform, "Door_Bedroom",
                    roomB + new Vector3(ROOM / 2f + 0.3f, 0, 0),
                    so.keyItem,
                    "Locked — the bedroom key should be nearby",
                    "Open Door", "door_bedroom");
                BuildLabel(geometry.transform, "LabelB", roomB + new Vector3(0, 2.8f, -4.5f), "LIVING QUARTERS");

                // ------------------------------------------------ CORRIDOR B→C: flicker event
                BuildTriggerZone(events.transform, "Trigger_Flicker",
                    roomB + new Vector3(ROOM / 2f + CL / 2f, H / 2f, 0),
                    new Vector3(1.2f, H, CW), "demo_flicker_corridor");

                // ------------------------------------------------ ROOM C: candle puzzle
                var studyDoor = BuildLockedDoor(geometry.transform, "Door_Study",
                    roomC + new Vector3(ROOM / 2f + 0.3f, 0, 0),
                    so.sealItem,
                    "Sealed shut — the candles on the wall seem important",
                    "Open Door", "door_study");

                BuildCandlePuzzle(puzzles.transform, roomC, so, studyDoor);

                BuildCabinet(interactables.transform, roomC + new Vector3(-3f, 0, -3f));
                BuildNote(interactables.transform, "Note_Candles",
                    roomC + new Vector3(-3f, 0.62f, -3f),
                    "Read the burnt journal",
                    "Father's Journal — Last Entry",
                    "The humming comes from below. I sealed the study door the " +
                    "only way the old books describe.\n\n" +
                    "Three candles guard the seal. On the night it worked, only " +
                    "the FIRST and the THIRD flame burned. The middle one must " +
                    "stay dark — it always must.\n\n" +
                    "If anyone ever undoes this... God forgive them.",
                    sanityOnFirstRead: -5f);
                BuildLabel(geometry.transform, "LabelC", roomC + new Vector3(0, 2.8f, -4.5f), "STUDY");

                // ------------------------------------------------ CORRIDOR C→D: ghost event
                var ghost = BuildGhost(events.transform, roomD + new Vector3(3f, 0, 3.5f));
                ghost.SetActive(false);

                BuildTriggerZone(events.transform, "Trigger_Ghost",
                    roomC + new Vector3(ROOM / 2f + CL - 1f, H / 2f, 0),
                    new Vector3(1.2f, H, CW), "demo_ghost");

                // ------------------------------------------------ ROOM D: finale
                BuildCabinet(interactables.transform, roomD + new Vector3(0f, 0, 0f));
                BuildNote(interactables.transform, "Note_Final",
                    roomD + new Vector3(0f, 0.62f, 0f),
                    "Read the page nailed to the crate",
                    "A Page In Your Own Handwriting",
                    "You shouldn't have come back.\n\n" +
                    "You were here that winter. You heard the humming too — you " +
                    "just chose to forget. The building remembers what you did " +
                    "with the candles.\n\n" +
                    "It has been waiting for you to open the seal again.\n\n" +
                    "        — — —  END OF DEMO  — — —\n\n" +
                    "Thanks for playing this skeleton build of FRACTURED ECHOES.",
                    sanityOnFirstRead: -10f);

                BuildSaveStation(interactables.transform, roomD + new Vector3(3.5f, 0, -3.5f), null);
                BuildLabel(geometry.transform, "LabelD", roomD + new Vector3(0, 2.8f, 4.5f), "CELLAR");
                BuildLabel(geometry.transform, "LabelEnd", roomD + new Vector3(0, 2.2f, 4.5f),
                    "<color=#AA3333>END OF DEMO</color>");

                // Cold spot (small, marked, off the main path)
                BuildColdSpot(events.transform, roomD + new Vector3(-3.5f, 0, 3.5f));

                // ------------------------------------------------ CORE SYSTEMS
                var core = BuildCoreSystems(systems.transform, so);
                var saveSys = core.GetComponent<SaveSystem>();
                var sec = core.GetComponent<ScriptedEventController>();

                // Wire scripted events (flicker corridor / ghost / flicker cellar)
                var ghostAudio = ghost.AddComponent<AudioSource>();
                ghostAudio.spatialBlend = 1f;
                ghostAudio.playOnAwake = false;

                var secSO = new SerializedObject(sec);
                var arr = secSO.FindProperty("_events");
                arr.arraySize = 3;

                var e0 = arr.GetArrayElementAtIndex(0); // corridor flicker
                e0.FindPropertyRelative("eventData").objectReferenceValue = so.flickerCorridor;
                e0.FindPropertyRelative("targetLight").objectReferenceValue = corrBCLight;

                var e1 = arr.GetArrayElementAtIndex(1); // ghost appear
                e1.FindPropertyRelative("eventData").objectReferenceValue = so.ghostEvent;
                e1.FindPropertyRelative("targetObject").objectReferenceValue = ghost;
                e1.FindPropertyRelative("audioSource").objectReferenceValue = ghostAudio;

                var e2 = arr.GetArrayElementAtIndex(2); // cellar flicker (chained)
                e2.FindPropertyRelative("eventData").objectReferenceValue = so.flickerCellar;
                e2.FindPropertyRelative("targetLight").objectReferenceValue = cellarLight;

                secSO.ApplyModifiedPropertiesWithoutUndo();

                // Wire save systems into both save stations
                foreach (var ui in Object.FindObjectsByType<SaveStationUI>(FindObjectsSortMode.None))
                {
                    var uiSO = new SerializedObject(ui);
                    uiSO.FindProperty("_saveSystem").objectReferenceValue = saveSys;
                    uiSO.ApplyModifiedPropertiesWithoutUndo();
                }

                // Audio manager
                var audioGO = new GameObject("AudioManager");
                audioGO.transform.SetParent(systems.transform, false);
                audioGO.AddComponent<AudioManager>();

                // HUD + help
                BuildHUD(player.transform);
                BuildHelpUI();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DemoLevelBuilder] Error during build (scene still saved): {ex.Message}\n{ex.StackTrace}");
            }

            // Save + register
            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();
            AddSceneToBuildSettings(SCENE_PATH);
            Debug.Log("[DemoLevelBuilder] Demo level built and saved to " + SCENE_PATH);
        }

        // =====================================================================
        // SCRIPTABLE OBJECT ASSETS
        // =====================================================================

        private struct DemoAssets
        {
            public ItemData keyItem, medicineItem, sealItem;
            public EnvironmentPhaseData phase0, phase1, phase2;
            public GameEvent onPuzzleSolved;
            public ScriptedEventData flickerCorridor, ghostEvent, flickerCellar;
        }

        private static DemoAssets CreateAssets()
        {
            DemoAssets a = new DemoAssets();

            a.onPuzzleSolved = GetOrCreateSO<GameEvent>("Demo_OnPuzzleSolved");

            // --- Items ---
            a.keyItem = GetOrCreateSO<ItemData>("Demo_Item_BrassKey");
            a.keyItem.itemID = "demo_key_bedroom";
            a.keyItem.displayName = "Brass Key";
            a.keyItem.description = "A small brass key, cold to the touch. Marta's hiding spot never changed.";
            a.keyItem.itemType = ItemType.Key;
            a.keyItem.canUseOnEnvironment = true;
            EditorUtility.SetDirty(a.keyItem);

            a.medicineItem = GetOrCreateSO<ItemData>("Demo_Item_Sedative");
            a.medicineItem.itemID = "demo_sedative";
            a.medicineItem.displayName = "Sedative Pills";
            a.medicineItem.description = "Marta's prescription. She said they make the humming louder. Restores sanity.";
            a.medicineItem.itemType = ItemType.Consumable;
            a.medicineItem.sanityRestore = 40f;
            EditorUtility.SetDirty(a.medicineItem);

            // Never obtainable — only used so the study door can't be opened by items
            a.sealItem = GetOrCreateSO<ItemData>("Demo_Item_Seal");
            a.sealItem.itemID = "demo_seal";
            a.sealItem.displayName = "Old Seal";
            a.sealItem.description = "The seal on the study door. It cannot be picked up.";
            a.sealItem.itemType = ItemType.QuestItem;
            EditorUtility.SetDirty(a.sealItem);

            // --- Environment phases ---
            a.phase0 = MakePhase("Demo_Phase_Normal", "Normal", 0,
                new Color(0.30f, 0.30f, 0.35f), 0.8f, new Color(0.14f, 0.14f, 0.17f), 0.010f, 3f);
            a.phase1 = MakePhase("Demo_Phase_Unsettling", "Unsettling", 1,
                new Color(0.20f, 0.17f, 0.22f), 0.5f, new Color(0.09f, 0.07f, 0.11f), 0.022f, 5f);
            a.phase2 = MakePhase("Demo_Phase_Dark", "Dark", 2,
                new Color(0.06f, 0.04f, 0.08f), 0.25f, new Color(0.02f, 0.01f, 0.04f), 0.045f, 8f);

            // --- Scripted events ---
            a.flickerCorridor = GetOrCreateSO<ScriptedEventData>("Demo_Event_FlickerCorridor");
            a.flickerCorridor.eventID = "demo_flicker_corridor";
            a.flickerCorridor.editorDescription = "Corridor lights flicker on approach to the study";
            a.flickerCorridor.triggerType = TriggerType.EnterArea;
            a.flickerCorridor.eventType = ScriptedEventType.LightFlicker;
            a.flickerCorridor.effectDuration = 2.5f;
            a.flickerCorridor.oneTimeOnly = true;
            a.flickerCorridor.sanityDamage = 4f;
            EditorUtility.SetDirty(a.flickerCorridor);

            a.flickerCellar = GetOrCreateSO<ScriptedEventData>("Demo_Event_FlickerCellar");
            a.flickerCellar.eventID = "demo_flicker_cellar";
            a.flickerCellar.editorDescription = "Cellar light flickers after the figure appears";
            a.flickerCellar.triggerType = TriggerType.Combination; // fired only via chaining
            a.flickerCellar.eventType = ScriptedEventType.LightFlicker;
            a.flickerCellar.effectDuration = 3f;
            a.flickerCellar.oneTimeOnly = true;
            a.flickerCellar.sanityDamage = 0f;
            EditorUtility.SetDirty(a.flickerCellar);

            a.ghostEvent = GetOrCreateSO<ScriptedEventData>("Demo_Event_Ghost");
            a.ghostEvent.eventID = "demo_ghost";
            a.ghostEvent.editorDescription = "Dark figure appears in the cellar corner";
            a.ghostEvent.triggerType = TriggerType.EnterArea;
            a.ghostEvent.eventType = ScriptedEventType.ObjectAppear;
            a.ghostEvent.effectDuration = 2f;
            a.ghostEvent.oneTimeOnly = true;
            a.ghostEvent.sanityDamage = 15f;
            a.ghostEvent.chainedEventID = "demo_flicker_cellar";
            EditorUtility.SetDirty(a.ghostEvent);

            AssetDatabase.SaveAssets();
            return a;
        }

        private static EnvironmentPhaseData MakePhase(string asset, string name, int index,
            Color ambient, float intensity, Color fog, float fogDensity, float duration)
        {
            var p = GetOrCreateSO<EnvironmentPhaseData>(asset);
            p.phaseName = name;
            p.phaseIndex = index;
            p.ambientColor = ambient;
            p.ambientIntensity = intensity;
            p.fogColor = fog;
            p.fogDensity = fogDensity;
            p.transitionDuration = duration;
            EditorUtility.SetDirty(p);
            return p;
        }

        // =====================================================================
        // PLAYER
        // =====================================================================

        private static GameObject BuildPlayer(Vector3 spawnPos, DemoAssets so)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = spawnPos;
            player.transform.rotation = Quaternion.Euler(0, 90, 0); // face down the hallway

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);

            GameObject camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(player.transform, false);
            camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            GameObject camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(camHolder.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();

            player.AddComponent<FirstPersonMotor>();
            var fpCam = player.AddComponent<FirstPersonCamera>();
            player.AddComponent<FirstPersonController>();

            var camSO = new SerializedObject(fpCam);
            camSO.FindProperty("_cameraHolder").objectReferenceValue = camHolder.transform;
            camSO.ApplyModifiedPropertiesWithoutUndo();

            GameObject inspectPoint = new GameObject("InspectionPoint");
            inspectPoint.transform.SetParent(camGo.transform, false);
            inspectPoint.transform.localPosition = new Vector3(0, 0, 0.5f);

            var inspectCtrl = player.AddComponent<InspectItemController>();
            var inspSO = new SerializedObject(inspectCtrl);
            inspSO.FindProperty("_inspectionPoint").objectReferenceValue = inspectPoint.transform;
            inspSO.FindProperty("_inspectionCamera").objectReferenceValue = cam;
            inspSO.ApplyModifiedPropertiesWithoutUndo();

            var invMgr = player.AddComponent<InventoryManager>();
            var invSO = new SerializedObject(invMgr);
            invSO.FindProperty("_inspectController").objectReferenceValue = inspectCtrl;
            var db = invSO.FindProperty("_itemDatabase");
            db.arraySize = 3;
            db.GetArrayElementAtIndex(0).objectReferenceValue = so.keyItem;
            db.GetArrayElementAtIndex(1).objectReferenceValue = so.medicineItem;
            db.GetArrayElementAtIndex(2).objectReferenceValue = so.sealItem;
            invSO.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<InventoryUI>();
            player.AddComponent<SanitySystem>();
            player.AddComponent<HotbarUI>();
            player.AddComponent<SanityBarUI>();

            var interSys = player.AddComponent<InteractionSystem>();
            var interSO = new SerializedObject(interSys);
            interSO.FindProperty("_cameraTransform").objectReferenceValue = camGo.transform;
            interSO.FindProperty("_interactionRange").floatValue = 3f;
            interSO.FindProperty("_interactableLayer").intValue = ~0;
            interSO.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        // =====================================================================
        // CORE SYSTEMS
        // =====================================================================

        private static GameObject BuildCoreSystems(Transform parent, DemoAssets so)
        {
            GameObject go = new GameObject("CoreSystems");
            go.transform.SetParent(parent, false);

            var saveSys = go.AddComponent<SaveSystem>();

            var gsm = go.AddComponent<GameStateManager>();
            var gsmSO = new SerializedObject(gsm);
            gsmSO.FindProperty("_saveSystem").objectReferenceValue = saveSys;
            var locNames = gsmSO.FindProperty("_locationSceneNames");
            locNames.arraySize = 1;
            locNames.GetArrayElementAtIndex(0).stringValue = "DemoLevel";
            gsmSO.ApplyModifiedPropertiesWithoutUndo();

            var sec = go.AddComponent<ScriptedEventController>();

            var envMgr = go.AddComponent<EnvironmentStateManager>();
            var envSO = new SerializedObject(envMgr);
            var phases = envSO.FindProperty("_phases");
            phases.arraySize = 3;
            phases.GetArrayElementAtIndex(0).objectReferenceValue = so.phase0;
            phases.GetArrayElementAtIndex(1).objectReferenceValue = so.phase1;
            phases.GetArrayElementAtIndex(2).objectReferenceValue = so.phase2;
            envSO.FindProperty("_scriptedEventController").objectReferenceValue = sec;
            envSO.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<PauseMenuController>();

            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.transform.SetParent(parent, false);
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<InputSystemUIInputModule>();

            // Firebase chain (same as test scene — safe to keep even if unconfigured)
            go.AddComponent<FirebaseInitializer>();
            go.AddComponent<FirebaseAuthManager>();
            var cloudSave = go.AddComponent<CloudSaveManager>();
            var cloudSO = new SerializedObject(cloudSave);
            cloudSO.FindProperty("_saveSystem").objectReferenceValue = saveSys;
            cloudSO.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        // =====================================================================
        // CANDLE LIGHT PUZZLE (Room C)
        // =====================================================================

        private static void BuildCandlePuzzle(Transform parent, Vector3 roomPos,
            DemoAssets so, LockedDoor doorToUnlock)
        {
            GameObject root = new GameObject("CandlePuzzle");
            root.transform.SetParent(parent, false);
            root.transform.position = roomPos;

            var switches = new LightSwitchInteractable[3];
            float[] xOffsets = { -1.8f, 0f, 1.8f };
            string[] names = { "First Candle", "Second Candle", "Third Candle" };

            for (int i = 0; i < 3; i++)
            {
                Vector3 basePos = roomPos + new Vector3(xOffsets[i], 1.4f, ROOM / 2f - 0.5f);

                // Shelf
                var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelf.name = $"CandleShelf_{i + 1}";
                shelf.transform.SetParent(root.transform, false);
                shelf.transform.position = basePos + new Vector3(0, -0.12f, 0.1f);
                shelf.transform.localScale = new Vector3(0.6f, 0.06f, 0.4f);
                shelf.GetComponent<Renderer>().sharedMaterial = _darkMat;

                // Candle body (interactable)
                var candle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                candle.name = $"Candle_{i + 1}";
                candle.transform.SetParent(root.transform, false);
                candle.transform.position = basePos + new Vector3(0, 0.12f, 0);
                candle.transform.localScale = new Vector3(0.12f, 0.2f, 0.12f);
                candle.GetComponent<Renderer>().sharedMaterial = _paperMat;

                // Flame light
                var flameGO = new GameObject("Flame");
                flameGO.transform.SetParent(candle.transform, false);
                flameGO.transform.localPosition = new Vector3(0, 1.4f, 0);
                var flame = flameGO.AddComponent<Light>();
                flame.type = LightType.Point;
                flame.color = new Color(1f, 0.72f, 0.35f);
                flame.intensity = 1.1f;
                flame.range = 4f;
                flame.enabled = false;

                // Switch component
                var sw = candle.AddComponent<LightSwitchInteractable>();
                var swSO = new SerializedObject(sw);
                swSO.FindProperty("_controlledLight").objectReferenceValue = flame;
                swSO.FindProperty("_startsOn").boolValue = false;
                swSO.FindProperty("_promptText").stringValue = $"Light / snuff the {names[i].ToLower()}";
                swSO.FindProperty("_interactionType").enumValueIndex = 4; // Puzzle
                swSO.ApplyModifiedPropertiesWithoutUndo();

                switches[i] = sw;

                BuildLabel(root.transform, $"CandleLabel_{i + 1}",
                    basePos + new Vector3(0, 0.65f, -0.2f), ToRoman(i + 1));
            }

            // Puzzle controller
            var puzzle = root.AddComponent<LightPatternPuzzle>();
            var pSO = new SerializedObject(puzzle);
            pSO.FindProperty("_puzzleID").stringValue = "demo_candle_puzzle";
            var swArr = pSO.FindProperty("_switches");
            swArr.arraySize = 3;
            for (int i = 0; i < 3; i++)
                swArr.GetArrayElementAtIndex(i).objectReferenceValue = switches[i];
            var pattern = pSO.FindProperty("_targetPattern");
            pattern.arraySize = 3;
            pattern.GetArrayElementAtIndex(0).boolValue = true;   // first lit
            pattern.GetArrayElementAtIndex(1).boolValue = false;  // middle dark
            pattern.GetArrayElementAtIndex(2).boolValue = true;   // third lit
            pSO.FindProperty("_doorToUnlock").objectReferenceValue = doorToUnlock;
            pSO.FindProperty("_triggerPhaseIndex").intValue = 1;  // shift to Unsettling
            pSO.FindProperty("_onSolved").objectReferenceValue = so.onPuzzleSolved;
            pSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string ToRoman(int n) => n switch { 1 => "I", 2 => "II", 3 => "III", _ => n.ToString() };

        // =====================================================================
        // INTERACTABLE BUILDERS
        // =====================================================================

        private static void BuildNote(Transform parent, string name, Vector3 position,
            string prompt, string title, string body, float sanityOnFirstRead = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0, Random.Range(-20f, 20f), 0);
            go.transform.localScale = new Vector3(0.28f, 0.012f, 0.38f);
            go.GetComponent<Renderer>().sharedMaterial = _paperMat;

            // Enlarge the trigger collider a little for easier targeting
            var col = go.GetComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 8f, 1.4f);

            var note = go.AddComponent<NoteInteractable>();
            var so = new SerializedObject(note);
            so.FindProperty("_promptText").stringValue = prompt;
            so.FindProperty("_interactionType").enumValueIndex = 2; // Inspect
            so.FindProperty("_noteTitle").stringValue = title;
            so.FindProperty("_noteText").stringValue = body;
            so.FindProperty("_sanityOnFirstRead").floatValue = sanityOnFirstRead;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildPickup(Transform parent, string name,
            Vector3 position, ItemData item, string displayName)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            go.GetComponent<Renderer>().sharedMaterial = _interactMat;

            var pickup = go.AddComponent<PickupInteractable>();
            var so = new SerializedObject(pickup);
            so.FindProperty("_itemData").objectReferenceValue = item;
            so.FindProperty("_promptText").stringValue = $"Pick up {displayName}";
            so.FindProperty("_interactionType").enumValueIndex = 1; // Pickup
            so.FindProperty("_destroyOnPickup").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LockedDoor BuildLockedDoor(Transform parent, string name,
            Vector3 position, ItemData requiredItem,
            string lockedPrompt, string unlockedPrompt, string saveID)
        {
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = name;
            door.transform.SetParent(parent, false);
            door.transform.position = position + new Vector3(0, H * 0.425f, 0);
            door.transform.localScale = new Vector3(T * 2f, H * 0.85f, CW - 0.1f);
            door.GetComponent<Renderer>().sharedMaterial = _darkMat;

            var lockedDoor = door.AddComponent<LockedDoor>();
            var so = new SerializedObject(lockedDoor);
            so.FindProperty("_requiredItem").objectReferenceValue = requiredItem;
            so.FindProperty("_lockedPrompt").stringValue = lockedPrompt;
            so.FindProperty("_unlockedPrompt").stringValue = unlockedPrompt;
            so.FindProperty("_openHeight").floatValue = H * 0.9f;
            so.FindProperty("_saveID").stringValue = saveID;
            so.ApplyModifiedPropertiesWithoutUndo();

            return lockedDoor;
        }

        private static void BuildSaveStation(Transform parent, Vector3 position, SaveSystem saveSystem)
        {
            GameObject root = new GameObject("SaveStation");
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(root.transform, false);
            pedestal.transform.localPosition = new Vector3(0, 0.2f, 0);
            pedestal.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
            pedestal.GetComponent<Renderer>().sharedMaterial = _darkMat;

            var crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crystal.name = "Crystal";
            crystal.transform.SetParent(root.transform, false);
            crystal.transform.localPosition = new Vector3(0, 1.0f, 0);
            crystal.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);

            var lightGO = new GameObject("Glow");
            lightGO.transform.SetParent(root.transform, false);
            lightGO.transform.localPosition = new Vector3(0, 1.5f, 0);
            var pointLight = lightGO.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(0.2f, 0.6f, 1f);
            pointLight.intensity = 1.2f;
            pointLight.range = 5f;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.8f, 0);
            col.size = new Vector3(0.8f, 1.8f, 0.8f);

            var station = root.AddComponent<SaveStation>();
            var stationSO = new SerializedObject(station);
            stationSO.FindProperty("_promptText").stringValue = "Save / Load Game";
            stationSO.ApplyModifiedPropertiesWithoutUndo();

            var stationUI = root.AddComponent<SaveStationUI>();
            if (saveSystem != null)
            {
                var uiSO = new SerializedObject(stationUI);
                uiSO.FindProperty("_saveSystem").objectReferenceValue = saveSystem;
                uiSO.ApplyModifiedPropertiesWithoutUndo();
            }

            BuildLabel(parent, "SaveLabel", position + new Vector3(0, 2.1f, 0), "SAVE");
        }

        private static GameObject BuildGhost(Transform parent, Vector3 position)
        {
            GameObject ghost = new GameObject("DarkFigure");
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

        private static void BuildTriggerZone(Transform parent, string name,
            Vector3 position, Vector3 size, string eventID)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;

            var zone = go.AddComponent<EventTriggerZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("_specificEventID").stringValue = eventID;
            so.FindProperty("_triggerTag").stringValue = "Player";
            so.FindProperty("_oneTimeOnly").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildColdSpot(Transform parent, Vector3 position)
        {
            // Visual: red-tinted floor patch
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "ColdSpot_Visual";
            visual.transform.SetParent(parent, false);
            visual.transform.position = position + new Vector3(0, 0.02f, 0);
            visual.transform.localScale = new Vector3(2f, 0.04f, 2f);
            visual.GetComponent<Renderer>().sharedMaterial = _dangerMat;
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            var triggerGO = new GameObject("ColdSpot");
            triggerGO.transform.SetParent(parent, false);
            triggerGO.transform.position = position + new Vector3(0, H / 2f, 0);
            var col = triggerGO.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2f, H, 2f);

            var drain = triggerGO.AddComponent<SanityDrainZone>();
            var so = new SerializedObject(drain);
            so.FindProperty("_entryDrain").floatValue = 5f;
            so.FindProperty("_drainPerSecond").floatValue = 3f;
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildLabel(parent, "ColdSpotLabel", position + new Vector3(0, 1.4f, 0),
                "<color=#FF5555>cold spot</color>");
        }

        private static void BuildCabinet(Transform parent, Vector3 position)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Cabinet";
            box.transform.SetParent(parent, false);
            box.transform.position = position + new Vector3(0, 0.3f, 0);
            box.transform.localScale = new Vector3(0.9f, 0.6f, 0.7f);
            box.GetComponent<Renderer>().sharedMaterial = _darkMat;
            box.isStatic = true;
        }

        // =====================================================================
        // GEOMETRY
        // =====================================================================

        private static void BuildRoom(Transform parent, string name, Vector3 center,
            bool openEast = false, bool openWest = false)
        {
            GameObject room = new GameObject(name);
            room.transform.SetParent(parent, false);
            room.transform.position = center;

            float half = ROOM / 2f;
            float halfC = CW / 2f;

            // Floor + ceiling
            MakeBox(room.transform, "Floor", new Vector3(0, -T / 2f, 0),
                new Vector3(ROOM, T, ROOM), _floorMat);
            MakeBox(room.transform, "Ceiling", new Vector3(0, H + T / 2f, 0),
                new Vector3(ROOM, T, ROOM), _ceilMat);

            // North/South always solid
            MakeBox(room.transform, "WallN", new Vector3(0, H / 2f, half),
                new Vector3(ROOM, H, T), _wallMat);
            MakeBox(room.transform, "WallS", new Vector3(0, H / 2f, -half),
                new Vector3(ROOM, H, T), _wallMat);

            // East wall
            if (openEast) BuildOpenWall(room.transform, "WallE", new Vector3(half, H / 2f, 0), halfC);
            else MakeBox(room.transform, "WallE", new Vector3(half, H / 2f, 0),
                new Vector3(T, H, ROOM), _wallMat);

            // West wall
            if (openWest) BuildOpenWall(room.transform, "WallW", new Vector3(-half, H / 2f, 0), halfC);
            else MakeBox(room.transform, "WallW", new Vector3(-half, H / 2f, 0),
                new Vector3(T, H, ROOM), _wallMat);
        }

        /// <summary>East/west wall with a corridor opening in the middle (runs along Z).</summary>
        private static void BuildOpenWall(Transform parent, string name, Vector3 center, float halfOpening)
        {
            float segment = (ROOM / 2f) - halfOpening;

            MakeBox(parent, name + "_F", center + new Vector3(0, 0, -(halfOpening + segment / 2f)),
                new Vector3(T, H, segment), _wallMat);
            MakeBox(parent, name + "_B", center + new Vector3(0, 0, halfOpening + segment / 2f),
                new Vector3(T, H, segment), _wallMat);

            // Header above the opening
            float headerH = H * 0.15f;
            MakeBox(parent, name + "_Header",
                center + new Vector3(0, (H - headerH) / 2f, 0),
                new Vector3(T, headerH, halfOpening * 2f), _wallMat);
        }

        private static void BuildCorridor(Transform parent, string name, Vector3 startPos)
        {
            GameObject corridor = new GameObject(name);
            corridor.transform.SetParent(parent, false);
            corridor.transform.position = startPos;

            Vector3 mid = Vector3.right * (CL / 2f);
            float halfW = CW / 2f;

            MakeBox(corridor.transform, "Floor", mid + new Vector3(0, -T / 2f, 0),
                new Vector3(CL, T, CW), _floorMat);
            MakeBox(corridor.transform, "Ceiling", mid + new Vector3(0, H + T / 2f, 0),
                new Vector3(CL, T, CW), _ceilMat);
            MakeBox(corridor.transform, "WallL", mid + new Vector3(0, H / 2f, halfW),
                new Vector3(CL, H, T), _wallMat);
            MakeBox(corridor.transform, "WallR", mid + new Vector3(0, H / 2f, -halfW),
                new Vector3(CL, H, T), _wallMat);
        }

        private static void MakeBox(Transform parent, string name, Vector3 localPos,
            Vector3 scale, Material mat)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPos;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = mat;
            box.isStatic = true;
        }

        // =====================================================================
        // LIGHTING
        // =====================================================================

        private static void BuildGlobalLighting()
        {
            GameObject dirLight = new GameObject("DirectionalLight");
            dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            var dl = dirLight.AddComponent<Light>();
            dl.type = LightType.Directional;
            dl.intensity = 0.12f;
            dl.color = new Color(0.6f, 0.6f, 0.7f);
            dl.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.09f, 0.09f, 0.11f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.06f);
            RenderSettings.fogDensity = 0.012f;
        }

        private static Light BuildRoomLight(Transform parent, string name, Vector3 position, float intensity)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = ROOM * 1.2f;
            light.color = new Color(0.9f, 0.85f, 0.75f);
            light.shadows = LightShadows.Soft;
            return light;
        }

        private static Light BuildCorridorLight(Transform parent, Vector3 position)
        {
            GameObject go = new GameObject("CorridorLight");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 0.4f;
            light.range = CL + 2f;
            light.color = new Color(0.7f, 0.65f, 0.6f);
            return light;
        }

        // =====================================================================
        // HUD / HELP
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

            GameObject crosshairGo = new GameObject("Crosshair");
            crosshairGo.transform.SetParent(canvasGo.transform, false);
            var crossRT = crosshairGo.AddComponent<RectTransform>();
            crossRT.anchorMin = new Vector2(0.5f, 0.5f);
            crossRT.anchorMax = new Vector2(0.5f, 0.5f);
            crossRT.sizeDelta = new Vector2(8, 8);
            var crossImg = crosshairGo.AddComponent<Image>();
            crossImg.color = new Color(1f, 1f, 1f, 0.6f);

            GameObject promptGo = new GameObject("PromptText");
            promptGo.transform.SetParent(canvasGo.transform, false);
            var promptRT = promptGo.AddComponent<RectTransform>();
            promptRT.anchorMin = new Vector2(0.5f, 0.35f);
            promptRT.anchorMax = new Vector2(0.5f, 0.35f);
            promptRT.sizeDelta = new Vector2(600, 40);
            var promptText = promptGo.AddComponent<Text>();
            promptText.text = "";
            promptText.fontSize = 20;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var interUI = canvasGo.AddComponent<InteractionUI>();
            var uiSO = new SerializedObject(interUI);
            uiSO.FindProperty("_promptText").objectReferenceValue = promptText;
            uiSO.FindProperty("_crosshair").objectReferenceValue = crossImg;
            uiSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHelpUI()
        {
            GameObject canvasGo = new GameObject("HelpCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GameObject helpGo = new GameObject("HelpText");
            helpGo.transform.SetParent(canvasGo.transform, false);
            var rt = helpGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(15, -15);
            rt.sizeDelta = new Vector2(460, 220);

            var tmp = helpGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 15;
            tmp.color = new Color(0.6f, 0.8f, 0.6f, 0.65f);
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.text =
                "<b>FRACTURED ECHOES — Demo</b>\n" +
                "WASD Move | Shift Sprint | C Crouch | E Interact\n" +
                "Tab Inventory | 1-5 Hotbar | Esc Pause | F5/F9 Save/Load\n\n" +
                "Read the notes — they tell you where to go.\n" +
                "Find the key. Solve the candles. Reach the cellar.";
        }

        private static void BuildLabel(Transform parent, string name, Vector3 worldPos,
            string text)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = 0.07f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.7f, 0.7f, 0.75f, 0.75f);
        }

        // =====================================================================
        // MATERIALS / ASSET HELPERS
        // =====================================================================

        private static void CreateMaterials()
        {
            _floorMat = MakeMat("DemoFloor", new Color(0.16f, 0.15f, 0.17f));
            _wallMat = MakeMat("DemoWall", new Color(0.26f, 0.24f, 0.25f));
            _ceilMat = MakeMat("DemoCeiling", new Color(0.10f, 0.10f, 0.12f));
            _darkMat = MakeMat("DemoDark", new Color(0.04f, 0.04f, 0.05f));
            _paperMat = MakeMat("DemoPaper", new Color(0.85f, 0.80f, 0.68f));
            _interactMat = MakeMat("DemoInteract", new Color(0.40f, 0.55f, 0.35f));
            _puzzleMat = MakeMat("DemoPuzzle", new Color(0.50f, 0.35f, 0.25f));
            _dangerMat = MakeMat("DemoDanger", new Color(0.55f, 0.10f, 0.10f));
        }

        private static Material MakeMat(string name, Color color)
        {
            string path = $"Assets/Art/Materials/{name}.mat";
            EnsureFolder("Assets/Art/Materials");

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null && existing.shader != null) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(path);

            string[] candidates = {
                "HDRP/Lit", "HD Render Pipeline/Lit",
                "Universal Render Pipeline/Lit", "Standard", "Unlit/Color"
            };

            Shader shader = null;
            foreach (var c in candidates)
            {
                shader = Shader.Find(c);
                if (shader != null) break;
            }

            if (shader == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:Shader Lit");
                foreach (var guid in guids)
                {
                    shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
                    if (shader != null) break;
                }
            }

            var material = new Material(shader != null ? shader : Shader.Find("UI/Default"));
            material.color = color;
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

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

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

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
