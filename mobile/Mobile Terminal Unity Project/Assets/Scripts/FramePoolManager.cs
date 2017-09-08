using System.Collections;
using System.Collections.Generic;
using Tango;
using UnityEngine;

public class FramePoolManager : MonoBehaviour, ITangoPointCloud {

	public UnityEngine.UI.Text textbox;
	public FrameObjectData prefab;
	public FrameObjectData spawn;
	public Vector3[] points;
	public int numPoints;
	public Dictionary<int, FrameObjectData> frameObjects;
	private TangoPointCloud cloud;


	//create frame object with associated data and add it to the dictionary of frame objects
	public void CreateFrameObject(Tango.TangoUnityImageData imageBuffer, int frameNumber, double timestamp, Vector3 cameraPos, Quaternion cameraRot, float uOffset, float vOffset, Camera cam)
	{
		spawn = prefab.GetPooledInstance<FrameObjectData>();
		spawn.timestamp = timestamp;
		spawn.frameNumber = frameNumber;
		spawn.imageBuffer = imageBuffer;
		points = cloud.m_points;
		numPoints = cloud.m_pointsCount;
		spawn.points = points;
		spawn.numPoints = numPoints;
		spawn.camPos = cameraPos;
		spawn.camRot = cameraRot;
		spawn.uOffset = uOffset;
		spawn.vOffset = vOffset;
		spawn.cam = cam;
		spawn.lifeTime = 60;
//		Debug.Log ("Frame info time: " + timestamp);
//		Debug.Log ("Frame info camera position: " + cameraPos);
//		Debug.Log ("Frame info camera rotation: " + cameraRot);
//		Debug.Log ("Frame info points number: " + numPoints);
//		Debug.Log ("Frame info points: " + points.ToString());
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
		Debug.Log ("Frame info: in on point cloud available");
		//points = cloud.m_points;
		//numPoints = cloud.m_pointsCount;
	}

	// Use this for initialization
	void Awake () {
		cloud = PointCloudGUI.FindObjectOfType<TangoPointCloud>();
		frameObjects = new Dictionary<int, FrameObjectData> ();

	}
	
	// Update is called once per frame
	void Update () {
		textbox.text = "" + frameObjects.Count;
	}
}
