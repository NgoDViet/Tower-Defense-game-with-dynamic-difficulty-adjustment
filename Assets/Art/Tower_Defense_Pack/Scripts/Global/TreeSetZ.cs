using UnityEngine;
using System.Collections;
using FThLib;

/// <summary>
/// It is used to set the Z position relative to the Y position of the one tree
/// </summary>
public class TreeSetZ : MonoBehaviour {

	// Use this for initialization
	void Start () {
		this.transform.position = master.setThisZ(this.transform.position,0.02f);
	}

}
