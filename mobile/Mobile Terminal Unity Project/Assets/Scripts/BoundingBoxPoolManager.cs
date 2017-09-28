﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoundingBoxPoolManager : MonoBehaviour {

	public BoundingBoxObjectData prefab;
	public BoundingBoxObjectData spawn;
	public LabelData prefabText;
	public LabelData spawnText;
	public Dictionary<string, List<BoundingBoxObjectData>> boundingBoxObjects;


	public void CreateBoundingBoxObject(Vector3 position, float x, float y, float z, string label, Color color)
	{

		//bounding box
		spawn = prefab.GetPooledInstance<BoundingBoxObjectData>();
		spawn.box.transform.position = position;
		//remove old rotations
		spawn.box.transform.rotation = Quaternion.identity;
		//face the camera
		spawn.box.transform.LookAt (spawn.box.transform.position + Camera.main.transform.rotation * Vector3.forward,
			Camera.main.transform.rotation * Vector3.up);
		//size and color box
		spawn.box.transform.localScale = new Vector3 (x, y, z);
		spawn.color = color;
		spawn.line.active = true;

		//label
		spawnText = prefabText.GetPooledInstance<LabelData> ();
		//label is centered on top of front face of box
		spawnText.text.transform.position = spawn.box.transform.position + new Vector3(0, y/2, -z/2);


		//set text size and label text
		float depth = Mathf.Abs(position.z);
		if(depth < 0.5f)
			spawnText.mesh.fontSize = 0.2f;
		else if(depth < 1.0f)
			spawnText.mesh.fontSize = 0.5f;
		else if(depth < 1.5f)
			spawnText.mesh.fontSize = 1.0f;
		else
			spawnText.mesh.fontSize = 1.5f;

		Debug.Log ("fontsize: " + spawnText.mesh.fontSize + "; depth: " + depth.ToString("F2"));


		spawnText.mesh.SetText(label + " - " + depth.ToString("F2") + "m");

		//set rect transform to size of text
		spawnText.rect.sizeDelta = new Vector2 (spawnText.mesh.preferredWidth, spawnText.mesh.preferredHeight);
		//move label up based on text size
		spawnText.rect.transform.position = spawnText.rect.transform.position + new Vector3 (0, (spawnText.mesh.preferredHeight / 2), 0);
		//set label background
		spawnText.plane.transform.position = spawnText.rect.transform.position;
		//move background slightly back for readability
		spawnText.plane.transform.localPosition = new Vector3 (0, 0, 0.01f);

		//label faces the camera
		spawnText.rect.transform.LookAt (spawnText.rect.transform.position + Camera.main.transform.rotation * Vector3.forward,
			Camera.main.transform.rotation * Vector3.up);
		
		//label background should be slightly larger than text mesh size (plane scale is 10x normal object scale)
		spawnText.plane.transform.localScale = new Vector3(spawnText.rect.sizeDelta.x/9.5f, 0, spawnText.rect.sizeDelta.y/10);
		spawnText.color = color;
		spawnText.textBox.active = true;

		//need to figure out what to do about the dictionary, how to store the boxes?
		//by {label, list<boxes>}?

	}

	public void RemoveBoundingBoxObject(BoundingBoxObjectData box)
	{
		box.Release ();
	}

	// Use this for initialization
	void Start () {
		Screen.sleepTimeout = (int)SleepTimeout.NeverSleep;
		boundingBoxObjects = new Dictionary<string, List<BoundingBoxObjectData>> ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
