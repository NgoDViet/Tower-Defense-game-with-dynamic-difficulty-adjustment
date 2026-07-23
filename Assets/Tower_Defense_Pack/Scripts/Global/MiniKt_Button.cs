using UnityEngine;
using System.Collections;
using FThLib;

/// <summary>
/// It is used by the MINIKT0 prfab (It is created by the 2 knights patrol button at left/bottom of the screen)
/// Set cursor as flag when you are placing the instantiated patrol
/// </summary>
public class MiniKt_Button : MonoBehaviour {
	public float delay = 30f;
	private bool mouseover=false;
	private Sprite aux;
	private Master_Instance masterPoint;
	private bool on=true;
	
    //Show hand
	void OnMouseOver(){ 
		if(!GameObject.Find("hand")&&!GameObject.Find("circle")){master.showHand (true);}
		Cursor.visible = false;
		mouseover=true;
	}
	//Hide hand
	void OnMouseExit(){
		if(GameObject.Find("hand")){master.showHand (false);}
		mouseover=false;
	}

	void Start () {
		masterPoint = GameObject.Find("Instance_Point").GetComponent<Master_Instance>();
		aux = GetComponent<SpriteRenderer>().sprite;
		master.setLayer("interface",this.gameObject);
		this.gameObject.GetComponent<Animator>().SetBool ("on",on);
	}
	
	// Update is called once per frame
	void Update () {
		if(!master.isFinish()){
			if (Input.GetMouseButtonDown(0)&&GameObject.Find("flag_")||Input.GetMouseButtonUp(0)&&GameObject.Find("flag_")){action();}
			if (Input.GetMouseButtonDown(0)&&mouseover==true){
				if(GameObject.Find("Interface")){Destroy (GameObject.Find("Interface"));}
                GameObject.Find("UI").GetComponent<AudioSource>().Play();
                master.showHand(false);
				showFlag();
				if(GameObject.Find("MiniKT0")){
					Destroy(GameObject.Find("MiniKT0"));
				}
			}
		}
	}
    /// <summary>
    /// Set the mouse cursor image as Flag
    /// </summary>
	private void showFlag(){
		Destroy (GameObject.Find("hand"));
		GameObject flag = Instantiate(Resources.Load("Cursor/flag_"), GameObject.Find("Out").transform.position , Quaternion.identity)as GameObject;
		flag.transform.SetParent(this.gameObject.transform);
		flag.name="flag_";
	}
    /// <summary>
    /// Start respawn progress bar
    /// </summary>
	private void action(){
		on = false;
		this.gameObject.GetComponent<Animator>().SetBool ("on",on);
		this.gameObject.GetComponent<BoxCollider2D>().enabled=on;
		master.Instantiate_Progressbar(delay,this.gameObject);
		Invoke("setOn",delay);
	}
    /// <summary>
    /// It is used to avoid problems when instantiating
    /// </summary>
	private void setOn(){
		if(master.getChildFrom("MiniKT0",this.gameObject)){
			Destroy (master.getChildFrom("MiniKT0",this.gameObject));
		}
		on=true;
		this.gameObject.GetComponent<Animator>().SetBool ("on",on);
		this.gameObject.GetComponent<BoxCollider2D>().enabled=on;
	}
}
