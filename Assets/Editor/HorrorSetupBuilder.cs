using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

// Builds the horror layer for HorrorScene: sanity zones, readable notes and
// phantom spawners, positioned against real landmarks in the level.
//
// Run from: Tools > Fractured Echoes > Build Horror Setup
//
// Everything lands under a single root called "HORROR SETUP". Rebuilding KEEPS
// anything you moved by hand - positions are captured before the rebuild and
// restored after. Use "Build Horror Setup (RESET positions)" only when you
// deliberately want the coordinates in this file to win.
public static class HorrorSetupBuilder
{
    private const string RootName = "HORROR SETUP";

    private const string ClipHell0 = "Assets/Horror Sfx/Wav/Ambience_Hell_00.wav";
    private const string ClipHell1 = "Assets/Horror Sfx/Wav/Ambience_Hell_01.wav";
    private const string ClipHell2 = "Assets/Horror Sfx/Wav/Ambience_Hell_02.wav";
    private const string ClipBelly = "Assets/Horror Sfx/Wav/Ambience_MonstersBelly_00.wav";
    private const string ClipMachine = "Assets/Horror Sfx/Wav/Evil_Machine_Loop_00.wav";
    private const string EnemyPrefab = "Assets/Enemies/Prefabs/Enemy1.prefab";

    [MenuItem("Tools/Fractured Echoes/Build Horror Setup")]
    public static void Build()
    {
        BuildInternal(true);
    }

