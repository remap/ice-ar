using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LastFrameData : MonoBehaviour {

	public double timestamp;
	public Tango.TangoUnityImageData imageBuffer;
	public Vector3 camPos;
	public Quaternion camRot;
	public float uOffset;
	public float vOffset;

	public void setLastFrameData (double ts, Tango.TangoUnityImageData fd, Vector3 cp, Quaternion cr, float u, float v)
	{
		timestamp = ts;
		imageBuffer = fd;
		camPos = cp;
		camRot = cr;
		uOffset = u;
		vOffset = v;
	}

	// Use this for initialization
	void Start () {
		timestamp = 0;
		imageBuffer = null;
		camPos = Vector3.zero;
		camRot = Quaternion.identity;
		uOffset = 0.0f;
		vOffset = 0.0f;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
