using UnityEngine;
using System.Collections;
using System;

using UnityEngine.UI;

class ShowTextForAFewSeconds : MonoBehaviour
{
    public Text notificationText;
    bool firstUpdateLoop = true;
    Hashtable beaconsInRange;
    
    /*
    IEnumerator showMessage (string message)
    {
        //notificationText.text = message;
        //notificationText.enabled = true;
        //yield return new WaitForSeconds(5);
        //notificationText.enabled = false;
    }
    */

    private void Start()
    {
        //notificationText.enabled = true;
    }

    void Update()
    {

    }

    public void displayNotificationText(string message)
    {
        notificationText.text = message;
    }

}
