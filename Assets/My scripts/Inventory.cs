using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] GameObject InventoryMenu;
    private bool InventoryActive = false;
    private AudioSource MyPlayer;
    private bool OptionsActive = false;
    [SerializeField] AudioClip AppleBite;
    [SerializeField] AudioClip BatteryChange;
    [SerializeField] AudioClip WeaponChange;
    [SerializeField] AudioClip GunShot;
    [SerializeField] AudioClip ArrowShot;
    [SerializeField] GameObject PlayerArms;
    [SerializeField] GameObject Knife;
    [SerializeField] GameObject Bat;
    [SerializeField] GameObject Axe;
    [SerializeField] GameObject Gun;
    [SerializeField] GameObject Crossbow;
    [SerializeField] GameObject GunUI;
    [SerializeField] GameObject BulletAmt;
    [SerializeField] GameObject BowUI;
    [SerializeField] GameObject BowAmt;
    [SerializeField] GameObject OptionsMenu;

    [SerializeField] Animator Anim;

    // Apples
    [SerializeField] GameObject AppleImage1;
    [SerializeField] GameObject AppleButton1;
    [SerializeField] GameObject AppleImage2;
    [SerializeField] GameObject AppleButton2;
    [SerializeField] GameObject AppleImage3;
    [SerializeField] GameObject AppleButton3;
    [SerializeField] GameObject AppleImage4;
    [SerializeField] GameObject AppleButton4;
    [SerializeField] GameObject AppleImage5;
    [SerializeField] GameObject AppleButton5;
    [SerializeField] GameObject AppleImage6;
    [SerializeField] GameObject AppleButton6;

    // Batteries
    [SerializeField] GameObject BatteryImage1;
    [SerializeField] GameObject BatteryButton1;
    [SerializeField] GameObject BatteryImage2;
    [SerializeField] GameObject BatteryButton2;
    [SerializeField] GameObject BatteryImage3;
    [SerializeField] GameObject BatteryButton3;
    [SerializeField] GameObject BatteryImage4;
    [SerializeField] GameObject BatteryButton4;

    //Weapons
    [SerializeField] GameObject KnifeImage;
    [SerializeField] GameObject KnifeButton;
    [SerializeField] GameObject BatImage;
    [SerializeField] GameObject BatButton;
    [SerializeField] GameObject AxeImage;
    [SerializeField] GameObject AxeButton;
    [SerializeField] GameObject GunImage;
    [SerializeField] GameObject GunButton;
    [SerializeField] GameObject CrossbowImage;
    [SerializeField] GameObject CrossbowButton;

    //Keys
    [SerializeField] GameObject CabinKey;
    [SerializeField] GameObject HouseKey;
    [SerializeField] GameObject RoomKey;

    //Ammo
    [SerializeField] GameObject BulletClip1Image;
    [SerializeField] GameObject BulletClip1Button;
    [SerializeField] GameObject BulletClip2Image;
    [SerializeField] GameObject BulletClip2Button;
    [SerializeField] GameObject BulletClip3Image;
    [SerializeField] GameObject BulletClip3Button;
    [SerializeField] GameObject BulletClip4Image;
    [SerializeField] GameObject BulletClip4Button;
    [SerializeField] GameObject ArrowImage;
    [SerializeField] GameObject ArrowButton;

    // Start is called before the first frame update
    void Start()
    {
        InventoryMenu.gameObject.SetActive(false);
        GunUI.gameObject.SetActive(false);
        BulletAmt.gameObject.SetActive(false);
        BowUI.gameObject.SetActive(false);
        BowAmt.gameObject.SetActive(false);
        InventoryActive = false;
        Cursor.visible = false;
        Time.timeScale = 1;
        MyPlayer = GetComponent<AudioSource>();
        OptionsMenu.gameObject.SetActive(false);

        //Apples
        AppleImage1.gameObject.SetActive(false);
        AppleButton1.gameObject.SetActive(false);
        AppleImage2.gameObject.SetActive(false);
        AppleButton2.gameObject.SetActive(false);
        AppleImage3.gameObject.SetActive(false);
        AppleButton3.gameObject.SetActive(false);
        AppleImage4.gameObject.SetActive(false);
        AppleButton4.gameObject.SetActive(false);
        AppleImage5.gameObject.SetActive(false);
        AppleButton5.gameObject.SetActive(false);
        AppleImage6.gameObject.SetActive(false);
        AppleButton6.gameObject.SetActive(false);

        //Batteries
        BatteryImage1.gameObject.SetActive(false);
        BatteryButton1.gameObject.SetActive(false);
        BatteryImage2.gameObject.SetActive(false);
        BatteryButton2.gameObject.SetActive(false);
        BatteryImage3.gameObject.SetActive(false);
        BatteryButton3.gameObject.SetActive(false);
        BatteryImage4.gameObject.SetActive(false);
        BatteryButton4.gameObject.SetActive(false);

        //Weapons
        KnifeImage.gameObject.SetActive(false);
        KnifeButton.gameObject.SetActive(false);
        BatImage.gameObject.SetActive(false);
        BatButton.gameObject.SetActive(false);
        AxeImage.gameObject.SetActive(false);
        AxeButton.gameObject.SetActive(false);
        GunImage.gameObject.SetActive(false);
        GunButton.gameObject.SetActive(false);
        CrossbowImage.gameObject.SetActive(false);
        CrossbowButton.gameObject.SetActive(false);

        //Keys
        CabinKey.gameObject.SetActive(false);
        HouseKey.gameObject.SetActive(false);
        RoomKey.gameObject.SetActive(false);

        //Ammo
        BulletClip1Image.gameObject.SetActive(false);
        BulletClip1Button.gameObject.SetActive(false);
        BulletClip2Image.gameObject.SetActive(false);
        BulletClip2Button.gameObject.SetActive(false);
        BulletClip3Image.gameObject.SetActive(false);
        BulletClip3Button.gameObject.SetActive(false);
        BulletClip4Image.gameObject.SetActive(false);
        BulletClip4Button.gameObject.SetActive(false);
        ArrowImage.gameObject.SetActive(false);
        ArrowButton.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        if(Input.GetKeyDown(KeyCode.O))
        {
            if(OptionsActive == false)
            {
                OptionsMenu.gameObject.SetActive(true);
                SaveScript.OptionsOpen = true;
                OptionsActive = true;
            }
            else if (OptionsActive == true)
            {
                OptionsMenu.gameObject.SetActive(false);
                Cursor.visible = false;
                SaveScript.OptionsOpen = false;
                Time.timeScale = 1;
                OptionsActive = false;
            }
        }
        if(Input.GetKeyDown(KeyCode.I))
        {
            if(InventoryActive == false)
            {
                InventoryMenu.gameObject.SetActive(true);
                InventoryActive = true;
                SaveScript.InventoryOpen = true;
                Time.timeScale = 0f;
                Cursor.visible = true;
            }
            else if (InventoryActive == true)
            {
                InventoryMenu.gameObject.SetActive(false);
                InventoryActive = false;
                SaveScript.InventoryOpen = false;
                Time.timeScale = 1f;
                Cursor.visible = false;
            }
        }

            CheckInventory();
            CheckWeapons();
            CheckKeys();
            CheckAmmo();
    }

    void CheckKeys ()
    {
        if(SaveScript.CabinKey == true)
        {
            CabinKey.gameObject.SetActive(true);
        }
        if (SaveScript.HouseKey == true)
        {
            HouseKey.gameObject.SetActive(true);
        }
        if (SaveScript.RoomKey == true)
        {
            RoomKey.gameObject.SetActive(true);
        }
    }

    void CheckAmmo()
    {
        if (!GameRules.WeaponsEnabled)
        {
            // No weapons means no ammo slots either.
            BulletClip1Image.gameObject.SetActive(false);
            BulletClip1Button.gameObject.SetActive(false);
            BulletClip2Image.gameObject.SetActive(false);
            BulletClip2Button.gameObject.SetActive(false);
            BulletClip3Image.gameObject.SetActive(false);
            BulletClip3Button.gameObject.SetActive(false);
            BulletClip4Image.gameObject.SetActive(false);
            BulletClip4Button.gameObject.SetActive(false);
            ArrowImage.gameObject.SetActive(false);
            ArrowButton.gameObject.SetActive(false);
            return;
        }

        if(SaveScript.BulletClips == 0)
        {
            BulletClip1Image.gameObject.SetActive(false);
            BulletClip1Button.gameObject.SetActive(false);
            BulletClip2Image.gameObject.SetActive(false);
            BulletClip2Button.gameObject.SetActive(false);
            BulletClip3Image.gameObject.SetActive(false);
            BulletClip3Button.gameObject.SetActive(false);
            BulletClip4Image.gameObject.SetActive(false);
            BulletClip4Button.gameObject.SetActive(false);
        }
        if (SaveScript.BulletClips == 1)
        {
            BulletClip1Image.gameObject.SetActive(true);
            BulletClip1Button.gameObject.SetActive(true);
            BulletClip2Image.gameObject.SetActive(false);
            BulletClip2Button.gameObject.SetActive(false);
            BulletClip3Image.gameObject.SetActive(false);
            BulletClip3Button.gameObject.SetActive(false);
            BulletClip4Image.gameObject.SetActive(false);
            BulletClip4Button.gameObject.SetActive(false);
        }
        if (SaveScript.BulletClips == 2)
        {
            BulletClip1Image.gameObject.SetActive(true);
            BulletClip1Button.gameObject.SetActive(false);
            BulletClip2Image.gameObject.SetActive(true);
            BulletClip2Button.gameObject.SetActive(true);
            BulletClip3Image.gameObject.SetActive(false);
            BulletClip3Button.gameObject.SetActive(false);
            BulletClip4Image.gameObject.SetActive(false);
            BulletClip4Button.gameObject.SetActive(false);
        }
        if (SaveScript.BulletClips == 3)
        {
            BulletClip1Image.gameObject.SetActive(true);
            BulletClip1Button.gameObject.SetActive(false);
            BulletClip2Image.gameObject.SetActive(true);
            BulletClip2Button.gameObject.SetActive(false);
            BulletClip3Image.gameObject.SetActive(true);
            BulletClip3Button.gameObject.SetActive(true);
            BulletClip4Image.gameObject.SetActive(false);
            BulletClip4Button.gameObject.SetActive(false);
        }
        if (SaveScript.BulletClips == 4)
        {
            BulletClip1Image.gameObject.SetActive(true);
            BulletClip1Button.gameObject.SetActive(false);
            BulletClip2Image.gameObject.SetActive(true);
            BulletClip2Button.gameObject.SetActive(false);
            BulletClip3Image.gameObject.SetActive(true);
            BulletClip3Button.gameObject.SetActive(false);
            BulletClip4Image.gameObject.SetActive(true);
            BulletClip4Button.gameObject.SetActive(true);
        }
        if (SaveScript.ArrowRefill == false)
        {
            ArrowImage.gameObject.SetActive(false);
            ArrowButton.gameObject.SetActive(false);
        }
        if(SaveScript.ArrowRefill == true)
        {
            ArrowImage.gameObject.SetActive(true);
            ArrowButton.gameObject.SetActive(true);
        }
    }

    void CheckInventory()
    {
        //Apples
        if (SaveScript.Apples == 0)
        {
            AppleImage1.gameObject.SetActive(false);
            AppleButton1.gameObject.SetActive(false);
            AppleImage2.gameObject.SetActive(false);
            AppleButton2.gameObject.SetActive(false);
            AppleImage3.gameObject.SetActive(false);
            AppleButton3.gameObject.SetActive(false);
            AppleImage4.gameObject.SetActive(false);
            AppleButton4.gameObject.SetActive(false);
            AppleImage5.gameObject.SetActive(false);
            AppleButton5.gameObject.SetActive(false);
            AppleImage6.gameObject.SetActive(false);
            AppleButton6.gameObject.SetActive(false);
        }
        if (SaveScript.Apples == 1)
        {
            AppleImage1.gameObject.SetActive(true);
            AppleButton1.gameObject.SetActive(true);
            AppleImage2.gameObject.SetActive(false);
            AppleButton2.gameObject.SetActive(false);
            AppleImage3.gameObject.SetActive(false);
            AppleButton3.gameObject.SetActive(false);
            AppleImage4.gameObject.SetActive(false);
            AppleButton4.gameObject.SetActive(false);
            AppleImage5.gameObject.SetActive(false);
            AppleButton5.gameObject.SetActive(false);
            AppleImage6.gameObject.SetActive(false);
            AppleButton6.gameObject.SetActive(false);
        }
        if (SaveScript.Apples == 2)
        {
            AppleImage1.gameObject.SetActive(true);
            AppleButton1.gameObject.SetActive(false);
            AppleImage2.gameObject.SetActive(true);
            AppleButton2.gameObject.SetActive(true);
            AppleImage3.gameObject.SetActive(false);
            AppleButton3.gameObject.SetActive(false);
            AppleImage4.gameObject.SetActive(false);
            AppleButton4.gameObject.SetActive(false);
            AppleImage5.gameObject.SetActive(false);
            AppleButton5.gameObject.SetActive(false);
            AppleImage6.gameObject.SetActive(false);
            AppleButton6.gameObject.SetActive(false);
        }
        if (SaveScript.Apples == 3)
        {
            AppleImage1.gameObject.SetActive(true);
            AppleButton1.gameObject.SetActive(false);
            AppleImage2.gameObject.SetActive(true);
            AppleButton2.gameObject.SetActive(false);
            AppleImage3.gameObject.SetActive(true);
            AppleButton3.gameObject.SetActive(true);
            AppleImage4.gameObject.SetActive(false);
            AppleButton4.gameObject.SetActive(false);
            AppleImage5.gameObject.SetActive(false);
            AppleButton5.gameObject.SetActive(false);
            AppleImage6.gameObject.SetActive(false);
            AppleButton6.gameObject.SetActive(false);
        }
        if (SaveScript.Apples == 4)
        {
            AppleImage1.gameObject.SetActive(true);
            AppleButton1.gameObject.SetActive(false);
            AppleImage2.gameObject.SetActive(true);
            AppleButton2.gameObject.SetActive(false);
            AppleImage3.gameObject.SetActive(true);
            AppleButton3.gameObject.SetActive(false);
            AppleImage4.gameObject.SetActive(true);
            AppleButton4.gameObject.SetActive(true);
            AppleImage5.gameObject.SetActive(false);
            AppleButton5.gameObject.SetActive(false);
            AppleImage6.gameObject.SetActive(false);
            AppleButton6.gameObject.SetActive(false);
        }
        if (SaveScript.Apples == 5)
        {
            AppleImage1.gameObject.SetActive(true);
            AppleButton1.gameObject.SetActive(false);
            AppleImage2.gameObject.SetActive(true);
            AppleButton2.gameObject.SetActive(false);
            AppleImage3.gameObject.SetActive(true);
            AppleButton3.gameObject.SetActive(false);
            AppleImage4.gameObject.SetActive(true);
            AppleButton4.gameObject.SetActive(false);
            AppleImage5.gameObject.SetActive(true);
            AppleButton5.gameObject.SetActive(true);
            AppleImage6.gameObject.SetActive(false);
            AppleButton6.gameObject.SetActive(false);
        }
        if (SaveScript.Apples == 6)
        {
            AppleImage1.gameObject.SetActive(true);
            AppleButton1.gameObject.SetActive(false);
            AppleImage2.gameObject.SetActive(true);
            AppleButton2.gameObject.SetActive(false);
            AppleImage3.gameObject.SetActive(true);
            AppleButton3.gameObject.SetActive(false);
            AppleImage4.gameObject.SetActive(true);
            AppleButton4.gameObject.SetActive(false);
            AppleImage5.gameObject.SetActive(true);
            AppleButton5.gameObject.SetActive(false);
            AppleImage6.gameObject.SetActive(true);
            AppleButton6.gameObject.SetActive(true);
        }

        //Batteries
        if (SaveScript.Batteries == 0)
        {
            BatteryImage1.gameObject.SetActive(false);
            BatteryButton1.gameObject.SetActive(false);
            BatteryImage2.gameObject.SetActive(false);
            BatteryButton2.gameObject.SetActive(false);
            BatteryImage3.gameObject.SetActive(false);
            BatteryButton3.gameObject.SetActive(false);
            BatteryImage4.gameObject.SetActive(false);
            BatteryButton4.gameObject.SetActive(false);
        }
        if (SaveScript.Batteries == 1)
        {
            BatteryImage1.gameObject.SetActive(true);
            BatteryButton1.gameObject.SetActive(true);
            BatteryImage2.gameObject.SetActive(false);
            BatteryButton2.gameObject.SetActive(false);
            BatteryImage3.gameObject.SetActive(false);
            BatteryButton3.gameObject.SetActive(false);
            BatteryImage4.gameObject.SetActive(false);
            BatteryButton4.gameObject.SetActive(false);
        }
        if (SaveScript.Batteries == 2)
        {
            BatteryImage1.gameObject.SetActive(true);
            BatteryButton1.gameObject.SetActive(false);
            BatteryImage2.gameObject.SetActive(true);
            BatteryButton2.gameObject.SetActive(true);
            BatteryImage3.gameObject.SetActive(false);
            BatteryButton3.gameObject.SetActive(false);
            BatteryImage4.gameObject.SetActive(false);
            BatteryButton4.gameObject.SetActive(false);
        }
        if (SaveScript.Batteries == 3)
        {
            BatteryImage1.gameObject.SetActive(true);
            BatteryButton1.gameObject.SetActive(false);
            BatteryImage2.gameObject.SetActive(true);
            BatteryButton2.gameObject.SetActive(false);
            BatteryImage3.gameObject.SetActive(true);
            BatteryButton3.gameObject.SetActive(true);
            BatteryImage4.gameObject.SetActive(false);
            BatteryButton4.gameObject.SetActive(false);
        }
        if (SaveScript.Batteries == 4)
        {
            BatteryImage1.gameObject.SetActive(true);
            BatteryButton1.gameObject.SetActive(false);
            BatteryImage2.gameObject.SetActive(true);
            BatteryButton2.gameObject.SetActive(false);
            BatteryImage3.gameObject.SetActive(true);
            BatteryButton3.gameObject.SetActive(false);
            BatteryImage4.gameObject.SetActive(true);
            BatteryButton4.gameObject.SetActive(true);
        }
    }

    void CheckWeapons()
    {
        if (!GameRules.WeaponsEnabled)
        {
            // No-combat horror: the weapon row never appears in the bag.
            KnifeImage.gameObject.SetActive(false);
            KnifeButton.gameObject.SetActive(false);
            BatImage.gameObject.SetActive(false);
            BatButton.gameObject.SetActive(false);
            AxeImage.gameObject.SetActive(false);
            AxeButton.gameObject.SetActive(false);
            GunImage.gameObject.SetActive(false);
            GunButton.gameObject.SetActive(false);
            CrossbowImage.gameObject.SetActive(false);
            CrossbowButton.gameObject.SetActive(false);
            return;
        }

        if(SaveScript.Knife == true)
        {
            KnifeImage.gameObject.SetActive(true);
            KnifeButton.gameObject.SetActive(true);
     
        }
        if(SaveScript.Bat == true)
        {
            
            BatImage.gameObject.SetActive(true);
            BatButton.gameObject.SetActive(true);
            
        }
        if (SaveScript.Axe == true)
        {

            AxeImage.gameObject.SetActive(true);
            AxeButton.gameObject.SetActive(true);
            
        }
        if (SaveScript.Gun == true)
        {
            
            GunImage.gameObject.SetActive(true);
            GunButton.gameObject.SetActive(true);
            
        }
        if (SaveScript.Crossbow == true)
        {
            
            CrossbowImage.gameObject.SetActive(true);
            CrossbowButton.gameObject.SetActive(true);
        }
    }

    public void HealthUpdate()
    {
        if (SaveScript.PlayerHealth < 100)
        {
            SaveScript.PlayerHealth += 30;
            SaveScript.HealthChanged = true;
            SaveScript.Apples -= 1;
            MyPlayer.clip = AppleBite;
            MyPlayer.Play();

            if(SaveScript.PlayerHealth > 100)
            {
                SaveScript.PlayerHealth = 100;
            }
        }
    }

    public void BatteryUpdate()
    {
        SaveScript.BatteryRefill = true;
        SaveScript.Batteries -= 1;
        MyPlayer.clip = BatteryChange;
        MyPlayer.Play();
    }

    public void AssignKnife()
    {
        if (!GameRules.WeaponsEnabled) return;

        PlayerArms.gameObject.SetActive(true);
        Knife.gameObject.SetActive(true);
        Anim.SetBool("Melee", true);
        MyPlayer.clip = WeaponChange;
        MyPlayer.Play();
        SaveScript.HaveKnife = true;
        SaveScript.HaveBat = false;
        SaveScript.HaveAxe = false;
        SaveScript.HaveGun = false;
        SaveScript.HaveCrossbow = false;
        GunUI.gameObject.SetActive(false);
        BulletAmt.gameObject.SetActive(false);
        BowUI.gameObject.SetActive(false);
        BowAmt.gameObject.SetActive(false);
    }

    public void AssignBat()
    {
        if (!GameRules.WeaponsEnabled) return;

        PlayerArms.gameObject.SetActive(true);
        Bat.gameObject.SetActive(true);
        Anim.SetBool("Melee", true);
        MyPlayer.clip = WeaponChange;
        MyPlayer.Play();
        SaveScript.HaveKnife = false;
        SaveScript.HaveBat = true;
        SaveScript.HaveAxe = false;
        SaveScript.HaveGun = false;
        SaveScript.HaveCrossbow = false;
        GunUI.gameObject.SetActive(false);
        BulletAmt.gameObject.SetActive(false);
        BowUI.gameObject.SetActive(false);
        BowAmt.gameObject.SetActive(false);
    }

    public void AssignAxe()
    {
        if (!GameRules.WeaponsEnabled) return;

        PlayerArms.gameObject.SetActive(true);
        Axe.gameObject.SetActive(true);
        Anim.SetBool("Melee", true);
        MyPlayer.clip = WeaponChange;
        MyPlayer.Play();
        SaveScript.HaveKnife = false;
        SaveScript.HaveBat = false;
        SaveScript.HaveAxe = true;
        SaveScript.HaveGun = false;
        SaveScript.HaveCrossbow = false;
        GunUI.gameObject.SetActive(false);
        BulletAmt.gameObject.SetActive(false);
        BowUI.gameObject.SetActive(false);
        BowAmt.gameObject.SetActive(false);
    }

    public void AssignGun()
    {
        if (!GameRules.WeaponsEnabled) return;

        PlayerArms.gameObject.SetActive(true);
        Gun.gameObject.SetActive(true);
        Anim.SetBool("Melee", false);
        MyPlayer.clip = GunShot;
        MyPlayer.Play();
        SaveScript.HaveKnife = false;
        SaveScript.HaveBat = false;
        SaveScript.HaveAxe = false;
        SaveScript.HaveGun = true;
        SaveScript.HaveCrossbow = false;
        GunUI.gameObject.SetActive(true);
        BulletAmt.gameObject.SetActive(true);
        BowUI.gameObject.SetActive(false);
        BowAmt.gameObject.SetActive(false);
    }

    public void AssignCrossbow()
    {
        if (!GameRules.WeaponsEnabled) return;

        PlayerArms.gameObject.SetActive(true);
        Crossbow.gameObject.SetActive(true);
        Anim.SetBool("Melee", false);
        MyPlayer.clip = ArrowShot;
        MyPlayer.Play();
        SaveScript.HaveKnife = false;
        SaveScript.HaveBat = false;
        SaveScript.HaveAxe = false;
        SaveScript.HaveGun = false;
        SaveScript.HaveCrossbow = true;
        BowUI.gameObject.SetActive(true);
        BowAmt.gameObject.SetActive(true);
        GunUI.gameObject.SetActive(false);
        BulletAmt.gameObject.SetActive(false);
    }

    public void WeaponsOff()
    {
        Axe.gameObject.SetActive(false);
        Bat.gameObject.SetActive(false);
        Knife.gameObject.SetActive(false);
        Gun.gameObject.SetActive(false);
        Crossbow.gameObject.SetActive(false);
    }

    public void AmmoRefill()
    {
        SaveScript.BulletClips -= 1;
        SaveScript.Bullets += 12;
        if(SaveScript.Bullets > 12)
        {
            SaveScript.Bullets = 12;
        }
        MyPlayer.clip = GunShot;
        MyPlayer.Play();
    }

    public void BowRefill()
    {
        SaveScript.ArrowRefill = false;
        SaveScript.Arrows += 6;
        if (SaveScript.Arrows > 6)
        {
            SaveScript.Arrows = 6;
        }
        MyPlayer.clip = ArrowShot;
        MyPlayer.Play();
    }
}
