using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabelBackground : MonoBehaviour {

	public MeshRenderer render;

	// Use this for initialization
	void Awake () {
		render = gameObject.GetComponent<MeshRenderer>() as MeshRenderer;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
