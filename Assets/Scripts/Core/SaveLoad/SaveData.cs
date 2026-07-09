// ============================================================================
// SaveData.cs — Data structures for the save/load system
// Separated from SaveSystem.cs for clarity and reusability.
// ============================================================================

using System;
using System.Collections.Generic;

namespace FracturedEchoes.Core.SaveLoad
{
    /// <summary>
    /// Root save data container. Serialized to/from JSON.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string timestamp;
        public string currentLocation;
        public float playTime;
        public List<SaveEntry> entries;
    }

    /// <summary>
    /// A single save entry for one ISaveable component.
    /// </summary>
    [Serializable]
    public class SaveEntry
    {
        public string saveID;
        public string stateJson;
        public string typeName;
    }

    // =========================================================================
    // PRIMITIVE / COLLECTION WRAPPERS
    // JsonUtility cannot serialize bare primitives (float, int) or bare
    // collections (List<string>) — it produces "{}" and the data is lost.
    // ISaveable implementations must wrap such values in these classes.
    // =========================================================================

    /// <summary>Save-safe wrapper for a single float value.</summary>
    [Serializable]
    public class SaveFloat
    {
        public float value;
    }

    /// <summary>Save-safe wrapper for a single int value.</summary>
    [Serializable]
    public class SaveInt
    {
        public int value;
    }

    /// <summary>Save-safe wrapper for a list of strings.</summary>
    [Serializable]
    public class SaveStringList
    {
        public List<string> values = new List<string>();
    }

    /// <summary>
    /// Metadata about a save slot for the UI.
    /// </summary>
    public class SaveSlotInfo
    {
        public int slotIndex;
        public string timestamp;
        public string locationName;
        public float playTime;
    }
}
