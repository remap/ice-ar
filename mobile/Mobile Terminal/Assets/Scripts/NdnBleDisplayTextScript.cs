using UnityEngine;
using System.Collections;
using System;

using UnityEngine.UI;

class NdnBleDisplayTextScript : MonoBehaviour
{
    public Text beaconListText;
    public Text notificationText;
    bool firstUpdateLoop = true;
    Hashtable beaconsInRange;

    IEnumerator showMessage ()
    {
        notificationText.enabled = true;
        yield return new WaitForSeconds(5);
        notificationText.enabled = false;
    }

    private void Start()
    {
        notificationText.enabled = false;
        notificationText.text = "Beacons in range list was updated.";
    }

    void Update()
    {

    }

    public void updateBeaconListText(string message)
    {
        beaconListText.text = message;
    }

    public void notifyUserOfBeaconListChange()
    {
        IEnumerator messageDisplayCoroutine = showMessage();

        StartCoroutine(messageDisplayCoroutine);
    }
}
