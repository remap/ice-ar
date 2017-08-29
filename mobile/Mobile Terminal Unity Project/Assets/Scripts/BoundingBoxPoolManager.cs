using System.Collections;
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


		//set label text
		spawnText.mesh.SetText(label);

		//set rect transform to size of text
		spawnText.rect.sizeDelta = new Vector2 (spawnText.mesh.preferredWidth, spawnText.mesh.preferredHeight);
		//move label up based on text size
		spawnText.rect.transform.position = spawnText.rect.transform.position + new Vector3 (0, (spawnText.mesh.preferredHeight / 2), 0);
		//set label background
		spawnText.plane.transform.position = spawnText.rect.transform.position;
		//move background slightly back for readability
		spawnText.plane.transform.localPosition = new Vector3 (0, 0, 0.05f);

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