    [MenuItem("Tools/Fractured Echoes/Build Horror Setup (RESET positions)")]
    public static void BuildReset()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Reset positions",
            "This throws away every position you moved by hand and puts everything " +
            "back where the script says.\n\nUse the normal Build if you want to keep your placement.",
            "Reset everything", "Cancel");

        if (!ok) return;

        BuildInternal(false);
    }

    private static void BuildInternal(bool keepPlacement)
    {
        GameObject old = GameObject.Find(RootName);

        Dictionary<string, Vector3> saved = keepPlacement ? CapturePlacement(old) : null;

        if (old != null) Object.DestroyImmediate(old);

        GameObject root = new GameObject(RootName);

        Transform zones = NewChild(root.transform, "Sanity Zones");
        Transform notes = NewChild(root.transform, "Notes");
        Transform phantoms = NewChild(root.transform, "Phantoms");
        Transform escape = NewChild(root.transform, "Escape");

        BuildZones(zones);
        BuildNotes(notes);
        BuildPhantoms(phantoms);
        BuildEscape(escape);

        int restored = 0;
        if (saved != null) restored = ApplyPlacement(root, saved);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        string msg = "HORROR SETUP built. 9 sanity zones, 10 notes, 4 phantom spawners, escape sequence + gate.";
        if (keepPlacement) msg += " Kept " + restored + " hand-placed positions.";
        Debug.Log(msg);
    }

    // ------------------------------------------------------- keep placement

    // Notes are matched on their number ("Note 03") rather than their full name,
    // so rewriting or retitling a note does not lose where you put it.
    private static string PlacementKey(Transform t)
    {
        string n = t.name;

        if (n.StartsWith("Note ") && n.Length >= 7)
            return "note:" + n.Substring(5, 2);

        Transform parent = t.parent;
        string prefix = parent != null ? parent.name + "/" : "";
        return prefix + n;
    }

    private static Dictionary<string, Vector3> CapturePlacement(GameObject root)
    {
        Dictionary<string, Vector3> map = new Dictionary<string, Vector3>();
        if (root == null) return map;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == root.transform) continue;

            string key = PlacementKey(all[i]);
            if (!map.ContainsKey(key)) map.Add(key, all[i].position);
        }

        return map;
    }

    private static int ApplyPlacement(GameObject root, Dictionary<string, Vector3> saved)
    {
        int n = 0;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        System.Array.Sort(all, delegate (Transform a, Transform b)
        {
            return Depth(a).CompareTo(Depth(b));
        });

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == root.transform) continue;

            Vector3 pos;
            if (!saved.TryGetValue(PlacementKey(all[i]), out pos)) continue;

            all[i].position = pos;
            n++;
        }

        return n;
    }

    private static int Depth(Transform t)
    {
        int d = 0;
        while (t.parent != null) { d++; t = t.parent; }
        return d;
    }

    // ================================================================= zones

    private static void BuildZones(Transform parent)
    {
        // Dread zones - the only places that drain sanity over time.
        Zone(parent, "Dread - Church and Churchyard", new Vector3(650f, 20f, 495f), new Vector3(40, 20, 50), -4.0f, false, ClipHell0, 0.55f);
        Zone(parent, "Dread - House 1", new Vector3(386f, 29f, 682f), new Vector3(46, 30, 56), -6.0f, false, ClipMachine, 0.70f);
        Zone(parent, "Dread - Main House", new Vector3(524f, 24f, 530f), new Vector3(48, 24, 48), -3.5f, false, ClipHell1, 0.60f);
        Zone(parent, "Dread - Cabins and Latrine", new Vector3(425f, 20f, 480f), new Vector3(50, 20, 70), -3.0f, false, ClipHell2, 0.55f);
        Zone(parent, "Dread - Outbuildings 2", new Vector3(686f, 22f, 450f), new Vector3(30, 18, 40), -2.5f, false, ClipHell0, 0.45f);
        Zone(parent, "Dread - Flooded Building", new Vector3(815f, 22f, 512f), new Vector3(44, 24, 50), -4.5f, false, ClipBelly, 0.60f);

        // Safe zones - sanity comes back here.
        Zone(parent, "Safe - Silo Start", new Vector3(719f, 22f, 268f), new Vector3(30, 20, 30), 6.0f, true, null, 0f);
        Zone(parent, "Safe - Lit Crossroads", new Vector3(623f, 23f, 430f), new Vector3(30, 20, 30), 4.0f, true, null, 0f);
        Zone(parent, "Safe - Bridge", new Vector3(452f, 23f, 410f), new Vector3(28, 20, 28), 4.0f, true, null, 0f);
    }

    private static void Zone(Transform parent, string name, Vector3 pos, Vector3 size,
                             float rate, bool safe, string clipPath, float volume)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        BoxCollider box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;

        SanityZone zone = go.AddComponent<SanityZone>();

        SerializedObject so = new SerializedObject(zone);
        so.FindProperty("RatePerSecond").floatValue = rate;
        so.FindProperty("CountsAsSafe").boolValue = safe;
        so.FindProperty("MusicVolume").floatValue = volume;
        so.FindProperty("FadeIn").floatValue = 1.5f;
        so.FindProperty("FadeOut").floatValue = 2.5f;
        so.FindProperty("OneShotOnEnter").boolValue = false;
        so.FindProperty("OneShotAmount").floatValue = -15f;

        if (!string.IsNullOrEmpty(clipPath))
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null) Debug.LogWarning("Audio clip not found: " + clipPath);
            so.FindProperty("Music").objectReferenceValue = clip;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================================================================= notes

    private static void BuildNotes(Transform parent)
    {
        // ---- 01  SILO, at the start. Tutorial and "cross the river".
        Note(parent, 1, "Where You Woke Up", new Vector3(719f, 21.4f, 268f), 0f,
@"You do not remember the water.

You were on the far bank an hour ago and now you are here,
soaked to the knee, holding nothing. Fine. Deal with it.

What you have: a flashlight and whatever you can carry.
E picks things up and opens doors. F is the light. I opens
your bag. Batteries die, so take every one you see.

What you do not have: a weapon. Do not look for one. If
something comes at you, run. They lose interest. They
always lose interest, eventually.

The compound is across the river. Buildings, a church, a
big house up the hill. Somebody lived here until recently
enough that the lamps still work.

Cross the water. Find out what happened here.
Find the way back out.");

        // ---- 02  CABIN 1. The prisoners' side of the story.
        Note(parent, 2, "Bunk 4", new Vector3(447f, 18f, 518f), -4f,
@"Day nine in this hut and I still cannot sleep past three.

It is not the cold. At about three the whispering starts
from the direction of the church and it does not stop until
the sky goes grey. Karel says it is the wind in the bell
housing. Karel has not been outside after dark since Tuesday.

They locked our cabins from the outside ""for our safety.""
One key for the whole row, and it lives up at the big house
with the doctor. Of course it does.

If you can read this you got in somehow, so you are already
doing better than I did.");

        // ---- 03  LATRINE 1. Scratched, not written. Points at the church.
        Note(parent, 3, "Scratched Into The Door", new Vector3(408f, 17.5f, 472f), -6f,
@"i sat in here four hours

it walked past the door twice and stopped both times and
then it went away. they do that. they get bored. thats the
only rule that has ever held

the big house is locked and the doctor has the key but the
doctor is not in the big house

hes at the church

hes been at the church since sunday");

        // ---- 04  HOUSE 1, far north-west. Optional, deepest dread, names Halloway.
        Note(parent, 4, "Ward Register - Building One", new Vector3(386f, 27f, 680f), -8f,
@"Building One decommissioned. All twelve moved down to the
cabins on Dr. Halloway's instruction. He wants them closer
to the house. He wants them where he can hear them.

I asked what we were treating. He said memory. I asked what
was wrong with their memory. He said nothing was wrong with
it, that was the difficulty.

Left the beds. Left the files. Left the lamps burning
because none of us wanted to be the last one out.

Whoever comes up here after us - take the batteries from
the store cupboard. You will not want to walk back down
in the dark.");

        // ---- 05  CHURCH STEPS, beside the House Key.
        Note(parent, 5, "Left On The Church Steps", new Vector3(646f, 18f, 512f), -8f,
@"Doctor,

I have knocked for three days. I know you are in there
because I can hear you answering, and I know it is not you
answering, because you answer before I finish asking.

I am leaving the house key here on the step. I am not
going back up to that building and I am not carrying the
only key into the water with me.

Whoever finds this: the big house is his. Everything he
wrote is in there. Read it before you decide he was mad.

Then read it again and decide properly.");

        // ---- 06  CHURCHYARD, among the gravestones.
        Note(parent, 6, "The Burial List", new Vector3(655f, 17.5f, 482f), -12f,
@"Interred, east plot, in order of the register:

   Anselm, K.      14th
   Bauer, M.       14th
   Reisz, T.       14th
   Novak, P.       14th
   Halloway, E.    14th
   Karel, J.       14th
   Weiss, A.       14th
   Bruck, S.       14th
   Fischer, D.     14th
   Adler, R.       14th
   Toma, L.        14th

   and one more, admitted late, no date entered,
   because the date has not happened yet.

All on the same day. Nobody wrote down who did the burying.");

        // ---- 07  MAIN HOUSE, ground floor. Halloway in his own words.
        Note(parent, 7, "Consultation Notes", new Vector3(522f, 22f, 528f), -6f,
@"Subject 12. Fourth session.

Reports the same intrusive image: standing on the far bank
of the river, wet to the knee, unable to account for the
crossing. Reports it as memory, not dream. Insists it has
already happened and is also about to happen.

I have stopped correcting him.

Note for the file: the image is not his. I have had it
myself for six weeks. So has the night warden. So, I now
understand, has everyone who has spent a full night inside
the fence.

We are not treating them. We are catching it.");

        // ---- 08  MAIN HOUSE upper floor, beside the Cabin Key.
        Note(parent, 8, "The Last Entry", new Vector3(529f, 28.3f, 543f), -10f,
@"We locked the cabins from the outside tonight.

Not to keep them in. To keep the count honest. If the doors
are bolted and the number in the morning is still wrong,
then the problem is not that somebody is wandering.

The number was wrong.

Cabin key stays up here with me. If you are holding it, you
have been in my house, and you have read enough to know
that opening those doors will not help you.

Open them anyway. They wrote things on the walls that I
was never allowed to see.");

        // ---- 09  CABIN 3, behind the locked door. Points east, to the water.
        Note(parent, 9, "Written On The Cabin Wall", new Vector3(416f, 18f, 464f), -8f,
@"we counted ourselves every night by touching the bunks

twelve bunks twelve hands then thirteen hands then twelve
again and nobody would say which of us it had been

halloway went east when the water came up. the building
standing in the flood, the one you can see from the road
when the fog thins

he took the last key with him

karel says the gate on the street is the only way through
the fence and karel was right about everything else");

        // ---- 10  FLOODED BUILDING, beside the Room Key. Starts the escape.
        Note(parent, 10, "Halloway's Final Page", new Vector3(814f, 26.9f, 512f), -15f,
@"I came out here because the water is the only thing on this
site that does not repeat.

I have worked out what we are. I will not write it plainly
because you already know, and if I write it plainly you
will have to agree with me.

The key in my hand opens the street gate. It opens nothing
else. There is nothing else worth opening.

Take it and go. Do not stop at the house. Do not stop at
the church. The moment you lift this key they will know
where you are, all of them, at once, and they will not get
bored this time.

Run for the gate.
I did not.");
    }

    private static void Note(Transform parent, int index, string title, Vector3 pos, float sanity, string body)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Note " + index.ToString("00") + " - " + title;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.32f, 0.02f, 0.44f);
        go.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        SetTagSafe(go, "Note");

        // The sheet is paper-thin, so widen the collider or the raycast misses it.
        BoxCollider box = go.GetComponent<BoxCollider>();
        box.size = new Vector3(1.6f, 22f, 1.6f);

        NoteInteractable note = go.AddComponent<NoteInteractable>();

        SerializedObject so = new SerializedObject(note);
        so.FindProperty("Title").stringValue = title;
        so.FindProperty("Body").stringValue = body;
        so.FindProperty("SanityOnFirstRead").floatValue = sanity;
        so.FindProperty("DisappearAfterReading").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ============================================================== phantoms

    private static void BuildPhantoms(Transform parent)
    {
        Phantom(parent, "Phantom - Cabin Row", new Vector3(430f, 20f, 480f), new Vector3(30, 14, 40), 0,
            new[] { new Vector3(415f, 17.5f, 462f), new Vector3(447f, 17.5f, 518f) });

        Phantom(parent, "Phantom - Churchyard", new Vector3(650f, 20f, 492f), new Vector3(30, 16, 34), 1,
            new[] { new Vector3(657f, 17.5f, 480f), new Vector3(640f, 17.5f, 505f) });

        Phantom(parent, "Phantom - Main House", new Vector3(524f, 24f, 528f), new Vector3(28, 16, 28), 1,
            new[] { new Vector3(508f, 21f, 528f), new Vector3(540f, 21f, 531f) });

        Phantom(parent, "Phantom - House 1", new Vector3(386f, 27f, 678f), new Vector3(26, 16, 32), 1,
            new[] { new Vector3(383f, 24.9f, 692f), new Vector3(391f, 24.9f, 670f) });
    }

    private static void Phantom(Transform parent, string name, Vector3 pos, Vector3 size, int where, Vector3[] points)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        BoxCollider box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;

        Transform[] spawns = new Transform[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            GameObject p = new GameObject("SpawnPoint " + (i + 1));
            p.transform.SetParent(go.transform, false);
            p.transform.position = points[i];
            spawns[i] = p.transform;
        }

        PhantomSpawner spawner = go.AddComponent<PhantomSpawner>();

        SerializedObject so = new SerializedObject(spawner);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
        if (prefab == null) Debug.LogWarning("Enemy prefab not found: " + EnemyPrefab);
        so.FindProperty("PhantomPrefab").objectReferenceValue = prefab;

        SerializedProperty arr = so.FindProperty("SpawnPoints");
        arr.arraySize = spawns.Length;
        for (int i = 0; i < spawns.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];

        so.FindProperty("Where").enumValueIndex = where;
        so.FindProperty("MinDistance").floatValue = 8f;
        so.FindProperty("MaxDistance").floatValue = 45f;
        so.FindProperty("OneShot").boolValue = false;
        so.FindProperty("RearmDelay").floatValue = 150f;
        so.FindProperty("SpawnDelay").floatValue = 0.6f;
        so.FindProperty("Chance").floatValue = 0.75f;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================================================================ escape

    // The street gate sits on Struct_Fence1_Gate_A_Door. The trigger is a bit
    // wider than the opening so the player cannot squeeze past the finish line.
    private static readonly Vector3 GatePos = new Vector3(683.0f, 18.0f, 415.0f);

    private static void BuildEscape(Transform parent)
    {
        // --- the controller that watches for the Room Key
        GameObject ctrl = new GameObject("Escape Controller");
        ctrl.transform.SetParent(parent, false);
        ctrl.transform.position = GatePos;

        EscapeSequence seq = ctrl.AddComponent<EscapeSequence>();

        SerializedObject so = new SerializedObject(seq);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
        if (prefab == null) Debug.LogWarning("Enemy prefab not found: " + EnemyPrefab);
        so.FindProperty("HunterPrefab").objectReferenceValue = prefab;
        so.FindProperty("GatePosition").vector3Value = GatePos;
        so.ApplyModifiedPropertiesWithoutUndo();

        // --- the gate itself
        GameObject gate = new GameObject("Escape Gate");
        gate.transform.SetParent(parent, false);
        gate.transform.position = GatePos;

        BoxCollider box = gate.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(10f, 8f, 6f);

        EscapeGate eg = gate.AddComponent<EscapeGate>();

        SerializedObject gso = new SerializedObject(eg);
        gso.FindProperty("NeedsRoomKey").boolValue = true;
        gso.FindProperty("FadeTime").floatValue = 1.6f;
        gso.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================================================================ helpers

    [MenuItem("Tools/Fractured Echoes/Capture Note Positions")]
    public static void CaptureNotePositions()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            Debug.LogError("No " + RootName + " in this scene. Run Build Horror Setup first.");
            return;
        }

        Transform notes = root.transform.Find("Notes");
        if (notes == null)
        {
            Debug.LogError("No Notes group under " + RootName + ".");
            return;
        }

        string log = "Current note positions:\n";

        for (int i = 0; i < notes.childCount; i++)
        {
            Vector3 p = notes.GetChild(i).position;
            log += string.Format("{0}  ->  new Vector3({1}f, {2}f, {3}f)\n",
                notes.GetChild(i).name, p.x.ToString("0.##"), p.y.ToString("0.##"), p.z.ToString("0.##"));
        }

        Debug.Log(log);
        EditorGUIUtility.systemCopyBuffer = log;
        Debug.Log("Copied to clipboard.");
    }

    private static Transform NewChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void SetTagSafe(GameObject go, string tag)
    {
        try
        {
            go.tag = tag;
        }
        catch
        {
            Debug.LogError("Tag '" + tag + "' does not exist. Add it in Project Settings > Tags and Layers, then re-run.");
        }
    }
}
