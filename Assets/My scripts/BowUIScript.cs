using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BowUIScript : MonoBehaviour
{
    [SerializeField] Text BowAmt;

    // Start is called before the first frame update
    void Start()
    {
        BowAmt.text = SaveScript.Arrows + "";
    }

    // Update is called once per frame
    void Update()
    {
        BowAmt.text = SaveScript.Arrows + "";

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (SaveScript.InventoryOpen == false && SaveScript.OptionsOpen == false)
            {
                if (SaveScript.Arrows > 0)
                {
                    SaveScript.Arrows -= 1;
                }
            }
        }
    }
}
