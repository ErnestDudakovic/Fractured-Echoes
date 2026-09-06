using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class LightSettingsPlayer : MonoBehaviour
{
    [SerializeField] PostProcessVolume MyVolume;
    [SerializeField] PostProcessProfile Standard;
    [SerializeField] PostProcessProfile NightVision;
    [SerializeField] GameObject NightVisionOverlay;
    [SerializeField] GameObject Flashlight;
    [SerializeField] GameObject EnemyFlashlight;
    public Light FlLight;

    private bool NightVisionActive = false;
    private bool FlashlightActive = false;

    void Start()
    {
        // FlLight is often left unassigned in the Inspector, which used to throw
        // an UnassignedReferenceException every frame. Resolve it from the
        // Flashlight object instead (GetComponent works even while inactive).
        if (FlLight == null && Flashlight != null)
        {
            FlLight = Flashlight.GetComponent<Light>();

            if (FlLight == null)
                FlLight = Flashlight.GetComponentInChildren<Light>(true);
        }

        if (FlLight == null)
            Debug.LogWarning("LightSettingsPlayer: no Light found on '" + (Flashlight != null ? Flashlight.name : "null") + "'. Assign FlLight in the Inspector.", this);

        NightVisionOverlay.gameObject.SetActive(false);
        Flashlight.gameObject.SetActive(false);
        EnemyFlashlight.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (SaveScript.BatteryPower > 0.0f)
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (NightVisionActive == false)
                {
                    MyVolume.profile = NightVision;
                    NightVisionActive = true;
                    NightVisionOverlay.gameObject.SetActive(true);
                    SaveScript.NVLightOn = true;
                }
                else
                {
                    MyVolume.profile = Standard;
                    NightVisionActive = false;
                    NightVisionOverlay.gameObject.SetActive(false);
                    SaveScript.NVLightOn = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                if (FlashlightActive == false)
                {
                    FlashlightActive = true;
                    Flashlight.gameObject.SetActive(true);
                    EnemyFlashlight.gameObject.SetActive(true);
                    SaveScript.FlashLightOn = true;
                }
                else
                {
                    FlashlightActive = false;
                    Flashlight.gameObject.SetActive(false);
                    EnemyFlashlight.gameObject.SetActive(false);
                    SaveScript.FlashLightOn = false;
                }
            }

            if (FlLight != null)
            {
                if (SaveScript.BatteryPower < 0.55f)
                {
                    FlLight.range = SaveScript.BatteryPower * 100;
                }
                else
                {
                    FlLight.range = 55f;
                }
            }
        }

        if(SaveScript.BatteryPower <= 0.0f)
        {
            MyVolume.profile = Standard;
            NightVisionActive = false;
            NightVisionOverlay.gameObject.SetActive(false);
            SaveScript.NVLightOn = false;
            FlashlightActive = false;
            Flashlight.gameObject.SetActive(false);
            EnemyFlashlight.gameObject.SetActive(false);
            SaveScript.FlashLightOn = false;
        }
    }
    
}
