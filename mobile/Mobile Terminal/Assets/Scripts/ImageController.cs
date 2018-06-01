using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.Windows;
using System.IO; //https://forum.unity.com/threads/unityengine-windows.222416/

public struct FetchedUIFrame
{
    public byte[] argbData_;
    public Int64 timestamp_;
    public float simLevel_;

    public FetchedUIFrame(byte[] argbData, Int64 timestamp, float simLevel)
    {
        argbData_ = argbData;
        timestamp_ = timestamp;
        simLevel_ = simLevel;
    }
}

public class ImageController : MonoBehaviour {

    public Text debugPanelText;
    public RawImage r0;
    public RawImage r1;
    public RawImage r2;
    public RawImage r3;
    public int refreshRate = 1;

    private Text[] memoryText;
    private Image[] memorySimLevel;

    private List<Texture2D> textures;
    private List<string> frameTimestamps;
    private List<float> frameSimLevels; //Similarity level of memory frames to captured key frame
    //To-Do: Test if including/excluding frameSimLevels significantly alters performance--it feels like the app has been running slower
    //       since I added similarity levels to the UI


    private Queue<FetchedUIFrame> fetchedFrames;
    private int nFramesToRetain_;
    private const float frameHeight = 324;

    private bool allowNewMemories;

    public void enqueueFrame(FetchedUIFrame frameData) {
        Debug.Log("[img-controller] enqueued frame");
        fetchedFrames.Enqueue(frameData);
    }

    private Image findSimilarityUIComponent(Image[] imagesInChild)
    {
        foreach(Image img in imagesInChild)
        {
            if (img.tag == "% Similar")
                return img;
        }

        Debug.Log("[img-controller] null SimilarityLevel UI component");
        return null;
    }

    public void updateDebugText(AnnotationData data)
    {
        Debug.Log("[img-controller] inside updateDebugText()");
        string debugText = "";
        for (int i = 0; i<data.annotationData.Length; i++)
        {
            if (data.annotationData[i].prob >= 0.5f)
            {
                Debug.Log("[img-controller] inside updateDebugText() and inside if");
                debugText += data.annotationData[i].label + ": " + data.annotationData[i].prob + "\n";
            }
        }
        debugPanelText.text = debugText;
    }

    //Code from: https://stackoverflow.com/questions/11/calculate-relative-time-in-c-sharp
    public string timeAgo(long yourDateMilliseconds)
    {
        const int SECOND = 1;
        const int MINUTE = 60 * SECOND;
        const int HOUR = 60 * MINUTE;
        const int DAY = 24 * HOUR;
        const int MONTH = 30 * DAY;

        DateTime history = new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds(yourDateMilliseconds);
        history.ToLocalTime();
        //var ts = new TimeSpan(DateTime.Now.ToLocalTime().Ticks - TimeSpan.FromMilliseconds(yourDateMilliseconds).Ticks);
        var ts = new TimeSpan(DateTime.Now.ToLocalTime().Ticks - history.Ticks);

        double delta = Math.Abs(ts.TotalSeconds);

        if (delta < 1 * MINUTE)
            return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

        if (delta < 2 * MINUTE)
            return "a minute ago";

        if (delta < 45 * MINUTE)
            return ts.Minutes + " minutes ago";

        if (delta < 90 * MINUTE)
            return "an hour ago";

        if (delta < 24 * HOUR)
            return ts.Hours + " hours ago";

        if (delta < 48 * HOUR)
            return "yesterday";

        if (delta < 30 * DAY)
            return ts.Days + " days ago";

        if (delta < 12 * MONTH)
        {
            int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
            return months <= 1 ? "one month ago" : months + " months ago";
        }
        else
        {
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }
    }


    // Use this for initialization
    void Start () {
        nFramesToRetain_ = 4;
        fetchedFrames = new Queue<FetchedUIFrame>();
        allowNewMemories = true;

        textures = new List<Texture2D>();
        frameTimestamps = new List<string>();
        frameSimLevels = new List<float>();

        memoryText = new Text[4];
        memoryText[0] = r0.gameObject.GetComponentsInChildren<Text>()[0];
        memoryText[1] = r1.gameObject.GetComponentsInChildren<Text>()[0];
        memoryText[2] = r2.gameObject.GetComponentsInChildren<Text>()[0];
        memoryText[3] = r3.gameObject.GetComponentsInChildren<Text>()[0];

        memorySimLevel = new Image[4];
        memorySimLevel[0] = findSimilarityUIComponent(r0.gameObject.GetComponentsInChildren<Image>());
        memorySimLevel[1] = findSimilarityUIComponent(r1.gameObject.GetComponentsInChildren<Image>());
        memorySimLevel[2] = findSimilarityUIComponent(r2.gameObject.GetComponentsInChildren<Image>());
        memorySimLevel[3] = findSimilarityUIComponent(r3.gameObject.GetComponentsInChildren<Image>());


    }

