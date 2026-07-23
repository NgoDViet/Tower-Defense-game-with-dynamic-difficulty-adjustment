using UnityEngine;
using System.Collections;
using FThLib;

/// <summary>
/// Archers Tower Interface Buttons Controller
/// It controls the action of the buttons reative to their name
/// It removes the money after purchase the new Tower Upgrade
/// </summary>

public class At_Buttons : MonoBehaviour {
	public Sprite clicked;
	private bool mouseover=false;
	private Sprite aux;
	private Master_Instance masterPoint;
	//Arrow improvable properties
	private float s_timer = 0.6f;           // Ratio, time between shots
	private int accuracy_mode = 1;          // Bassically it is used by the bullet, and add a force in direction to the target.
	private int damage = 4;                 //Upgrade damage, to damage with fire 3 -> 4
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
			if(masterPoint.countMoney()<masterPoint.getPrice(this.gameObject)){master.isHide(this.gameObject);}                 //Disable button if no money
			aux = GetComponent<SpriteRenderer>().sprite;
			master.setLayer("interface",this.gameObject);
		}
	}
    /// <summary>
    /// If there is not enough money, Disable the button
    /// </summary>
	void Update () {
		if(this.gameObject.GetComponent<SpriteRenderer>().material.color.a!=0.5f){                                              //It is not disabled...
			if (Input.GetMouseButtonDown(0)&&mouseover==true){
				GetComponent<SpriteRenderer>().sprite = clicked;
				masterPoint.anybuttonclicked=true;
			}
			if (Input.GetMouseButtonUp(0)&&GetComponent<SpriteRenderer>().sprite==clicked){
                GameObject.Find("UI").GetComponent<AudioSource>().Play();
                master.showHand(false);
				GetComponent<SpriteRenderer>().sprite = aux;
				masterPoint.removeMoney(masterPoint.getPrice(this.gameObject));                                                 //Remove Money
				action();                                                                                                       //Action
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
			masterPoint.addMoney((int)(masterPoint.getPrice(this.gameObject.transform.parent.transform.parent.gameObject)/3)*2);
			master.sellTower(this.gameObject.transform.parent.transform.parent.gameObject);
		}
		if(this.gameObject.name=="Fire"){
			this.gameObject.transform.parent.transform.parent.GetComponent<AT_Controller>().fire=true;
			this.gameObject.transform.parent.transform.parent.GetComponent<AT_Controller>().Damage_ = damage;
		}
		if(this.gameObject.name=="Ratio"){
			this.gameObject.transform.parent.transform.parent.GetComponent<AT_Controller>().s_timer=s_timer;
		}
		if(this.gameObject.name=="Accuracy"){
			this.gameObject.transform.parent.transform.parent.GetComponent<AT_Controller>().accuracy_mode=accuracy_mode;
			this.gameObject.transform.parent.transform.parent.GetComponent<AT_Controller>().Damage_ =this.gameObject.transform.parent.transform.parent.GetComponent<AT_Controller>().Damage_ + 1;
		}
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
}
