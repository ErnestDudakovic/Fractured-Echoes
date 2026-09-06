using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reload : MonoBehaviour
{
    [SerializeField] GameObject Knife;
    [SerializeField] GameObject Bat;
    [SerializeField] GameObject Axe;
    [SerializeField] GameObject Gun;
    [SerializeField] GameObject Crossbow;
    [SerializeField] GameObject CabinKey;
    [SerializeField] GameObject HouseKey;
    [SerializeField] GameObject RoomKey;
    [SerializeField] GameObject Enemy1;
    [SerializeField] GameObject Enemy2;
    [SerializeField] GameObject Enemy3;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitToDestroy());
    }

    IEnumerator WaitToDestroy()
    {
        yield return new WaitForSeconds(1);

        // In no-combat mode the weapon pickups are removed from the scene
        // outright, so these references are usually already gone. Guard anyway
        // so a loaded save from the old shooter build cannot throw.
        if (GameRules.WeaponsEnabled)
        {
            if (SaveScript.Knife == true && Knife != null)
            {
                Destroy(Knife.gameObject);
            }
            if (SaveScript.Bat == true && Bat != null)
            {
                Destroy(Bat.gameObject);
            }
            if (SaveScript.Axe == true && Axe != null)
            {
                Destroy(Axe.gameObject);
            }
            if (SaveScript.Gun == true && Gun != null)
            {
                Destroy(Gun.gameObject);
            }
            if (SaveScript.Crossbow == true && Crossbow != null)
            {
                Destroy(Crossbow.gameObject);
            }
        }

        if (SaveScript.CabinKey == true && CabinKey != null)
        {
            Destroy(CabinKey.gameObject);
        }
        if (SaveScript.HouseKey == true && HouseKey != null)
        {
            Destroy(HouseKey.gameObject);
        }
        if (SaveScript.RoomKey == true && RoomKey != null)
        {
            Destroy(RoomKey.gameObject);
        }

        if (SaveScript.Enemy1 == 0 && Enemy1 != null)
        {
            Destroy(Enemy1.gameObject);
        }
        if (SaveScript.Enemy2 == 0 && Enemy2 != null)
        {
            Destroy(Enemy2.gameObject);
        }
        if (SaveScript.Enemy3 == 0 && Enemy3 != null)
        {
            Destroy(Enemy3.gameObject);
        }
    }

}
