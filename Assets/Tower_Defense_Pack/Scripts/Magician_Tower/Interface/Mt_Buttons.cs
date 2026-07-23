using UnityEngine;
using System.Collections;
using FThLib;

/// <summary>
/// Magician Tower Interface Buttons Controller
/// It controls the action of the buttons reative to their name
/// It removes the money after purchase the new Tower Upgrade
/// </summary>

public class Mt_Buttons : MonoBehaviour {
	public Sprite clicked;
	private bool mouseover=false;
	private Sprite aux;
	private Master_Instance masterPoint;
	//Arrow improvable properties
	private float s_timer = 0.4f;                   // Ratio, time between shots
	private int damage = 5;                         //Upgrade damage 3 -> 5

    //Show hand
	void OnMouseOver(){ 
		if(!GameObject.Find("hand")&&!GameObject.Find("circle")){master.showHand (true);}
		mouseover=true;
	}
	//Hide hand
	void OnMouseExit(){
		if(GameObject.Find("hand")){master.showHand (false);}
		mouseover=false;
	}
    /// <summary>
    /// If this button has been clicked -> change its image, and set as unclickeable
    /// </summary>
    void Start () {
		if(this.gameObject.transform.parent.transform.parent.gameObject.GetComponent<Buttons_Clicked>().isClicked(this.gameObject.name)==true){
			GameObject unclickeable = Instantiate(Resources.Load("Buttons/Unclickeable"), this.transform.position , Quaternion.identity)as GameObject;
			unclickeable.transform.parent = this.transform.parent;
			Destroy(this.gameObject);
		}else{
			masterPoint = GameObject.Find("Instance_Point").GetComponent<Master_Instance>();
			if(masterPoint.countMoney()<masterPoint.getPrice(this.gameObject)){master.isHide(this.gameObject); }                         //Disable button if no money
            if (this.gameObject.name=="Trap"){
				enableTrap();
			}
			aux = GetComponent<SpriteRenderer>().sprite;
			master.setLayer("interface",this.gameObject);
		}
	}
	
	// Update is called once per frame
	void Update () {
		if(this.gameObject.name=="Trap"){
			enableTrap();
		}
		if(this.gameObject.GetComponent<SpriteRenderer>().material.color.a!=0.5f){                                                      //It is not disabled...
			if (Input.GetMouseButtonDown(0)&&mouseover==true){
				GetComponent<SpriteRenderer>().sprite = clicked;
				masterPoint.anybuttonclicked=true;
			}
			if (Input.GetMouseButtonUp(0)&&GetComponent<SpriteRenderer>().sprite==clicked){
				master.showHand(false);
				masterPoint.removeMoney(masterPoint.getPrice(this.gameObject));                                                         //Remove money
				GetComponent<SpriteRenderer>().sprite = aux;
				action();                                                                                                               //Action
			}
		}else{
			if (Input.GetMouseButtonDown(0)&&mouseover==true){
				master.showHand(false);
			}
		}
		if (Input.GetMouseButtonDown(0)&&mouseover==false&&this.gameObject.GetComponent<SpriteRenderer>().enabled==true){
			Invoke ("mouseDownDelay",0.01f);
		}
	}
    /// <summary>
    /// Select the action relative to the button name
    /// </summary>
	private void action(){
		if(this.gameObject.name=="sell"){
            GameObject.Find("UI").GetComponent<AudioSource>().Play();
			masterPoint.addMoney((int)(masterPoint.getPrice(this.gameObject.transform.parent.transform.parent.gameObject)/3)*2);
			master.sellTower(this.gameObject.transform.parent.transform.parent.gameObject);
			actionEnd();}
		if(this.gameObject.name=="MTFire"){
            GameObject.Find("UI").GetComponent<AudioSource>().Play();
            this.gameObject.transform.parent.transform.parent.GetComponent<MT_Controller>().fire=true;
			this.gameObject.transform.parent.transform.parent.GetComponent<MT_Controller>().Damage_ = damage;
			actionEnd();
		}
		if(this.gameObject.name=="Ice"){
            GameObject.Find("UI").GetComponent<AudioSource>().Play();
            this.gameObject.transform.parent.transform.parent.GetComponent<MT_Controller>().ice=true;
			actionEnd();
		}
		if(this.gameObject.name=="Trap"){
            GameObject.Find("UI").GetComponent<AudioSource>().Play();
            if (master.getChildFrom("trap_",this.transform.parent.transform.parent.gameObject)==null){                                   //Only 1 trap at same time
				showTrap(true);
				this.gameObject.transform.parent.transform.parent.GetComponent<MT_Controller>().trap=true;
			}
			actionEnd();                                                                                                               
		}
	}
    /// <summary>
    /// Remove this interface
    /// </summary>
	private void actionEnd(){
		this.gameObject.transform.parent.transform.parent.gameObject.GetComponent<Buttons_Clicked>().addButton(this.gameObject.name);
		this.gameObject.transform.parent.transform.parent.GetComponent<CircleCollider2D>().enabled=true;
		master.getChildFrom("zoneImg",this.transform.parent.transform.parent.gameObject).GetComponent<SpriteRenderer>().enabled=false;
		masterPoint.anybuttonclicked=false;
		Destroy (this.gameObject.transform.parent.gameObject);
	}
    /// <summary>
    /// Delay after mouse down to destroy this interface
    /// </summary>
    void mouseDownDelay(){
		if(masterPoint.anybuttonclicked==false){
			this.gameObject.transform.parent.transform.parent.GetComponent<CircleCollider2D>().enabled=true;
			master.getChildFrom("zoneImg",this.transform.parent.transform.parent.gameObject).GetComponent<SpriteRenderer>().enabled=false;
			Destroy (this.gameObject.transform.parent.gameObject);
		}
	}
    /// <summary>
    /// Active the trap
    /// </summary>
	public void enableTrap(){
		if(master.getChildFrom("trap_",this.gameObject.transform.parent.transform.parent.gameObject)){
			Color col = this.gameObject.GetComponent<SpriteRenderer>().material.color;
			col.a = 0.5f;
			this.gameObject.GetComponent<SpriteRenderer>().material.color = col;
		}else{Color col = this.gameObject.GetComponent<SpriteRenderer>().material.color;
			col.a = 1f;
			this.gameObject.GetComponent<SpriteRenderer>().material.color = col;
		}
	}
    /// <summary>
    /// It shows a trap as cursor
    /// </summary>
    /// <param name="value"></param>
	public void showTrap(bool value){
		if(value==true){
			Cursor.visible = false;
			if(Camera.main){
				Vector3 aux = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				aux.z=-0.5f;
				GameObject trap_ = Instantiate(Resources.Load("Cursor/trap"), aux , Quaternion.identity)as GameObject;
				trap_.name="trap_";
				trap_.gameObject.transform.parent = this.transform.parent.transform.parent.gameObject.transform;
			}else{
				Debug.Log("errr");
			}
		}else{
			Cursor.visible = true;
			Destroy (GameObject.Find("trap"));
		}
	}
}
