using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LenseObjects : MonoBehaviour {

	public Dictionary<string, GameObject> objects;

	// Use this for initialization
	void Awake () {
		objects = new Dictionary<string, GameObject> ();
//		GameObject bottle = Resources.Load<GameObject>("bottle") as GameObject;
//		GameObject car = Resources.Load<GameObject>("car") as GameObject;
//		GameObject tv = Resources.Load<GameObject>("tv") as GameObject;
//		GameObject chair = Resources.Load<GameObject>("chair") as GameObject;
//		objects.Add ("bottle", bottle);
//		objects.Add ("car", car);
//		objects.Add ("tvmonitor", tv);
//		objects.Add ("chair", chair);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

//	public GameObject GetLense(string label, Vector3 position)
//	{
//		return Instantiate (objects [label], position, Quaternion.identity) as GameObject;
//	}

	//build dictionary with key "object label" and value of GameObject corresponding to label
	//when creating or updating a bounding box, grab a copy of the GameObject to use

	//to have multiple of one object, keep a list of the GameObjects for the label,
	//use the object that are not active, if no inactive object then create a new one
}
