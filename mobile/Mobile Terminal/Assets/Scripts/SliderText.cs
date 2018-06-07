using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderText : MonoBehaviour {

    public Text text;

    public void adjustText(float f)
    {
        Debug.Log("[slider-text] adjustText called!");
        text.text = f.ToString() + " Sec";
        Debug.Log("[slider-text] End of adjustText!");
    }

    // Use this for initialization
    void Start () {
        Debug.Log("[slider-text] start called!");
    }
	
	// Update is called once per frame
	void Update () {
        Debug.Log("[slider-text] update called!");
    }
}
