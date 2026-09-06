using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveScript : MonoBehaviour
{

    public static int PlayerHealth = 100;
    public static bool HealthChanged = false;
    public static float Sanity = 100.0f;
    public static float BatteryPower = 1.0f;
    public static bool BatteryRefill = false;
    public static bool FlashLightOn = false;
    public static bool NVLightOn = false;
    public static int Apples = 0;
    public static int Batteries = 0;
    public static bool Knife = false;
    public static bool Bat = false;
    public static bool Axe = false;
    public static bool Gun = false;
    public static bool Crossbow = false;
    public static bool CabinKey = false;
    public static bool HouseKey = false;
    public static bool RoomKey = false;
    public static int BulletClips = 0;
    public static bool ArrowRefill = false;
    public static bool HaveKnife = false;
    public static bool HaveBat = false;
    public static bool HaveAxe = false;
    public static bool HaveGun = false;
    public static bool HaveCrossbow = false;
    public static int Bullets = 12;
    public static int Arrows = 6;
    public static int NotesFound = 0;
    public static bool NewGame = false;
    public static bool SavedGame = false;
    public static Transform Target1;
    public static Transform Target2;
    public static Transform Target3;
    public static Transform Target4;
    public static Transform Target5;
    public static Transform Target6;
    public static Transform Target7;
    public static Transform Target8;
    public static Transform Target9;
    public static Transform Target10;
    public static Transform PlayerChar;
    public static GameObject Chase;
    public static GameObject HurtScreen;
    public static AudioSource StabSound;
    public static GameObject SplatKnife;
    public static GameObject SplatBat;
    public static GameObject SplatAxe;
    public static Animator Hurt;
    public static AudioSource AudioP;
    public static GameObject Arms;
    public static int MaxEnemiesOnScreen = 3;
    public static int EnemiesOnScreen = 0;
    public static int MaxEnemiesInGame = 12;
    public static int EnemiesCurrent = 0;
    public static int ApplesLeft = 10;
    public static int AmmoLeft = 4;
    public static int BatteriesLeft = 6;
    public static int ArrowsLeft = 4;
    public static int Enemy1 = 1;
    public static int Enemy2 = 1;
    public static int Enemy3 = 1;
    public static bool InventoryOpen = false;
    public static bool OptionsOpen = false;

    [SerializeField] Transform _Target1;
    [SerializeField] Transform _Target2;
    [SerializeField] Transform _Target3;
    [SerializeField] Transform _Target4;
    [SerializeField] Transform _Target5;
    [SerializeField] Transform _Target6;
    [SerializeField] Transform _Target7;
    [SerializeField] Transform _Target8;
    [SerializeField] Transform _Target9;
    [SerializeField] Transform _Target10;
    [SerializeField] Transform PlayerPrefab;
    [SerializeField] GameObject ChaseMusic;
    [SerializeField] GameObject HurtUI;
    [SerializeField] AudioSource StabPlayer;
    [SerializeField] GameObject BloodSplatKnife;
    [SerializeField] GameObject BloodSplatBat;
    [SerializeField] GameObject BloodSplatAxe;
    [SerializeField] Animator HurtAnim;
    [SerializeField] AudioSource MyPlayer;
    [SerializeField] GameObject FPSArms;


    private void Start()
    {
        Target1 = _Target1;
        Target2 = _Target2;
        Target3 = _Target3;
        Target4 = _Target4;
        Target5 = _Target5;
        Target6 = _Target6;
        Target7 = _Target7;
        Target8 = _Target8;
        Target9 = _Target9;
        Target10 = _Target10;
        PlayerChar = PlayerPrefab;
        Chase = ChaseMusic;
        HurtScreen = HurtUI;
        StabSound = StabPlayer;
        SplatKnife = BloodSplatKnife;
        SplatBat = BloodSplatBat;
        SplatAxe = BloodSplatAxe;
        Hurt = HurtAnim;
        AudioP = MyPlayer;
        Arms = FPSArms;

        if (NewGame == true)
        {
            PlayerHealth = 100;
            HealthChanged = true;
            Sanity = 100.0f;
            BatteryPower = 1.0f;
            BatteryRefill = false;
            FlashLightOn = false;
            NVLightOn = false;
            Apples = 0;
            Batteries = 0;
            Knife = false;
            Bat = false;
            Axe = false;
            Gun = false;
            Crossbow = false;
            CabinKey = false;
            HouseKey = false;
            RoomKey = false;
            BulletClips = 0;
            ArrowRefill = false;
            HaveKnife = false;
            HaveBat = false;
            HaveAxe = false;
            HaveGun = false;
            HaveCrossbow = false;
            Bullets = 12;
            Arrows = 6;
            NotesFound = 0;
            NewGame = false;
            ApplesLeft = 10;
            AmmoLeft = 4;
            BatteriesLeft = 6;
            ArrowsLeft = 4;
            Enemy1 = 1;
            Enemy2 = 1;
            Enemy3 = 1;
            InventoryOpen = false;
            OptionsOpen = false;
}

        if(SavedGame == true)
        {
            PlayerHealth = PlayerPrefs.GetInt("PlayersHealth");
            HealthChanged = true;
            Sanity = PlayerPrefs.GetFloat("SanityAmt", 100.0f);
            BatteryPower = PlayerPrefs.GetFloat("BatteriesPower");
            Apples = PlayerPrefs.GetInt("ApplesAmt");
            Batteries = PlayerPrefs.GetInt("BatteriesAmt");
            BulletClips = PlayerPrefs.GetInt("BulletsClips");
            Bullets = PlayerPrefs.GetInt("BulletsAmt");
            Arrows = PlayerPrefs.GetInt("ArrowsAmt");
            NotesFound = PlayerPrefs.GetInt("NotesFoundAmt", 0);
            MaxEnemiesOnScreen = PlayerPrefs.GetInt("MaxEScreen");
            MaxEnemiesInGame = PlayerPrefs.GetInt("MaxEGame");
            ApplesLeft = PlayerPrefs.GetInt("ApplesL");
            AmmoLeft = PlayerPrefs.GetInt("AmmoL");
            BatteriesLeft = PlayerPrefs.GetInt("BatteriesL");
            ArrowsLeft = PlayerPrefs.GetInt("ArrowsL");
            Enemy1 = PlayerPrefs.GetInt("Enemy1Alive");
            Enemy2 = PlayerPrefs.GetInt("Enemy2Alive");
            Enemy3 = PlayerPrefs.GetInt("Enemy3Alive");

            if (PlayerPrefs.GetInt("KnifeInv") == 1)
            {
                Knife = true;
            }
            if (PlayerPrefs.GetInt("BatInv") == 1)
            {
                Bat = true;
            }
            if (PlayerPrefs.GetInt("AxeInv") == 1)
            {
                Axe = true;
            }
            if (PlayerPrefs.GetInt("GunInv") == 1)
            {
                Gun = true;
            }
            if (PlayerPrefs.GetInt("CrossbowInv") == 1)
            {
                Crossbow = true;
            }
            if (PlayerPrefs.GetInt("CabinK") == 1)
            {
                CabinKey = true;
            }
            if (PlayerPrefs.GetInt("HouseK") == 1)
            {
                HouseKey = true;
            }
            if (PlayerPrefs.GetInt("RoomK") == 1)
            {
                RoomKey = true;
            }
            if (PlayerPrefs.GetInt("ArrowR") == 1)
            {
                ArrowRefill = true;
            }
            SavedGame = false;
            InventoryOpen = false;
            OptionsOpen = false;

        }
    }
}
