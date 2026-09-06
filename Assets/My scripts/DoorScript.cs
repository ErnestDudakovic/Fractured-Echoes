using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : MonoBehaviour
{
    private Animator Anim;
    public bool IsOpen = false;
    private AudioSource MyPlayer;
    [SerializeField] AudioClip CabinSound;
    [SerializeField] AudioClip RoomSound;
    [SerializeField] AudioClip HouseSound;
    [SerializeField] bool Cabin;
    [SerializeField] bool Room;
    [SerializeField] bool House;
    public bool Locked;
    public string DoorType;

    // Start is called before the first frame update
    void Start()
    {
        Anim = GetComponent<Animator>();
        MyPlayer = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
       if(Cabin == true)
        {
            DoorType = "Cabin";
            if(SaveScript.CabinKey == true)
            {
                Locked = false;
            }
        }
        if (Room == true)
        {
            DoorType = "Room";
            if (SaveScript.RoomKey == true)
            {
                Locked = false;
            }
        }
        if (House == true)
        {
            DoorType = "House";
            if (SaveScript.HouseKey == true)
            {
                Locked = false;
            }
        }
    }

    public void DoorOpen()
    {
        if(IsOpen == false)
        {
            Anim.SetTrigger("Open");
            IsOpen = true;
            if(Cabin == true)
            {
                MyPlayer.clip = CabinSound;
                MyPlayer.Play();
            }
            if (Room == true)
            {
                MyPlayer.clip = RoomSound;
                MyPlayer.Play();
            }
            if (House == true)
            {
                MyPlayer.clip = HouseSound;
                MyPlayer.Play();
            }
        }
        /*else if (IsOpen == true)
        {
            Anim.SetTrigger("Close");
            IsOpen = false;
            if (Cabin == true)
            {
                MyPlayer.clip = CabinSound;
                MyPlayer.Play();
            }
            if (Room == true)
            {
                MyPlayer.clip = RoomSound;
                MyPlayer.Play();
            }
            if (House == true)
            {
                MyPlayer.clip = HouseSound;
                MyPlayer.Play();
            }
        }*/
    }

    public void DoorClose()
    {
        if (IsOpen == true)
        {
            Anim.SetTrigger("Close");
            IsOpen = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Enemy"))
        {
            if(Locked == false)
            {
                if(IsOpen == false)
                {
                    Anim.SetTrigger("Open");
                    IsOpen = true;

                }
            }
        }
    }
}
