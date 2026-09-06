using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// EscapeGate - the way out, on Struct_Fence1_Gate_A_Door at roughly 683, 17, 415.
//
// Walking into it without the Room Key does nothing except tell the player what
// they are missing. With the key, the run is over and the game ends.
//
// SETUP: empty GameObject at the gate -> Box Collider with "Is Trigger" ticked,
// sized to cover the opening -> this component.
[DisallowMultipleComponent]
public class EscapeGate : MonoBehaviour
{
    [Header("Requirement")]
    [Tooltip("Only the Room Key opens the street gate.")]
    [SerializeField] bool NeedsRoomKey = true;

    [Header("Exit")]
    [Tooltip("Seconds of black before the victory scene loads.")]
    [SerializeField] float FadeTime = 1.6f;
    [SerializeField] AudioClip GateSound;

    private bool Used;
    private float LastNagTime = -99.0f;

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Used) return;
        if (!PlayerRef.IsPlayer(other)) return;

        if (NeedsRoomKey && !SaveScript.RoomKey)
        {
            Nag();
            return;
        }

        Used = true;
        StartCoroutine(Escape());
    }

    private void OnTriggerStay(Collider other)
    {
        if (Used) return;
        if (!NeedsRoomKey || SaveScript.RoomKey) return;
        if (!PlayerRef.IsPlayer(other)) return;

        Nag();
    }

    private void Nag()
    {
        if (Time.time - LastNagTime < 4.0f) return;
        LastNagTime = Time.time;

        Debug.Log("The gate is chained. Something out east holds the key.");
    }

    private IEnumerator Escape()
    {
        if (GateSound != null)
            AudioSource.PlayClipAtPoint(GateSound, transform.position, 0.9f);

        if (EscapeSequence.Instance != null)
            EscapeSequence.Instance.End();

        // Getting out is the win condition, so make sure a death cannot fire
        // from a sanity tick during the fade.
        SaveScript.Sanity = 100.0f;

        yield return new WaitForSeconds(FadeTime);

        SceneManager.LoadScene(GameScenes.Victory);
    }

    private void OnDrawGizmosSelected()
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;

        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.3f);
        Gizmos.DrawCube(c.bounds.center, c.bounds.size);
    }
}
