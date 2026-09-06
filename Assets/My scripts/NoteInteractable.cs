using UnityEngine;

// NoteInteractable - a readable note lying in the world.
//
// SETUP: put this on any object (a paper mesh, a quad, a book prop), set its
// tag to "Note", give it a Collider, then fill in Title and Body.
// Pickups.cs raycasts for the "Note" tag and calls Read() when you press E.
//
// Notes are not destroyed after reading, so the player can come back to one
// they half-remember. Only the first read counts toward the collected total.
[DisallowMultipleComponent]
public class NoteInteractable : MonoBehaviour
{
    [Header("Content")]
    [Tooltip("Shown at the top of the page.")]
    [SerializeField] string Title = "Untitled";

    [TextArea(6, 20)]
    [Tooltip("The body of the note. Blank lines are kept.")]
    [SerializeField] string Body = "";

    [Header("Optional")]
    [Tooltip("Played once the first time this note is read.")]
    [SerializeField] AudioClip ReadSound;

    [Tooltip("Sanity change the first time it is read. Negative for disturbing notes.")]
    [SerializeField] float SanityOnFirstRead = 0.0f;

    [Tooltip("Hide the object once it has been read.")]
    [SerializeField] bool DisappearAfterReading = false;

    private bool HasBeenRead;

    /// <summary>The prompt Pickups.cs shows while you are looking at this note.</summary>
    public string Prompt
    {
        get { return HasBeenRead ? "Press E to read again" : "Press E to read"; }
    }

    public void Read()
    {
        if (NoteUI.Instance == null) return;

        NoteUI.Instance.Show(Title, Body);

        if (HasBeenRead) return;
        HasBeenRead = true;

        SaveScript.NotesFound++;

        if (ReadSound != null)
            AudioSource.PlayClipAtPoint(ReadSound, transform.position, 0.6f);

        if (SanityOnFirstRead != 0.0f && SanityScript.Instance != null)
        {
            if (SanityOnFirstRead < 0.0f) SanityScript.Instance.Drain(-SanityOnFirstRead);
            else SanityScript.Instance.Restore(SanityOnFirstRead);
        }

        if (DisappearAfterReading)
            Invoke("HideSelf", 0.1f);
    }

    private void HideSelf()
    {
        gameObject.SetActive(false);
    }
}
