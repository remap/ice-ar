using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tango;

public class OnCameraFrame : MonoBehaviour, ITangoVideoOverlay {

	TangoApplication tango;
	public UnityEngine.UI.Text textbox;
	public FrameObjectData prefab;
	public long frameNumber;
	public FrameObjectData spawn;
	public double timestamp;
	public Tango.TangoUnityImageData imgBuffer;
	public Vector3 cameraPos;
	public Quaternion cameraRot;
	public float uOffset;
	public float vOffset;
	public Dictionary<long, FrameObjectData> frameObjects;

	void Awake () {
		QualitySettings.vSyncCount = 0;  // VSync must be disabled
		Application.targetFrameRate = 30;
	}

	void Start()
	{
		tango = FindObjectOfType <TangoApplication> ();
		tango.Register (this);
		frameNumber = 0;
		frameObjects = new Dictionary<long, FrameObjectData> ();
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

		int publishedFrameNo = NdnRtc.videoStream.processIncomingFrame (imageBuffer);

		if (publishedFrameNo > 0) {
			// frame was published succesfully, do something here
		} else {
			// frame was dropped by the encoder and was not published
		}
	}

	void saveData(Tango.TangoUnityImageData imageBuffer, long frameNumber)
	{
		spawn = prefab.GetPooledInstance<FrameObjectData>();
		//this doesn't work for some reason
		/*spawn.setFrameObjectData(gameObject.GetComponent<TangoARScreen> ().m_screenUpdateTime,
			frameNumber, imageBuffer, 
			gameObject.GetComponent<TangoPointCloud> ().m_points,
			gameObject.GetComponent<TangoPointCloud> ().m_pointsCount,
			gameObject.GetComponent<TangoPoseController> ().finalPosition,
			gameObject.GetComponent<TangoPoseController> ().finalRotation,
			gameObject.GetComponent<TangoARScreen> ().m_uOffset,
			gameObject.GetComponent<TangoARScreen> ().m_vOffset);*/
		spawn.timestamp = gameObject.GetComponent<TangoARScreen> ().m_screenUpdateTime;
		spawn.frameNumber = frameNumber;
		spawn.imageBuffer = imageBuffer;
		spawn.points = gameObject.GetComponent<TangoPointCloud> ().m_points;
		spawn.numPoints = gameObject.GetComponent<TangoPointCloud> ().m_pointsCount;
		spawn.camPos = gameObject.GetComponent<TangoPoseController> ().transform.position;
		spawn.camRot = gameObject.GetComponent<TangoPoseController> ().transform.rotation;
		spawn.uOffset = gameObject.GetComponent<TangoARScreen> ().m_uOffset;
		spawn.vOffset = gameObject.GetComponent<TangoARScreen> ().m_vOffset;

	}

		
}
