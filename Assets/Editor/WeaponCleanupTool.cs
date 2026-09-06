using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Clears the weapon and ammo props out of the open scene.
//
// The code side is already handled by GameRules.WeaponsEnabled - weapons cannot
// be picked up or equipped regardless. This tool is about the world: a shotgun
// lying on a table still reads as "combat game" even if the player cannot take
// it, and that kills the tension a no-combat horror game depends on.
//
// Run from: Tools > Fractured Echoes > Remove Weapons From Scene
//
// Preview first, then delete. Deletion is undoable with Ctrl+Z.
public static class WeaponCleanupTool
{
    private static readonly string[] WeaponTags =
    {
        "Knife", "Axe", "Bat", "Gun", "Crossbow", "Ammo", "Arrows",
        "PKnife", "PBat", "PAxe", "PCrossbow"
    };

    [MenuItem("Tools/Fractured Echoes/Preview Weapons In Scene")]
    public static void Preview()
    {
        List<GameObject> found = FindWeapons();

        if (found.Count == 0)
        {
            Debug.Log("No weapon or ammo objects found in this scene.");
            return;
        }

        string log = "Found " + found.Count + " weapon/ammo objects:\n";

        for (int i = 0; i < found.Count; i++)
        {
            Vector3 p = found[i].transform.position;
            log += string.Format("  [{0}] {1}  ({2}, {3}, {4})\n",
                found[i].tag, found[i].name, p.x.ToString("0.#"), p.y.ToString("0.#"), p.z.ToString("0.#"));
        }

        log += "\nRun 'Remove Weapons From Scene' to delete them.";
        Debug.Log(log);

        Selection.objects = found.ToArray();
    }

    [MenuItem("Tools/Fractured Echoes/Remove Weapons From Scene")]
    public static void Remove()
    {
        List<GameObject> found = FindWeapons();

        if (found.Count == 0)
        {
            Debug.Log("Nothing to remove: no weapon or ammo objects in this scene.");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "Remove weapons",
            "Delete " + found.Count + " weapon and ammo objects from this scene?\n\n" +
            "This can be undone with Ctrl+Z, and the scene is not saved automatically.",
            "Delete", "Cancel");

        if (!ok) return;

        for (int i = 0; i < found.Count; i++)
            Undo.DestroyObjectImmediate(found[i]);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Removed " + found.Count + " weapon/ammo objects. Save the scene to keep this.");
    }

    private static List<GameObject> FindWeapons()
    {
        List<GameObject> found = new List<GameObject>();

        for (int i = 0; i < WeaponTags.Length; i++)
        {
            GameObject[] tagged;

            try
            {
                tagged = GameObject.FindGameObjectsWithTag(WeaponTags[i]);
            }
            catch
            {
                // Tag not defined in this project - nothing to clean up for it.
                continue;
            }

            for (int j = 0; j < tagged.Length; j++)
            {
                if (!found.Contains(tagged[j])) found.Add(tagged[j]);
            }
        }

        return found;
    }
}
