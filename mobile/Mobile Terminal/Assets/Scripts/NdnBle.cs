using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.Text;

using net.named_data.jndn;
using net.named_data.jndn.encoding;
using net.named_data.jndn.transport;
using net.named_data.jndn.util;

using net.named_data.jndn.security;
using net.named_data.jndn.security.identity;
using net.named_data.jndn.security.policy;
using ILOG.J2CsMapping.NIO;

using System.Threading;

public class NdnBle : MonoBehaviour
{

    // hard coded hmacKey used for data verification; this key is also in the arduino and BLEApp3
    static int[] hmacKey = new int[4] {
            1, 2, 3, 4
    };

    const int MAC_ADDRESS_LENGTH_NO_COLONS = 12;

    Face face;
    Thread expressStatusInterestsThread;
    onStatusDataClass onStatusData;
    onStatusTimeoutClass onStatusTimeout;

    static String lastDataContent;

    // this is a reference to the Text box in the UI that displays log information related to ndn ble
    public Text logTextBox;

    // this is the script used to make notifications for when beacons come in and out of range pop up on the UI
    NdnBleDisplayTextScript showNotificationScript;

    // this boolean is used to detect when we are first entering the update loop, so that it can send
    // the first status interest to start the cycle of sending status interests
    bool firstUpdateLoop = true;

    class onStatusDataClass : OnData
    {
        public onStatusDataClass(Text textReference, NdnBleDisplayTextScript notifyScript, Face face)
        {
            logTextBox = textReference;
            mNotifyScript = notifyScript;
            mFace = face;
        }

        public void
        onData(Interest interest, Data data)
        {
            //logTextBox.text += "\n" + "Got a status data packet with name " + data.getName().toUri();

            if (KeyChain.verifyDataWithHmacWithSha256(data, new Blob(hmacKey)))
            {
                //logTextBox.text += "\n" + "Successfully verified data";
            }
            else
            {
                logTextBox.text += "\n" + "Failed to verify incoming data packet, ignoring it...";
                logTextBox.text += "\n" + "Data name: " + data.getName().ToString();
            }

            var content = data.getContent().buf();
            var contentString = "";
            for (int i = content.position(); i < content.limit(); ++i)
                contentString += (char)content.get(i);
            logTextBox.text += "\n" + "Data name: " + data.getName().ToString();
            logTextBox.text += "\n" + "Data content:";
            logTextBox.text += "\n" + "-------------";
            logTextBox.text += "\n" + contentString;
            logTextBox.text += "\n" + "---------------------------------";
            logTextBox.text += "\n";

            mNotifyScript.updateBeaconListText(contentString);

            if (!contentString.Equals(lastDataContent))
                mNotifyScript.notifyUserOfBeaconListChange();

            Interest statusInterest = new Interest(interest.getName());
            statusInterest.setInterestLifetimeMilliseconds(2000);

            var onStatusData = new onStatusDataClass(logTextBox, mNotifyScript, mFace);
            var onStatusTimeout = new onStatusTimeoutClass(logTextBox, mFace, mNotifyScript);

            mFace.expressInterest(statusInterest, onStatusData, onStatusTimeout);

            lastDataContent = contentString;
        }

        NdnBleDisplayTextScript mNotifyScript;
        Text logTextBox;
        Face mFace;

    }

    class onStatusTimeoutClass : OnTimeout
    {
        public onStatusTimeoutClass(Text textReference, Face face, NdnBleDisplayTextScript notifyScript)
        {
            logTextBox = textReference;
            mFace = face;
            mNotifyScript = notifyScript;
        }

        Text logTextBox;
        GameObject buttonCreatorScriptObject;
        Face mFace;
        NdnBleDisplayTextScript mNotifyScript;

        public void onTimeout(Interest interest)
        {
            //logTextBox.text += "\n" + "Time out for interest " + interest.getName().toUri();
            //logTextBox.text += "\n";

            Interest statusInterest = new Interest(interest.getName());
            statusInterest.setInterestLifetimeMilliseconds(2000);

            var onStatusData = new onStatusDataClass(logTextBox, mNotifyScript, mFace);
            var onStatusTimeout = new onStatusTimeoutClass(logTextBox, mFace, mNotifyScript);

            mFace.expressInterest(statusInterest, onStatusData, onStatusTimeout);
        }
    }

    public static String removeColonsFromMACAddress(String address)
    {
        StringBuilder sb = new StringBuilder(MAC_ADDRESS_LENGTH_NO_COLONS);

        sb.append(address.Substring(0, 2));
        sb.append(address.Substring(3, 2));
        sb.append(address.Substring(6, 2));
        sb.append(address.Substring(9, 2));
        sb.append(address.Substring(12, 2));
        sb.append(address.Substring(15, 2));

        String noColons = sb.toString();

        return noColons;
    }

    // Use this for initialization
    void Start()
    {
        // this is here to force the orientation of the app; the UI looks funny if it goes into vertical mode
        Screen.orientation = ScreenOrientation.LandscapeRight;

        showNotificationScript = GetComponent<NdnBleDisplayTextScript>();

        onStatusData = new onStatusDataClass(logTextBox, showNotificationScript, face);
        onStatusTimeout = new onStatusTimeoutClass(logTextBox, face, showNotificationScript);

        try
        {
            face = new Face
            (new TcpTransport(), new TcpTransport.ConnectionInfo("127.0.0.1"));

        }
        catch (Exception e)
        {
            logTextBox.text += "\n" + "exception: " + e.Message;
        }

    }

    // Update is called once per frame
    void Update()
    {

        face.processEvents();

        if (firstUpdateLoop)
        {

            Interest statusInterest = new Interest(new Name("icear/beacon/status"));
            statusInterest.setInterestLifetimeMilliseconds(2000);

            var onStatusData = new onStatusDataClass(logTextBox, showNotificationScript, face);
            var onStatusTimeout = new onStatusTimeoutClass(logTextBox, face, showNotificationScript);

            face.expressInterest(statusInterest, onStatusData, onStatusTimeout);

            firstUpdateLoop = false;
        }

        // We need to sleep for a few milliseconds so we don't use 100% of the CPU.

        System.Threading.Thread.Sleep(5);
    }
}
