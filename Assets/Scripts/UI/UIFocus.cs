// ============================================================================
// UIFocus.cs — Global UI modal / Escape-key coordination
// Multiple self-building UIs (pause menu, inventory, save station, game over,
// item inspection) each listen for Escape independently, which caused the
// pause menu to open on the same keypress that closed another panel.
// This static helper lets modal UIs register themselves and "consume" the
// Escape press so only one system reacts per frame.
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Tracks which UI modal panels are open and whether the Escape key has
    /// already been handled this frame. The PauseMenuController checks this
    /// before toggling the pause menu.
    /// </summary>
    public static class UIFocus
    {
        private static int _modalCount;
        private static int _escapeConsumedFrame = -1;

        /// <summary>True while any modal UI (inventory, save station, etc.) is open.</summary>
        public static bool AnyModalOpen => _modalCount > 0;

        /// <summary>True if a UI already handled Escape during this frame.</summary>
        public static bool EscapeConsumedThisFrame => _escapeConsumedFrame == Time.frameCount;

        /// <summary>Call when a modal panel opens.</summary>
        public static void RegisterModalOpen() => _modalCount++;

        /// <summary>Call when a modal panel closes.</summary>
        public static void RegisterModalClosed() => _modalCount = Mathf.Max(0, _modalCount - 1);

        /// <summary>Call when a UI reacts to the Escape key this frame.</summary>
        public static void ConsumeEscape() => _escapeConsumedFrame = Time.frameCount;

        // ---------------------------------------------------------------------
        // Interact-key coordination — prevents the E press that closes a UI
        // (e.g. a note) from immediately re-triggering a world interaction.
        // ---------------------------------------------------------------------

        private static int _interactConsumedFrame = -1;

        /// <summary>True if a UI already handled the Interact key during this frame.</summary>
        public static bool InteractConsumedThisFrame => _interactConsumedFrame == Time.frameCount;

        /// <summary>Call when a UI reacts to the Interact (E) key this frame.</summary>
        public static void ConsumeInteract() => _interactConsumedFrame = Time.frameCount;

        // ---------------------------------------------------------------------
        // Reset static state on domain reload / scene changes so a modal left
        // open during a scene transition can't leak its count.
        // ---------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            _modalCount = 0;
            _escapeConsumedFrame = -1;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                _modalCount = 0;
                _escapeConsumedFrame = -1;
                _interactConsumedFrame = -1;
            }
        }
    }
}
