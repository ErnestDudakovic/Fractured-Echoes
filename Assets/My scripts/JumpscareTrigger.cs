using System.Collections;
using UnityEngine;

// JumpscareTrigger - a one-shot scare fired by walking into a trigger volume.
//
// Deliberately does NOT spawn anything that can hurt the player. In a no-combat
// game the scare is the payload; if every bang is followed by a real threat the
// player learns to brace, and bracing is the opposite of fear.
//
// SETUP: empty GameObject -> Box Collider with "Is Trigger" ticked -> this
// component -> fill in whichever effects you want. All fields are optional.
[DisallowMultipleComponent]
public class JumpscareTrigger : MonoBehaviour
{
    [Header("Sound")]
    [Tooltip("The bang. Door slam, crash, scream. Played at SoundOrigin if set.")]
    [SerializeField] AudioClip Sound;
    [SerializeField] float Volume = 0.9f;
    [Tooltip("Where the sound comes from. Leave empty to play at this trigger.")]
    [SerializeField] Transform SoundOrigin;

    [Header("Door slam")]
    [Tooltip("Door to snap shut. Works with the DoorScript doors in this scene.")]
    [SerializeField] GameObject DoorToSlam;

    [Header("Lights")]
    [Tooltip("Lights killed the instant this fires.")]
    [SerializeField] Light[] LightsToKill;
    [Tooltip("Seconds until the lights come back. 0 leaves them off for good.")]
    [SerializeField] float LightsReturnAfter = 6.0f;

    [Header("Reveal")]
    [Tooltip("Switched on when this fires. A figure, a moved chair, a body.")]
    [SerializeField] GameObject ObjectToReveal;
    [Tooltip("Seconds before the revealed object disappears. 0 keeps it forever.")]
    [SerializeField] float RevealFor = 3.0f;

    [Header("Effect")]
    [Tooltip("Sanity taken instantly. Keep it modest - the scare is the point.")]
    [SerializeField] float SanityHit = 10.0f;
    [Tooltip("Delay between entering the trigger and the scare. A short pause hurts more.")]
    [SerializeField] float Delay = 0.35f;

    [Header("Repeat")]
    [Tooltip("Fire once ever. Leave this on - a scare you can farm is not a scare.")]
    [SerializeField] bool OneShot = true;
    [Tooltip("Only used when OneShot is off.")]
    [SerializeField] float RearmDelay = 240.0f;
    [Tooltip("Chance it actually fires. Below 1 keeps repeat playthroughs honest.")]
    [Range(0.0f, 1.0f)][SerializeField] float Chance = 1.0f;

    private bool Spent;
    private float NextAllowed;

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Spent) return;
        if (Time.time < NextAllowed) return;
        if (!PlayerRef.IsPlayer(other)) return;

        if (OneShot) Spent = true;
        else NextAllowed = Time.time + RearmDelay;

        if (Random.value > Chance) return;

        StartCoroutine(Fire());
    }

    private IEnumerator Fire()
    {
        if (Delay > 0.0f) yield return new WaitForSeconds(Delay);

        if (Sound != null)
        {
            Vector3 at = SoundOrigin != null ? SoundOrigin.position : transform.position;
            AudioSource.PlayClipAtPoint(Sound, at, Volume);
        }

        if (DoorToSlam != null)
            DoorToSlam.SendMessage("DoorClose", SendMessageOptions.DontRequireReceiver);

        if (LightsToKill != null)
        {
            for (int i = 0; i < LightsToKill.Length; i++)
                if (LightsToKill[i] != null) LightsToKill[i].enabled = false;
        }

        if (ObjectToReveal != null)
            ObjectToReveal.SetActive(true);

        if (SanityHit > 0.0f && SanityScript.Instance != null)
            SanityScript.Instance.Jumpscare(SanityHit);

        if (RevealFor > 0.0f && ObjectToReveal != null)
        {
            yield return new WaitForSeconds(RevealFor);
            ObjectToReveal.SetActive(false);
        }

        if (LightsReturnAfter > 0.0f && LightsToKill != null)
        {
            yield return new WaitForSeconds(Mathf.Max(0.0f, LightsReturnAfter - RevealFor));

            for (int i = 0; i < LightsToKill.Length; i++)
                if (LightsToKill[i] != null) LightsToKill[i].enabled = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;

        Gizmos.color = new Color(0.9f, 0.4f, 0.1f, 0.3f);
        Gizmos.DrawCube(c.bounds.center, c.bounds.size);

        if (SoundOrigin != null)
        {
            Gizmos.color = new Color(0.9f, 0.4f, 0.1f, 0.9f);
            Gizmos.DrawLine(transform.position, SoundOrigin.position);
        }
    }
}
