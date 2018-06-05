using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;
using TMPro;

public class LabelData : PooledObject {

	public GameObject text;
	public GameObject box;
	public BoxCollider col;
	public GameObject plane;
	public LabelBackground background;
	//public MeshRenderer render;
	public VectorLine textBox;
	public TextMeshPro mesh;
	public RectTransform rect;
	public int frameCount;
	public Color color;
	public string labelText;
	public string guid;
	public BoundingBoxPoolManager boxMgr;
	public OnCameraFrame camFrame;

	void Awake() {
		background = gameObject.GetComponentInChildren<LabelBackground>();
		background.render.material.color = color;
	}

	// Use this for initialization
	void Start () {

		//label
		mesh.color = Color.black;

//		background = gameObject.GetComponentInChildren<LabelBackground>();
//		background.render.material.color = color;

		frameCount = 10;

		boxMgr = GameObject.FindObjectOfType<BoundingBoxPoolManager>();
		camFrame = GameObject.FindObjectOfType<OnCameraFrame>();
		//not using label border lines right now
//		var thisMatrix = box.transform.localToWorldMatrix;
//		var storedRotation = box.transform.rotation;
//		box.transform.rotation = Quaternion.identity;
//		var vertices = new Vector3[4];
//
//
//		vertices[0] = col.center + new Vector3 (col.size.x, col.size.y, 0) * 0.5f;
//		vertices[1] = col.center + new Vector3 (-col.size.x, col.size.y, 0) * 0.5f;
//		vertices[2] = col.center + new Vector3 (col.size.x, -col.size.y, 0) * 0.5f;
//		vertices[3] = col.center + new Vector3 (-col.size.x, -col.size.y, 0) * 0.5f;
//
//		box.transform.rotation = storedRotation;
//
//		var boxPoints = new List<Vector3>{
//			vertices[0],
//			vertices[2],
//			vertices[1],
//			vertices[3],
//			vertices[2], 
//			vertices[0], 
//			vertices[0], 
//			vertices[1],
//			vertices[3], 
//			vertices[1], 
//			vertices[3], 
//			vertices[2]};
//
//
//		textBox = new VectorLine ("LabelBoxLines", boxPoints, 5.0f);
//		textBox.color = color;
//		//line.joins = Joins.Weld;
//		textBox.drawTransform = mesh.transform;
//		textBox.Draw3DAuto ();

	}
	
	// Update is called once per frame
	void LateUpdate () {
		text.transform.LookAt (text.transform.position + Camera.main.transform.rotation * Vector3.forward,
			Camera.main.transform.rotation * Vector3.up);
		//remove after 10 frames
		frameCount--;
		if (frameCount == 0) {
			frameCount = 10;
			Release ();
		}

		//testing for rotations and resizing
//		//rotate to camera
		//text.transform.rotation = Quaternion.LookRotation (Camera.main.transform.up, -Camera.main.transform.forward) * Quaternion.Euler (90f, 0, 0);
//
//		//resize based on distance from camera
//		if (Mathf.Abs (Camera.main.transform.position.z - text.transform.position.z) > Mathf.Abs (Camera.main.transform.position.x - text.transform.position.x))
//		{
//			mesh.characterSize = Mathf.Abs (Camera.main.transform.position.z - text.transform.position.z) / 28;
//			//textBack.SetWidth (Mathf.Abs (Camera.main.transform.position.z - text.transform.position.z) + 20);
//		}
//		else {
//			mesh.characterSize = Mathf.Abs (Camera.main.transform.position.x - text.transform.position.x) / 28;
//			//textBack.SetWidth (Mathf.Abs (Camera.main.transform.position.x - text.transform.position.x) + 20);
//		}

	}

	//not needed anymore?
	/*public void SizeCollider()
	{   

		col.center = new Vector3 (0, 0, 0);
		col.size = new Vector3 (rect.sizeDelta.x, rect.sizeDelta.y, 0);
		var vertices = new Vector3[4];


		vertices[0] = col.center + new Vector3 (col.size.x, col.size.y, 0) * 0.5f;
		vertices[1] = col.center + new Vector3 (-col.size.x, col.size.y, 0) * 0.5f;
		vertices[2] = col.center + new Vector3 (col.size.x, -col.size.y, 0) * 0.5f;
		vertices[3] = col.center + new Vector3 (-col.size.x, -col.size.y, 0) * 0.5f;

		textBox.points3 = new List<Vector3>{
			vertices[0],
			vertices[2],
			vertices[1],
			vertices[3],
			vertices[2], 
			vertices[0], 
			vertices[0], 
			vertices[1],
			vertices[3], 
			vertices[1], 
			vertices[3], 
			vertices[2]};

	}*/
		

	public void Release()
	{
		Debug.Log ("remove box label = " + labelText);
		textBox.active = false;
		//remove label from the dictionary
		try
		{
			List<BoundingBox> list = boxMgr.boundingBoxObjects[labelText];
			for(int i = 0; i < list.Count; i++)
			{
				if(list[i].guid == guid)
				{
					Debug.Log("remove box");
					list.RemoveAt(i);
					camFrame.kalman.Remove(guid);
				}
			}
		}
		catch(KeyNotFoundException) {
			Debug.Log ("exception caught box removal: KeyNotFoundException");
		}
		ReturnToPool();
	}
}
