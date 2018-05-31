using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.Windows;
using System.IO; //https://forum.unity.com/threads/unityengine-windows.222416/

public class ImageController : MonoBehaviour {

    public RawImage r1;
    public RawImage r2;
    public RawImage r3;
    public RawImage r4;
    public int refreshRate = 1;

    private List<Texture2D> textures;
    
    private Queue<byte[]> fetchedFrames;
    private int nFramesToRetain_;

    private bool allowNewMemories;

    public void enqueueFrame(byte [] frameArgbData) {
        Debug.Log("[img-controller] enqueued frame");
        fetchedFrames.Enqueue(frameArgbData);
    }

    // Use this for initialization
    void Start () {
        nFramesToRetain_ = 4;
        fetchedFrames = new Queue<byte[]>();

        /*
        ///******************METHOD 1*********************************************************
        //byte[] imageData = File.ReadAllBytes("Assets/Resources/Images/Color Supernova.jpg");

        ///******************METHOD 2*********************************************************
        byte[] imageData;

        //If loading a jpg via Resources.load, we must change the image to a .bytes extension instead
        // //https://docs.unity3d.com/Manual/class-TextAsset.html
        TextAsset img = Resources.Load("ImagesBytes/ColorSupernova") as TextAsset; 
        imageData = img.bytes;

        Debug.Log(imageData.Length);

        ///************* COMMON FOR METHOD 1 AND METHOD 2 *****************************************
        Texture2D tex = new Texture2D(1, 1);
        tex.LoadImage(imageData);
        r1.texture = tex;
        //*** https://forum.unity.com/threads/unityengine-texture2d-loadimage-is-missing.467202/  --> 
        //          https://docs.unity3d.com/2017.1/Documentation/ScriptReference/ImageConversion.LoadImage.html
        */


        List<byte[]> imageData = new List<byte[]>();
        allowNewMemories = true;

        TextAsset img = Resources.Load("ImagesBytes/ColorSupernova") as TextAsset;
        imageData.Add(img.bytes);
        img = Resources.Load("ImagesBytes/Bokeh") as TextAsset;
        imageData.Add(img.bytes);
        img = Resources.Load("ImagesBytes/Color1") as TextAsset;
        imageData.Add(img.bytes);
        img = Resources.Load("ImagesBytes/Color2") as TextAsset;
        imageData.Add(img.bytes);
        img = Resources.Load("ImagesBytes/Color3") as TextAsset;
        imageData.Add(img.bytes);
        img = Resources.Load("ImagesBytes/People1") as TextAsset;
        imageData.Add(img.bytes);
        img = Resources.Load("ImagesBytes/Setting1") as TextAsset;
        imageData.Add(img.bytes);
        img = Resources.Load("ImagesBytes/TheBed") as TextAsset;
        imageData.Add(img.bytes);


        textures = new List<Texture2D>();

        // for (int i = 0; i < imageData.Count; i++)
        // {
        //     Texture2D tempTex = new Texture2D(1, 1);
        //     tempTex.LoadImage(imageData[i]);
        //     textures.Add(tempTex);
        // }


        /*
        Resources.Load("Images/Color Supernova") as Texture; //https://answers.unity.com/questions/318921/correct-folder-for-resourcesload-.html

        Texture2D tex = new Texture2D(width, height);
        tex.LoadImage(byteArray);
        renderer.material.mainTexture = tex;
        */


        /*
        Texture2D tempTex = new Texture2D(400, 400);

        var colorArray = new Color32[imageData.Length / 4];
        for (var i = 0; i < 10000; i += 4)  //imageData.Length
        {
            var color = new Color32(imageData[i + 0], imageData[i + 1], imageData[i + 2], imageData[i + 3]);
            colorArray[i / 4] = color;
        }

        tempTex.SetPixels32(colorArray);
        r1.texture = tempTex; //https://forum.unity.com/threads/how-to-set-new-texture-to-rawimage.266283/
        */

        
    }

    // Update is called once per frame
    void Update () {
        // if have new frames - dequeue them into our textures array
        while (fetchedFrames.Count > 0)
        {
            Debug.Log("[img-controller] dequeuing frames "+fetchedFrames.Count);

            Texture2D tex = new Texture2D(320, 180, TextureFormat.ARGB32, false);
            tex.LoadRawTextureData(fetchedFrames.Dequeue());
            tex.Apply();
            textures.Insert(0, tex);

            allowNewMemories = true;
        }

        while (textures.Count > nFramesToRetain_)
            textures.RemoveAt(textures.Count-1);

        if (allowNewMemories)
            UpdateMemories();
	}

    //https://answers.unity.com/questions/132154/how-to-limit-the-players-rate-of-fire.html
    //https://docs.unity3d.com/ScriptReference/WaitForSeconds.html
    // IEnumerator UpdateMemories()
    void UpdateMemories()
    {
        Debug.Log("[img-controller] updating memories");

        if (textures.Count > 0)
            r1.texture = textures[0]; //Random.Range(0, textures.Count)];
        if (textures.Count > 1)
            r2.texture = textures[1]; //Random.Range(0, textures.Count)];
        if (textures.Count > 2)
            r3.texture = textures[2]; //Random.Range(0, textures.Count)];
        if (textures.Count > 3)
            r4.texture = textures[3]; //Random.Range(0, textures.Count)];

        allowNewMemories = false;
    }
}
