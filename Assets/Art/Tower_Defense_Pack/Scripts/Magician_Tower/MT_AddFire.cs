using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// It is used to create Fire to one gameobject
/// </summary>
public class MT_AddFire: MonoBehaviour {
	private List<GameObject> firelist;                                                                  //Fire Object Pooling list
	private bool on=false;
	// Use this for initialization
	void Start () {
		firelist = GameObject.Find("Instance_Point").GetComponent<Fire_Pooling>().fireList;
	}
	
	// Update is called once per frame
	void Update () {
		if(on==false){
			CreateFire();
		}
	}

	void CreateFire(){
		if(on==false){
			for(int i = 0;i<firelist.Count;i++){
				if(!firelist[i].activeInHierarchy){
					firelist[i].transform.localScale= new Vector3(0.06f,0.06f,0.06f);
					firelist[i].transform.position = this.transform.position;
					firelist[i].SetActive(true);
					break;
				}
			}
		}
	}

	IEnumerator CreateFire_(){
		yield return new WaitForSeconds(0);
		for(int i = 0;i<firelist.Count;i++){
			if(!firelist[i].activeInHierarchy){
				firelist[i].transform.localScale= new Vector3(0.065f,0.065f,0.065f);
				firelist[i].transform.position = this.transform.position;
				firelist[i].SetActive(true);
				break;
			}
		}
		on=false;
	}
}
