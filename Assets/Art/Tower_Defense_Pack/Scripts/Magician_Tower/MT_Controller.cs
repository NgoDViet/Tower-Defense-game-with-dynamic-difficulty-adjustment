using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using FThLib;

/// <summary>
/// Magician Tower controller
/// Mage animations controller
/// Create Mage Bullet
/// </summary>
public class MT_Controller : MonoBehaviour {
	public List<GameObject> enemies;                                //Enemies detected on zone
	public Sprite block;
	private GameObject mage=null;
	private bool faceright = false;
	private Animator anim;
	private GameObject zone=null;                                   //Child gameobject used to detect enemies
	private GameObject spawner=null;                                //Bullet instance point
	private GameObject spawner1=null;                               //Bullet instance point
    private GameObject spawner2=null;                               //Bullet instance point
    private bool shot_ = false;
	private bool mouseover=false;
	public float s_timer = 0.9f;
	public int Damage_ = 1;
	public bool fire = false;
	public bool ice = false;
	public bool trap = false;
	private bool off=false;		                                    //Sorcerer can turn off
    private AudioSource audio;
    private AudioClip[] magic;
    //Show hand
    void OnMouseOver(){ 
		if(!GameObject.Find("hand")){master.showHand (true);}
		mouseover=true;
	}
	//Hide hand
	void OnMouseExit(){
		if(GameObject.Find("hand")){master.showHand (false);}
		mouseover=false;
	}
	// Use this for initialization
	void Start () {
        audio = this.gameObject.AddComponent<AudioSource>();
        magic = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().Arrows;
        Init ();
	}

	private void Init(){
		this.transform.position = master.setThisZ(this.transform.position,0.02f);
		spawner = master.getChildFrom("spawner",this.gameObject);
		spawner1 = master.getChildFrom("spawner1",this.gameObject);
		spawner2 = master.getChildFrom("spawner2",this.gameObject);
		zone = master.getChildFrom("zone",this.gameObject);
		master.setLayer("tower",this.gameObject);
		//About mage in tower
		mage = master.getChildFrom("Mage",this.gameObject);
		anim = mage.GetComponent<Animator> ();
		anim.SetBool ("attack", false);
	}
	
	// Update is called once per frame
	void Update () {
		if(!master.isFinish()){
			if(master.getChildFrom("Interface",this.gameObject)==null&&!GameObject.Find("circle")){                                 //Show the green circle are when Interface of this tower is enabled
				master.getChildFrom("zoneImg",this.gameObject).GetComponent<SpriteRenderer>().enabled=false;
				GetComponent<CircleCollider2D>().enabled=true;
			}
			if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Attack")) {anim.SetBool ("attack", false);}
			if (Input.GetMouseButtonDown(0)&&mouseover==true){                                                                      //Show interface of this tower when click
				master.showInterface(this.gameObject.name,this.gameObject,zone.transform);
				GetComponent<CircleCollider2D>().enabled=false;
				master.getChildFrom("zoneImg",this.gameObject).GetComponent<SpriteRenderer>().enabled=true;
			}
			remove_null();
			if(enemies.Count>0){
				if(shot_==false){
					shot_=true;
					Invoke("shot",s_timer);
				}
			}
		}
	}
    /// <summary>
    /// It is used by Sorcerer Enemey
    /// </summary>
	public void Turn_Off(){
		off=true;
		Invoke("Turn_On",GameObject.Find("Instance_Point").GetComponent<Master_Instance>().Sorcerer_Runes_Time);
	}
    /// <summary>
    /// Re enable tower after Turn_off
    /// </summary>
	private void Turn_On(){
		off=false;
	}
    /// <summary>
    /// 1º Step of bullet creation process by mage
    /// </summary>
	private void shot(){
		shot_=false;
		if(off==false){
			if(enemies.Count>0&&enemies[enemies.Count-1]!=null){
				if(enemies[enemies.Count-1].transform.position.x < this.transform.position.x && faceright == true){             //Enemy is on left and mage looks to right
					Flip();
				}
				if(enemies[enemies.Count-1].transform.position.x >= this.transform.position.x && faceright == false){           //Enemy is on Right and mage looks to left
					Flip();
				}
				anim.SetBool ("attack", true);
				Instantiate_Bullet(spawner, "Magic");
			}
		}
	}
    /// <summary>
    /// Instantiate bullet with delay
    /// </summary>
	private void InstantateWithDelay(){
		Instantiate_Bullet(spawner, "Magic");
	}
    /// <summary>
    /// 2º Step of bullet creation process
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="name"></param>
	private void Instantiate_Bullet(GameObject pos, string name){
		GameObject Bullet = Instantiate(Resources.Load("MT/Mfire"), pos.transform.position, Quaternion.identity)as GameObject;
		MT_Bullet BulletProperties = Bullet.GetComponent<MT_Bullet>();
        //############# Bullet properties #############//
        BulletProperties.target = enemies[enemies.Count-1];         //Set the target to the bullet
		BulletProperties.fire = fire;                               //Fire = true create fire
		BulletProperties.ice = ice;                                 //Not used
		BulletProperties.Damage_ = Damage_;                         //Set damage
		Bullet.name=name;
	}
    /// <summary>
    /// Add enemy to the enemy list
    /// It is called by the 'zone' child Gameobject of the tower / Zone_Controller.cs script attached
    /// </summary>
    /// <param name="other">Enemy gameobject</param>
    public void enemyAdd(GameObject other){enemies.Add (other);}
    /// <summary>
    /// Remove enemies from the enemy list
    /// </summary>
    /// <param name="other">Enemy name</param>
	public void enemyRemove(string other){
		for(int i=0; i<enemies.Count ;i++){
			if(enemies[i]!=null){
				if(enemies[i].name==other){enemies.RemoveAt(i);}
			}
		}
	}
    /// <summary>
    /// Remove nulls from the enemy list
    /// </summary>
	void remove_null(){for(int i=0; i<enemies.Count ;i++){if(enemies[i]==null){enemies.RemoveAt(i);}}}
    /// <summary>
    /// Flip the mage character in the tower
    /// </summary>
	void Flip(){
		faceright=!faceright;
		Vector3 theScale = mage.transform.localScale;
		theScale.x *= -1;
		mage.transform.localScale = theScale;
	}
}
