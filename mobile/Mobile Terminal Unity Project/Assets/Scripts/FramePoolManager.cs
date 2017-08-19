using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FramePoolManager : MonoBehaviour, ITangoPointCloud {

	public UnityEngine.UI.Text textbox;
	public FrameObjectData prefab;
	public FrameObjectData spawn;
	public Vector3[] points;
	public int numPoints;
	public Dictionary<long, FrameObjectData> frameObjects;


	//create frame object with associated data and add it to the dictionary of frame objects
	public void CreateFrameObject(Tango.TangoUnityImageData imageBuffer, int frameNumber, double timestamp, Vector3 cameraPos, Quaternion cameraRot, float uOffset, float vOffset)
	{
		spawn = prefab.GetPooledInstance<FrameObjectData>();
		spawn.timestamp = timestamp;
		spawn.frameNumber = frameNumber;
		spawn.imageBuffer = imageBuffer;
		spawn.points = points;
		spawn.numPoints = numPoints;
		spawn.camPos = cameraPos;
		spawn.camRot = cameraRot;
		spawn.uOffset = uOffset;
		spawn.vOffset = vOffset;
		spawn.lifeTime = 60;

		//add frame object to dictionary
		frameObjects.Add (frameNumber, spawn);
	}

	public void RemoveFrameObject(FrameObjectData obj)
	{
		obj.Release ();
	}

	//get new point cloud data when it becomes available
	public void OnTangoPointCloudAvailable(Tango.TangoPointCloudData pointCloud)
	{
		points = gameObject.GetComponent<TangoPointCloud> ().m_points;
		numPoints = gameObject.GetComponent<TangoPointCloud> ().m_pointsCount;
	}

	// Use this for initialization
	void Start () {
		frameObjects = new Dictionary<long, FrameObjectData> ();
		points = null;
		numPoints = 0;
	}
	
	// Update is called once per frame
	void Update () {
		textbox.text = "" + frameObjects.Count;
	}
}
