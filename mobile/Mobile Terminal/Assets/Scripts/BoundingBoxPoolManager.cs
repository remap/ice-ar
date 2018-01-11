using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoundingBoxPoolManager : MonoBehaviour {

	public UnityEngine.UI.Text textbox;
	public BoundingBoxObjectData prefab;
	public BoundingBoxObjectData spawn;
	public LabelData prefabText;
	public LabelData spawnText;
	//public LenseObjects lenseObjects;
	public Dictionary<string, List<BoundingBox>> boundingBoxObjects;


	public void CreateBoundingBoxObject(Vector3 position, float x, float y, float z, string label, Color color)
	{

		string guid = System.Guid.NewGuid().ToString();

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
		spawn.line.color = color;
		spawn.labelText = label;
		spawn.line.active = true;

		spawn.guid = guid;

		//spawn.lense = lenseObjects.GetLense (label, position);
		//spawn.lense.SetActive (true);

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

		//these lines down to the try trigger a null reference exception
		//label background should be slightly larger than text mesh size (plane scale is 10x normal object scale)
		spawnText.plane.transform.localScale = new Vector3(spawnText.rect.sizeDelta.x/9.5f, 0, spawnText.rect.sizeDelta.y/10);
//		//spawnText.color = color;
		spawnText.background.render.material.color = color;
		spawnText.labelText = label;
		//spawnText.textBox.active = true;
		try{
			//spawnText.labelText = label;
			//spawnText.plane.transform.localScale = new Vector3(spawnText.rect.sizeDelta.x/9.5f, 0, spawnText.rect.sizeDelta.y/10);
			//spawnText.color = color;
			//spawnText.background.render.material.color = color;
			//spawnText.labelText = label;
		}
		catch(System.Exception e) {
			Debug.Log ("exception caught creating box: " + e);
		}
		spawnText.guid = guid;

		//by {label, list<boxes>}?

		List<BoundingBox> boxObjects;
		if(boundingBoxObjects.TryGetValue(label, out boxObjects))
		{
			//found list of boxObjects for this label, just update the list
			BoundingBox ob = new BoundingBox();
			ob.box = spawn;
			ob.label = spawnText;
			ob.guid = guid;
			ob.direction = Vector3.zero;
			ob.speed = 0.0f;
			ob.x = x;
			ob.y = y;
			ob.z = z;
			boxObjects.Add (ob);
			boundingBoxObjects [label] = boxObjects;
		}
		else
		{
			//list of boxOjects doens't exit, create one and add to dictionary
			BoundingBox ob = new BoundingBox();
			ob.box = spawn;
			ob.label = spawnText;
			ob.guid = guid;
			ob.direction = Vector3.zero;
			ob.speed = 0.0f;
			ob.x = x;
			ob.y = y;
			ob.z = z;
			boxObjects = new List<BoundingBox> ();
			boxObjects.Add (ob);
			boundingBoxObjects.Add (label, boxObjects);
		} 
		string output = "";
		int boxCount = 0;
		foreach( KeyValuePair<string, List<BoundingBox>> kvp in boundingBoxObjects )
		{
			output = output + "; " + kvp.Key;
			for (int i = 0; i < kvp.Value.Count; i++) {
				boxCount++;
			}
		}
		Debug.Log ("Label output and box count: " + boxCount + " " + output);

	}

	public void UpdateBoundingBoxObject(BoundingBox boundingBox, Vector3 position, float x, float y, float z, string label, Vector3 previousPos)
	{
		try{
		BoundingBoxObjectData boxObject = new BoundingBoxObjectData();
		LabelData labelObject = new LabelData();
		boxObject = boundingBox.box;
		labelObject = boundingBox.label;

		//bounding box
		boxObject.box.transform.position = position;
		//remove old rotations
		boxObject.box.transform.rotation = Quaternion.identity;
		//face the camera
		boxObject.box.transform.LookAt (boxObject.box.transform.position + Camera.main.transform.rotation * Vector3.forward,
			Camera.main.transform.rotation * Vector3.up);
		//size and color box
		boxObject.box.transform.localScale = new Vector3 (x, y, z);


		//label is centered on top of front face of box
		labelObject.text.transform.position = boxObject.box.transform.position + new Vector3(0, y/2, -z/2);


		//set text size and label text
		float depth = Mathf.Abs(position.z);
		if(depth < 0.5f)
			labelObject.mesh.fontSize = 0.2f;
		else if(depth < 1.0f)
			labelObject.mesh.fontSize = 0.5f;
		else if(depth < 1.5f)
			labelObject.mesh.fontSize = 1.0f;
		else
			labelObject.mesh.fontSize = 1.5f;

		labelObject.mesh.SetText(label + " - " + depth.ToString("F2") + "m");

		//set rect transform to size of text
		labelObject.rect.sizeDelta = new Vector2 (labelObject.mesh.preferredWidth, labelObject.mesh.preferredHeight);
		//move label up based on text size
		labelObject.rect.transform.position = labelObject.rect.transform.position + new Vector3 (0, (labelObject.mesh.preferredHeight / 2), 0);
		//set label background
		labelObject.plane.transform.position = labelObject.rect.transform.position;
		//move background slightly back for readability
		labelObject.plane.transform.localPosition = new Vector3 (0, 0, 0.01f);

		//label faces the camera
		labelObject.rect.transform.LookAt (labelObject.rect.transform.position + Camera.main.transform.rotation * Vector3.forward,
			Camera.main.transform.rotation * Vector3.up);

		//label background should be slightly larger than text mesh size (plane scale is 10x normal object scale)
		labelObject.plane.transform.localScale = new Vector3(labelObject.rect.sizeDelta.x/9.5f, 0, labelObject.rect.sizeDelta.y/10);

		boxObject.frameCount = 10;
		labelObject.frameCount = 10;

//		boundingBox.direction = direction;
//		boundingBox.speed = speed;
		boundingBox.last = previousPos;

		string output = "";
		int boxCount = 0;
		foreach( KeyValuePair<string, List<BoundingBox>> kvp in boundingBoxObjects )
		{
			for (int i = 0; i < kvp.Value.Count; i++) {
				//output = output + "; " + kvp.Key + ", " + kvp.Value [i].guid;
				boxCount++;
			}
		}
		Debug.Log ("Label output and box count: " + boxCount + " " + output);
		}
		catch(System.Exception e) {
			Debug.Log ("exception caught in bounding box manager update: " + e);
		}
	}

	// Use this for initialization
	void Start () {
		Screen.sleepTimeout = (int)SleepTimeout.NeverSleep;
		boundingBoxObjects = new Dictionary<string, List<BoundingBox>> ();
		//lenseObjects = GameObject.FindObjectOfType<LenseObjects>();
	}
	
	// Update is called once per frame
	void Update () {
		//textbox.text = "" + boundingBoxObjects.Count;
	}
}


public struct BoundingBox
{
	public BoundingBoxObjectData box;
	public LabelData label;
	public Vector3 direction;
	public float speed;
	public float x, y, z;
	public Vector3 last;
	public string guid;
}
