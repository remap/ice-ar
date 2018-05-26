using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayNotificationScript : MonoBehaviour
{

    //refrence for the pause menu panel in the hierarchy
    public GameObject notificationPanel;
    //animator reference
    private Animator anim;

    // Use this for initialization
    void Start()
    {
        //get the animator component
        anim = notificationPanel.GetComponent<Animator>();
        //disable it on start to stop it from playing the default animation
        anim.enabled = false;
    }

    // Update is called once per frame
    public void Update()
    {

    }

    //function to pause the game
    public void displayNotification()
    {
        //enable the animator component
        anim.enabled = true;
        //play the Slidein animation
        anim.Play("DisplayNotification");

    }
}