    // Update is called once per frame
    void Update () {
        // if have new frames - dequeue them into our textures array
        while (fetchedFrames.Count > 0)
        {
            Debug.Log("[img-controller] dequeuing frames "+fetchedFrames.Count);

            Texture2D tex = new Texture2D(320, 180, TextureFormat.ARGB32, false);
            //To-Do: Figure out why frame RGB data is reversed (across the vertical axis). Maybe texture format is not ARGB32?
            //       Could try using Array.reverse(), but that's probably too slow 
            //       See: https://gamedev.stackexchange.com/questions/108444/unity-texture2d-raw-data-textureformat-problem
            FetchedUIFrame tempUIFrame = fetchedFrames.Dequeue();
            tex.LoadRawTextureData(tempUIFrame.argbData_);
            tex.Apply();
            textures.Insert(0, tex);
            //frameTimestamps.Insert(0, (new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds(tempUIFrame.timestamp_)).ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss"));
            frameTimestamps.Insert(0, timeAgo(tempUIFrame.timestamp_));
            frameSimLevels.Insert(0, tempUIFrame.simLevel_);
            //To-Do: Format above to PST (or w/e your regional time is). Right now I think it's about 7 or 8 hours ahead of our time

            allowNewMemories = true;
        }

        while (textures.Count > nFramesToRetain_)
            textures.RemoveAt(textures.Count - 1);
        while (frameTimestamps.Count > nFramesToRetain_)
            frameTimestamps.RemoveAt(frameTimestamps.Count - 1);
        while (frameSimLevels.Count > nFramesToRetain_)
            frameSimLevels.RemoveAt(frameSimLevels.Count - 1);

        if (allowNewMemories)
            UpdateMemories();
	}


    void UpdateMemories()
    {
        Debug.Log("[img-controller] updating memories");

        if (textures.Count > 0) //hopefully fixed
        {
            r0.texture = textures[0]; //Random.Range(0, textures.Count)];
            memoryText[0].text = frameTimestamps[0];
            
            //See: https://forum.unity.com/threads/setting-top-and-bottom-on-a-recttransform.265415/
            memorySimLevel[0].rectTransform.offsetMin = new Vector2(memorySimLevel[0].rectTransform.offsetMin.x, 0);
            memorySimLevel[0].rectTransform.offsetMax = new Vector2(memorySimLevel[0].rectTransform.offsetMax.x, -1 * frameHeight * frameSimLevels[0]);
        }
        if (textures.Count > 1) //fluctate from top
        { 
            r1.texture = textures[1]; //Random.Range(0, textures.Count)];
            memoryText[1].text = frameTimestamps[1];
            memorySimLevel[1].rectTransform.offsetMin = new Vector2(memorySimLevel[1].rectTransform.offsetMin.x, 0);
            memorySimLevel[1].rectTransform.offsetMax = new Vector2(memorySimLevel[1].rectTransform.offsetMax.x, -1 * frameHeight * frameSimLevels[1]);
        }
        if (textures.Count > 2) // ??
        {
            r2.texture = textures[2]; //Random.Range(0, textures.Count)];
            memoryText[2].text = frameTimestamps[2];
            memorySimLevel[2].rectTransform.offsetMin = new Vector2(memorySimLevel[2].rectTransform.offsetMin.x, 0);
            memorySimLevel[2].rectTransform.offsetMax = new Vector2(memorySimLevel[2].rectTransform.offsetMax.x, -1 * frameHeight * frameSimLevels[2]);
        }
        if (textures.Count > 3) //max all time
        {
            r3.texture = textures[3]; //Random.Range(0, textures.Count)];
            memoryText[3].text = frameTimestamps[3];
            memorySimLevel[3].rectTransform.offsetMin = new Vector2(memorySimLevel[3].rectTransform.offsetMin.x, 0);
            memorySimLevel[3].rectTransform.offsetMax = new Vector2(memorySimLevel[3].rectTransform.offsetMax.x, -1 * frameHeight * frameSimLevels[3]);
        }
          
        allowNewMemories = false;
    }
}
