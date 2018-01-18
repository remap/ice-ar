using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

public class BoundingBoxObjectData : PooledObject {

	public GameObject box;
	public GameObject lense;
	public BoxCollider col;
	public VectorLine line;
	public int frameCount;
	public Color color;
	public string labelText;
	public string guid;
	public BoundingBoxPoolManager boxMgr;

	// Use this for initialization
	void Start () {
		frameCount = 10;

		//vertices for bounding box lines
		var vertices = new Vector3[8];
		var thisMatrix = box.transform.localToWorldMatrix;
		var storedRotation = box.transform.rotation;
		box.transform.rotation = Quaternion.identity;


		vertices[0] = col.center + new Vector3 (col.size.x, col.size.y, col.size.z) * 0.5f;
		vertices[1] = col.center + new Vector3 (-col.size.x, col.size.y, col.size.z) * 0.5f;
		vertices[2] = col.center + new Vector3 (col.size.x, col.size.y, -col.size.z) * 0.5f;
		vertices[3] = col.center + new Vector3 (-col.size.x, col.size.y, -col.size.z) * 0.5f;
		vertices[4] = col.center + new Vector3 (col.size.x, -col.size.y, col.size.z) * 0.5f;
		vertices[5] = col.center + new Vector3 (-col.size.x, -col.size.y, col.size.z) * 0.5f;
		vertices[6] = col.center + new Vector3 (col.size.x, -col.size.y, -col.size.z) * 0.5f;
		vertices[7] = col.center + new Vector3 (-col.size.x, -col.size.y, -col.size.z) * 0.5f;


		box.transform.rotation = storedRotation;

		var boxPoints = new List<Vector3>{
			vertices[5],
			vertices[4],
			vertices[1],
			vertices[5],
			vertices[4], 
			vertices[0], 
			vertices[0], 
			vertices[1],
			vertices[3], 
			vertices[1], 
			vertices[0], 
			vertices[2], 
			vertices[2], 
			vertices[3], 
			vertices[7],
			vertices[3], 
			vertices[2], 
			vertices[6], 
			vertices[6], 
			vertices[7], 
			vertices[5], 
			vertices[7], 
			vertices[6], 
			vertices[4]};


		line = new VectorLine ("BoundingBoxLines", boxPoints, 5.0f);
		line.color = color;
		//line.joins = Joins.Weld;
		line.drawTransform = box.transform;
		line.Draw3DAuto ();
		//line.active = false;
		boxMgr = GameObject.FindObjectOfType<BoundingBoxPoolManager>();
		//this is super slow
		//VectorManager.ObjectSetup (box, line, Visibility.Dynamic, Brightness.None);

	}


	// Update is called once per frame
	void LateUpdate () {
		//rotations for testing
		//box.transform.RotateAround (box.transform.position, box.transform.TransformDirection(Vector3.up), 5f);
		//box.transform.rotation = Quaternion.LookRotation (Camera.main.transform.up, -Camera.main.transform.forward) * Quaternion.Euler (90f, 0, 0);
		box.transform.LookAt (box.transform.position + Camera.main.transform.rotation * Vector3.forward,
			Camera.main.transform.rotation * Vector3.up);
			frameCount--;
			if (frameCount == 0) {
				frameCount = 10;
				Release();
			}
	}
		

	public void Release()
	{
		line.active = false;
		//lense.SetActive (false);
		//remove bounding box from the dictionary
//		try
//		{
//			List<BoundingBox> list = boxMgr.boundingBoxObjects[labelText];
//			for(int i = 0; i < list.Count; i++)
//			{
//				if(list[i].guid == guid)
//				{
//					list.RemoveAt(i);
//					boxMgr.boxCount--;
//				}
//			}
//		}
//		catch(KeyNotFoundException) {
//			
//		}
		ReturnToPool ();
	}

	void OnDestroy()
	{
		VectorLine.Destroy (ref line);
	}
		
}
