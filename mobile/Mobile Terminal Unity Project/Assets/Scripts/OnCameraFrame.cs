using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tango;

public class OnCameraFrame : MonoBehaviour, ITangoVideoOverlay {

	TangoApplication tango;
	public UnityEngine.UI.Text textbox;
	//public FrameObjectData prefab;
	public int frameNumber;
	//public FrameObjectData spawn;
	public double timestamp;
	public Tango.TangoUnityImageData imgBuffer;
	public Vector3 cameraPos;
	public Quaternion cameraRot;
	public float uOffset;
	public float vOffset;
	//public Dictionary<long, FrameObjectData> frameObjects;
	public FramePoolManager frameMgr;
	public BoundingBoxPoolManager boxMgr;

	void Awake () {
		QualitySettings.vSyncCount = 0;  // VSync must be disabled
		Application.targetFrameRate = 30;
	}

	void Start()
	{
		tango = FindObjectOfType <TangoApplication> ();
		tango.Register (this);
		frameMgr = GameObject.FindObjectOfType<FramePoolManager>();
		frameNumber = 0;
		//frameObjects = new Dictionary<long, FrameObjectData> ();
		boxMgr = GameObject.FindObjectOfType<BoundingBoxPoolManager>();
		timestamp = 0;
		imgBuffer = null;
		cameraPos = Vector3.zero;
		cameraRot = Quaternion.identity;
		uOffset = 0.0f;
		vOffset = 0.0f;

		NdnRtc.Initialize ();
	}

	public void OnDestroy()
	{
		tango.Unregister(this);
	}

	public void OnTangoImageAvailableEventHandler(Tango.TangoEnums.TangoCameraId cameraId, Tango.TangoUnityImageData imageBuffer)
	{

		timestamp = gameObject.GetComponent<TangoARScreen> ().m_screenUpdateTime;
		imgBuffer = imageBuffer;
		cameraPos = gameObject.GetComponent<TangoPoseController> ().finalPosition;
		cameraRot = gameObject.GetComponent<TangoPoseController> ().finalRotation;
		uOffset = gameObject.GetComponent<TangoARScreen> ().m_uOffset;
		vOffset = gameObject.GetComponent<TangoARScreen> ().m_vOffset;
		
		//for testing
		//frameNumber++;

		//add FrameObjects to dictionary
		//frameObjects.Add (frameNumber, spawn);

		//access and remove FrameObject
		/*FrameObjectData value = null;

		if (frameObjects.TryGetValue(frameNumber-1, out value))
		{
			textbox.text = "" + frameObjects.Count;
			frameObjects.Remove (frameNumber - 1);
			value.Release();
		}*/

		//create a FrameObject and save associated data
		//saveData (imageBuffer, frameNumber);

		//create a box every frame with random size and text
//		var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
//		var stringChars = new char[Random.Range (1, 10)];
//		var random = new Random ();
//
//		for (int i = 0; i < stringChars.Length; i++) {
//			stringChars [i] = chars [Random.Range (0, chars.Length)];
//		}
//
//		string finalString = new string (stringChars);
//		//boxMgr.CreateBoundingBoxObject(new Vector3 (0, 0, .5f), Random.value, Random.value, Random.value);
//		boxMgr.CreateBoundingBoxObject (new Vector3 (Random.Range (-2f, 2f), 0, Random.Range (0f, 2f)), Random.value, Random.value, 0, finalString, Color.green);
//

		int publishedFrameNo = NdnRtc.videoStream.processIncomingFrame (imageBuffer);

		if (publishedFrameNo >= 0) {
			// frame was published succesfully, do something here
			frameMgr.CreateFrameObject (imgBuffer, publishedFrameNo, timestamp, cameraPos, cameraRot, uOffset, vOffset);
		} else {
			// frame was dropped by the encoder and was not published
		}
		/*
		//needed to use this to test the frame pool. no code after this line was executed:
		//int publishedFrameNo = NdnRtc.videoStream.processIncomingFrame (imageBuffer);

		if (frameNumber >= 0) {
			// frame was published succesfully, do something here
			//textbox.text = "here";
			//frameMgr.CreateFrameObject(imgBuffer, frameNumber, timestamp, cameraPos, cameraRot, uOffset, vOffset);
		} else {
			// frame was dropped by the encoder and was not published
		}*/


	}
}
