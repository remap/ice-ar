using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tango;

public class FrameObjectData : PooledObject {
	
	public double timestamp;
	public int frameNumber;
	public Tango.TangoUnityImageData imageBuffer;
	public Vector3[] points;
	public int numPoints;
	public Vector3 camPos;
	public Quaternion camRot;
	public float uOffset;
	public float vOffset;
	public Dictionary<long, FrameObjectData> frameObj;
	public int lifeTime;

	/*public void setFrameObjectData (double ts, int fn, Tango.TangoUnityImageData fd, Vector3[] p, int numP, Vector3 cp, Quaternion cr, float u, float v)
	{
		timestamp = ts;
		frameNumber = fn;
		imageBuffer = fd;
		points = p;
		numPoints = numP;
		camPos = cp;
		camRot = cr;
		uOffset = u;
		vOffset = v;
	}*/

	public void Release()
	{
		//remove frame object from dictionary
		frameObj.Remove (frameNumber);
		ReturnToPool ();
	}

	// Use this for initialization
	void Start () {
		frameObj = GameObject.FindObjectOfType<FramePoolManager>().frameObjects;
		//frame objects live for 2 seconds
		lifeTime = 60;
	}
	
	// Update is called once per frame
	void Update () {
		lifeTime--;
		if (lifeTime == 0) {
			Release ();
		}
	}
}
