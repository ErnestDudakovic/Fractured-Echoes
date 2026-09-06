using UnityEngine;

// PlayerRef - reliable "is this collider the player?" check.
//
// In HorrorScene only the child object "PlayerDetectionZone" carries the
// "Player" tag; the FPSController root that actually owns the CharacterController
// is untagged. A plain CompareTag("Player") therefore misses most trigger hits,
// which is why trigger volumes can look like they simply do not fire.
//
// This checks the whole player hierarchy instead.
public static class PlayerRef
{
    public static bool IsPlayer(Collider other)
    {
        if (other == null) return false;

        Transform player = SaveScript.PlayerChar;
        if (player != null && other.transform.IsChildOf(player.root)) return true;

        if (other.CompareTag("Player")) return true;

        if (other.GetComponentInParent<CharacterController>() != null) return true;

        return false;
    }
}
