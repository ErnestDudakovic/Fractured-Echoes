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
