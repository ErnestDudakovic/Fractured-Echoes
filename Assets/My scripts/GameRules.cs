using UnityEngine;

// GameRules - one place to switch the game between its two modes.
//
// Fractured Echoes is a no-combat psychological horror game: the player runs,
// hides and reads, and never gets a weapon. Weapons are gated behind this flag
// rather than deleted from the code, so the original shooter behaviour can be
// restored by flipping one bool instead of rewriting five scripts.
//
// This only stops weapons from being picked up, equipped and used. To also
// clear the weapon props out of HorrorScene, run
// Tools > Fractured Echoes > Remove Weapons From Scene.
public static class GameRules
{
    /// <summary>False = no-combat horror. True = original shooter behaviour.</summary>
    public static readonly bool WeaponsEnabled = false;
}
