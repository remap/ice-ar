using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NdnBleMenuSlideScript : MonoBehaviour {

    //refrence for the pause menu panel in the hierarchy
    public GameObject pauseMenuPanel;
    //animator reference
    private Animator anim;

    // Use this for initialization
    void Start()
    {
        //get the animator component
        anim = pauseMenuPanel.GetComponent<Animator>();
        //disable it on start to stop it from playing the default animation
        anim.enabled = false;
    }

    // Update is called once per frame
    public void Update()
    {

    }

    //function to pause the game
    public void OpenNdnBleMenu()
    {
        //enable the animator component
        anim.enabled = true;
        //play the Slidein animation
        anim.Play("BleMenuSlideDown");

    }
    //function to unpause the game
    public void CloseNdnBleMenu()
    {
        //play the SlideOut animation
        anim.Play("BleMenuSlideUp");
    }
}
