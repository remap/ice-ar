using System;
using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;

public class FramePoolManager : MonoBehaviour {

	public UnityEngine.UI.Text textbox;
	public FrameObjectData prefab;
	public FrameObjectData spawn;
	public List<Vector4> points;
	public int numPoints;
	public Dictionary<int, FrameObjectData> frameObjects;


	//create frame object with associated data and add it to the dictionary of frame objects
	public void CreateFrameObject(int frameNumber, double timestamp, Vector3 cameraPos, Quaternion cameraRot, Camera cam)
	{
		spawn = prefab.GetPooledInstance<FrameObjectData>();
		spawn.timestamp = timestamp;
		spawn.frameNumber = frameNumber;
		Frame.PointCloud.CopyPoints(points);
		numPoints = Frame.PointCloud.PointCount;
		spawn.points = points;
		spawn.numPoints = numPoints;
		spawn.camPos = cameraPos;
		spawn.camRot = cameraRot;
		spawn.cam = cam;
		spawn.lifeTime = 30;
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
		

	// Use this for initialization
	void Awake () {
		frameObjects = new Dictionary<int, FrameObjectData> ();

	}
	
	// Update is called once per frame
	void Update () {
		//textbox.text = "" + frameObjects.Count;
	}
}
