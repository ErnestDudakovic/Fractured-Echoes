using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pickups : MonoBehaviour
{
    RaycastHit hit;
    [SerializeField] float Distance = 4.0f;
    [SerializeField] GameObject PickupMessage;
    [SerializeField] GameObject PlayerArms;
    private AudioSource MyPlayer;

    private float RayDistance;
    private bool CanSeePickup = false;

    private bool CanSeeDoor = false;
    [SerializeField] GameObject DoorMessage;
    [SerializeField] Text DoorText;


    // Start is called before the first frame update
    void Start()
    {
        PickupMessage.gameObject.SetActive(false);
        DoorMessage.gameObject.SetActive(false);
        PlayerArms.gameObject.SetActive(false);
        RayDistance = Distance;
        MyPlayer = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        // While a note is open the world is paused, so ignore all interaction.
        if (NoteUI.IsOpen)
        {
            PickupMessage.gameObject.SetActive(false);
            DoorMessage.gameObject.SetActive(false);
            return;
        }

        if (Physics.Raycast(transform.position, transform.forward, out hit, RayDistance))
        {
            if (hit.transform.tag == "Note")
            {
                NoteInteractable note = hit.transform.GetComponent<NoteInteractable>();

                if (note != null)
                {
                    CanSeeDoor = true;
                    DoorText.text = note.Prompt;

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        note.Read();
                    }
                }
            }
            else if (hit.transform.tag == "Apple")
            {
                CanSeePickup = true;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (SaveScript.Apples < 6)
                    {
                        Destroy(hit.transform.gameObject);
                        SaveScript.ApplesLeft--;
                        SaveScript.Apples += 1;
                        MyPlayer.Play();
                    }
                }
            }
            else if (hit.transform.tag == "Battery")
            {
                CanSeePickup = true;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (SaveScript.Batteries < 4)
                    {
                        Destroy(hit.transform.gameObject);
                        SaveScript.BatteriesLeft--;
                        SaveScript.Batteries += 1;
                        MyPlayer.Play();
                    }
                }
            }
            else if (hit.transform.tag == "Knife"
                  || hit.transform.tag == "Axe"
                  || hit.transform.tag == "Bat"
                  || hit.transform.tag == "Gun"
                  || hit.transform.tag == "Crossbow"
                  || hit.transform.tag == "Ammo"
                  || hit.transform.tag == "Arrows")
            {
                // No-combat horror: weapons and ammo are inert scenery.
                // Flip GameRules.WeaponsEnabled to bring the old behaviour back.
                if (!GameRules.WeaponsEnabled)
                {
                    CanSeePickup = false;
                    CanSeeDoor = false;
                }
                else
                {
                    CanSeePickup = true;

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        string t = hit.transform.tag;

                        if (t == "Knife" && SaveScript.Knife == false)
                        {
                            Destroy(hit.transform.gameObject); SaveScript.Knife = true; MyPlayer.Play();
                        }
                        else if (t == "Axe" && SaveScript.Axe == false)
                        {
                            Destroy(hit.transform.gameObject); SaveScript.Axe = true; MyPlayer.Play();
                        }
                        else if (t == "Bat" && SaveScript.Bat == false)
                        {
                            Destroy(hit.transform.gameObject); SaveScript.Bat = true; MyPlayer.Play();
                        }
                        else if (t == "Gun" && SaveScript.Gun == false)
                        {
                            Destroy(hit.transform.gameObject); SaveScript.Gun = true; MyPlayer.Play();
                        }
                        else if (t == "Crossbow" && SaveScript.Crossbow == false)
                        {
                            Destroy(hit.transform.gameObject); SaveScript.Crossbow = true; MyPlayer.Play();
                        }
                        else if (t == "Ammo" && SaveScript.BulletClips < 4)
                        {
                            Destroy(hit.transform.gameObject); SaveScript.AmmoLeft--; SaveScript.BulletClips++; MyPlayer.Play();
                        }
                        else if (t == "Arrows" && SaveScript.ArrowRefill == false)
                        {
                            Destroy(hit.transform.gameObject); SaveScript.ArrowsLeft--; SaveScript.ArrowRefill = true; MyPlayer.Play();
                        }
                    }
                }
            }
            else if (hit.transform.tag == "CabinKey")
            {
                CanSeePickup = true;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (SaveScript.CabinKey == false)
                    {
                        Destroy(hit.transform.gameObject);
                        SaveScript.CabinKey = true;
                        MyPlayer.Play();
                    }
                }

            }
            else if (hit.transform.tag == "HouseKey")
            {
                CanSeePickup = true;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (SaveScript.HouseKey == false)
                    {
                        Destroy(hit.transform.gameObject);
                        SaveScript.HouseKey = true;
                        MyPlayer.Play();
                    }
                }

            }
            else if (hit.transform.tag == "RoomKey")
            {
                CanSeePickup = true;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (SaveScript.RoomKey == false)
                    {
                        Destroy(hit.transform.gameObject);
                        SaveScript.RoomKey = true;
                        MyPlayer.Play();
                    }
                }

            }
            else if (hit.transform.tag == "Door")
            {
                CanSeeDoor = true;
                if (hit.transform.gameObject.GetComponent<DoorScript>().Locked == false)
                {
                    if (hit.transform.gameObject.GetComponent<DoorScript>().IsOpen == false)
                    {
                        DoorText.text = "Press E to open";
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            hit.transform.gameObject.SendMessage("DoorOpen");
                        }
                    }
                    else if (hit.transform.gameObject.GetComponent<DoorScript>().IsOpen == true)
                    {
                        DoorText.text = "Press E to close";
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            hit.transform.gameObject.SendMessage("DoorClose");
                        }
                    }
                   /* if (Input.GetKeyDown(KeyCode.E))
                    {
                        hit.transform.gameObject.SendMessage("DoorOpen");
                    }*/
                }
                else if (hit.transform.gameObject.GetComponent<DoorScript>().Locked == true)
                {
                    DoorText.text = "You need the " + hit.transform.gameObject.GetComponent<DoorScript>().DoorType + " key";
                }

            }

            else
            {
                CanSeePickup = false;
                CanSeeDoor = false;
            }
        }

        if(CanSeePickup == true)
        {
            PickupMessage.gameObject.SetActive(true);
            RayDistance = 1000f;
        }
        if (CanSeePickup == false)
        {
            PickupMessage.gameObject.SetActive(false);
            RayDistance = Distance;
        }

        if (CanSeeDoor == true)
        {
            DoorMessage.gameObject.SetActive(true);
            RayDistance = 1000f;
        }
        if (CanSeeDoor == false)
        {
            DoorMessage.gameObject.SetActive(false);
            RayDistance = Distance;
        }


    }
}
