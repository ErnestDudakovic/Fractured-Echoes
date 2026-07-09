// ============================================================================
// NoteInteractable.cs — Readable story note in the game world
// Press E on a note to open a full-screen reading view with title + text.
// The reading UI (NoteUI) is created automatically if not present.
// This is the primary environmental-storytelling delivery tool.
// ============================================================================

using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.UI;

namespace FracturedEchoes.Interaction
{
    /// <summary>
    /// A note, letter, or document the player can read in the world.
    /// Opens the shared <see cref="NoteUI"/> overlay with the configured text.
    /// </summary>
    public class NoteInteractable : InteractableObject
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Note Content")]
        [Tooltip("Title shown at the top of the reading view.")]
        [SerializeField] private string _noteTitle = "Note";

        [TextArea(4, 12)]
        [Tooltip("The story text of this note.")]
        [SerializeField] private string _noteText = "";

        [Header("Note Options")]
        [Tooltip("Sanity change applied the first time this note is read (negative = drain).")]
        [SerializeField] private float _sanityOnFirstRead = 0f;

        // =====================================================================
        // RUNTIME
        // =====================================================================

        private bool _hasBeenRead;
        private Player.SanitySystem _cachedSanity;

        /// <summary>True once the player has read this note at least once.</summary>
        public bool HasBeenRead => _hasBeenRead;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        protected override void Awake()
        {
            base.Awake();
            _cachedSanity = FindFirstObjectByType<Player.SanitySystem>();

            // Make sure a NoteUI exists in the scene
            NoteUI.EnsureExists();
        }

        // =====================================================================
        // INTERACTION
        // =====================================================================

        public override void OnInteract()
        {
            if (!CanInteract) return;

            NoteUI.Show(_noteTitle, _noteText);

            // Optional sanity effect on first read (disturbing content)
            if (!_hasBeenRead && Mathf.Abs(_sanityOnFirstRead) > 0.01f && _cachedSanity != null)
            {
                if (_sanityOnFirstRead < 0f)
                    _cachedSanity.DrainSanity(-_sanityOnFirstRead);
                else
                    _cachedSanity.RestoreSanity(_sanityOnFirstRead);
            }

            _hasBeenRead = true;

            base.OnInteract();
        }

        /// <summary>Sets the note content at runtime (for generated notes).</summary>
        public void SetContent(string title, string text)
        {
            _noteTitle = title;
            _noteText = text;
        }
    }
}
