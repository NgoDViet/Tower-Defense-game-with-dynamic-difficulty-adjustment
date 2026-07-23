using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// it will be used in a new feature
/// </summary>

public class Hero_Controller : MonoBehaviour {
    public int life = 100;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ReduceLife(int value) {
        if (life - value > 0)
        {
            life = life - value;
        }
        else {
            Destroy(this.gameObject);
        }
    }
}
