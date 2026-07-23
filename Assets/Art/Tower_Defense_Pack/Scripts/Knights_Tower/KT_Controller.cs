using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using FThLib;

/// <summary>
/// Here you can set the life and damage of the Knights
/// Respawn Knights
/// Knights tower controller, it is used by the knights tower
/// It controls the soldiers properties
/// Door animation
/// Sorcerer enemy can turn off this tower
/// Play Door Sound
/// </summary>
public class KT_Controller : MonoBehaviour {
	public List<GameObject> enemies;                                //List of enemies detected on zone
	public Sprite lvl2;
	public Sprite block;
	private int towerlvl = 0;
    private float instancetime = 11f;                               //Time to instance one knigth
	private GameObject door=null;                                   //Door Gameobject
	private GameObject opened=null;                                 //Point when door opened
	private GameObject closed=null;                                 //Point when door closed
	private GameObject spawner=null;                                //Point to instantiate the knights
	private GameObject flag=null;                                   //Flag point
	private GameObject zone=null;                                   //Detection zone
	private bool mouseover=false;
	//--About door
	private bool opening=false;
	private bool closing=false;
	private bool inprocess=false;
	private bool firstime=true;
	private int a=0;
	//About knights
	public int damage = 3;
	public int life = 20;                                           //Life of the instantiated knights
	public bool shield =false;
	private bool off=false;                                         //Sorcerer can turn off
    private AudioSource audio;
    private AudioClip[] door_clips;
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
    /// <summary>
    /// When knight instantiated set Z relative to the Y position
    /// </summary>
    /// <param name="coll"></param>
	void OnTriggerEnter2D(Collider2D coll) {
		if(coll.name=="Knight1"||coll.name=="Knight2"||coll.name=="Knight3"){
			StartCoroutine(setZ(coll.gameObject, 1f));
		}
	}
    /// <summary>
    /// Get child gameobjects
    /// </summary>
	private void Init_(){
        audio = this.gameObject.AddComponent<AudioSource>();
        audio.volume = 1;
        door_clips = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().KnightsDoor;
        door = master.getChildFrom("door",this.gameObject);                           
		opened = master.getChildFrom("opened",this.gameObject);
		closed = master.getChildFrom("closed",this.gameObject);
		spawner = master.getChildFrom("spawner",this.gameObject);
		flag = master.getChildFrom("flag",this.gameObject);
		zone = master.getChildFrom("zone",this.gameObject);
	}
    /// <summary>
    /// Set the Z to the tower relative to Y position
    /// </summary>
	void Start () {
		this.transform.position = master.setThisZ(this.transform.position,0.02f);
		master.setLayer("tower",this.gameObject);
		setFlagZ();
		Init_();
	}

	void Update () {
		if(!master.isFinish()){
			if(master.getChildFrom("Interface",this.gameObject)==null&&!GameObject.Find("circle")){                             //Show the green circle are when Interface of this tower is enabled
				master.getChildFrom("zoneImg",this.gameObject).GetComponent<SpriteRenderer>().enabled=false;
				GetComponent<CircleCollider2D>().enabled=true;
			}
			if (Input.GetMouseButtonDown(0)&&mouseover==true){                                                                  //Show interface of this tower when click
				master.showInterface(this.gameObject.name,this.gameObject,zone.transform);
				GetComponent<CircleCollider2D>().enabled=false;
				master.getChildFrom("zoneImg",this.gameObject).GetComponent<SpriteRenderer>().enabled=true;
			}
			if(inprocess==false){knightCall();}                                                                                 //progressbar included
			doorisdoing();
			remove_null();
			if(enemies.Count>0){getEnemy();}                                                                                    //If enemy on area and no fighting, call a knight
		}
	}
    /// <summary>
    /// It is used by Sorcerer Enemey
    /// </summary>
	public void Turn_Off(){
		off=true;		
		foreach(Transform child in gameObject.transform){if(child.name=="ProgressBar"){Destroy(child.gameObject);} }            //ProgressBar
        CancelInvoke("Instantiate_Knight");
		inprocess=false;
		Invoke("Turn_On",GameObject.Find("Instance_Point").GetComponent<Master_Instance>().Sorcerer_Runes_Time);
	}
    /// <summary>
    /// Re eneable the tower after Turn_off by the sorcerer enemy
    /// </summary>
	private void Turn_On(){
		off=false;
	}
    /// <summary>
    /// Get an 'no fighting enemy' and search one 'no fighting knight'
    /// </summary>
	void getEnemy(){
		for(int i=0; i<enemies.Count ;i++){
			if(enemies[i]!=null){
				PathFollower enemyProperties = enemies[i].GetComponent<PathFollower>();
				if (enemyProperties.fighting==false){                                                                           //This enemy is not fighting
					enemyProperties.target=getKnight(enemies[i]);                                                               //Now the target of the enemy = 'no fighting knight'
					if(enemyProperties.target!=null){                                                                           //This enemy has target?
						enemyProperties.fighting=true;                                                                          //Then this enemy is fighting
					}else{
						enemyProperties.fighting=false;                                                                         //This enemy is not fighting
					}
				}
			}
		}
	}
    /// <summary>
    /// Search one knight for one enemy
    /// </summary>
    /// <param name="target">Enemy</param>
    /// <returns></returns>
	GameObject getKnight(GameObject target){//Knight stoped and no fighting
		GameObject aux = null;
		Knights_Controller k1properties;
		Knights_Controller k2properties;
		Knights_Controller k3properties;
		bool k1 =false;
		bool k2 =false;
		bool k3 =false;
		if(master.getChildFrom ("Knight1",this.gameObject)!=null){
			k1properties = master.getChildFrom("Knight1",this.gameObject).GetComponent<Knights_Controller>();
			k1 = knightCanFight("Knight1", target);
		}
		if(master.getChildFrom ("Knight2",this.gameObject)!=null&&k1==false){
			k2properties = master.getChildFrom("Knight2",this.gameObject).GetComponent<Knights_Controller>();
			k2 = knightCanFight("Knight2", target);
		}
		if(master.getChildFrom ("Knight3",this.gameObject)!=null&&k1==false&&k2==false){
			k3properties = master.getChildFrom("Knight3",this.gameObject).GetComponent<Knights_Controller>();
			k3 = knightCanFight("Knight3", target);
		}
		if(k1 == true){
			aux = master.getChildFrom ("Knight1",this.gameObject);
		}else{
			if(k2==true){
				aux = master.getChildFrom ("Knight2",this.gameObject);
			}else{
				if(k3==true){aux = master.getChildFrom ("Knight3",this.gameObject);}
			}
		}
		return aux;
	}

    /// <summary>
    /// Set knights patrol properties
    /// </summary>
    public void setDamage(){
		if(master.getChildFrom("Knight1",this.gameObject)){
			master.getChildFrom("Knight1",this.gameObject).GetComponent<Knights_Controller>().damage=damage;
		}
		if(master.getChildFrom("Knight2",this.gameObject)){
			master.getChildFrom("Knight2",this.gameObject).GetComponent<Knights_Controller>().damage=damage;
		}
		if(master.getChildFrom("Knight3",this.gameObject)){
			master.getChildFrom("Knight3",this.gameObject).GetComponent<Knights_Controller>().damage=damage;
		}
	}
    /// <summary>
    /// Set knights patrol properties
    /// </summary>
    public void setShield(){
		shield = true;
		if(master.getChildFrom("Knight1",this.gameObject)){
			master.getChildFrom("Knight1",this.gameObject).GetComponent<Knights_Controller>().shield=true;
			master.getChildFrom("Knight1",this.gameObject).GetComponent<Knights_Controller>().resetLife(life);
		}
		if(master.getChildFrom("Knight2",this.gameObject)){
			master.getChildFrom("Knight2",this.gameObject).GetComponent<Knights_Controller>().shield=true;
			master.getChildFrom("Knight2",this.gameObject).GetComponent<Knights_Controller>().resetLife(life);
		}
		if(master.getChildFrom("Knight3",this.gameObject)){
			master.getChildFrom("Knight3",this.gameObject).GetComponent<Knights_Controller>().shield=true;
			master.getChildFrom("Knight3",this.gameObject).GetComponent<Knights_Controller>().resetLife(life);
		}
	}
    /// <summary>
    /// All knights stop fighting, it is called when flag changes its position
    /// </summary>
	public void Reset(){
		master.getChildFrom("TargetedZone",this.gameObject).transform.position = flag.transform.position;
        GetComponent<AudioSource>().Play();
		if(enemies.Count>0){
			for(int i=0; i<enemies.Count ;i++){
				PathFollower enemyProperties = enemies[i].GetComponent<PathFollower>();
				if (enemyProperties.fighting==true){
					enemyProperties.target=null;
					enemyProperties.fighting=false;
				}
				enemyRemove(enemies[i].name);
			}
		}
		if(master.getChildFrom("Knight1",this.gameObject)){
			Knights_Controller properties = master.getChildFrom("Knight1",this.gameObject).GetComponent<Knights_Controller>();
			if(properties.fighting==true&&properties.target!=null){
				properties.target.GetComponent<PathFollower>().fighting=false;
				properties.target.GetComponent<PathFollower>().target = null;
			}
			properties.fighting=false;
			properties.target=null;
		}
		if(master.getChildFrom("Knight2",this.gameObject)){
			Knights_Controller properties = master.getChildFrom("Knight2",this.gameObject).GetComponent<Knights_Controller>();
			if(properties.fighting==true&&properties.target!=null){
				properties.target.GetComponent<PathFollower>().fighting=false;
				properties.target.GetComponent<PathFollower>().target = null;
			}
			properties.fighting=false;
			properties.target=null;
		}
		if(master.getChildFrom("Knight3",this.gameObject)){
			Knights_Controller properties = master.getChildFrom("Knight3",this.gameObject).GetComponent<Knights_Controller>();
			if(properties.fighting==true&&properties.target!=null){
				properties.target.GetComponent<PathFollower>().fighting=false;
				properties.target.GetComponent<PathFollower>().target = null;
			}
			properties.fighting=false;
			properties.target=null;
		}
	}

	/// <summary>
    /// Search one no fighting Knight, then set him target with an detected enemy
    /// </summary>
    /// <param name="name">Knight1, 2 ,3</param>
    /// <param name="target">Enemy to attack</param>
    /// <returns>true if it has one no fighting knight</returns>
	bool knightCanFight(string name, GameObject target){
		bool aux = false;
		Knights_Controller kproperties = master.getChildFrom(name,this.gameObject).GetComponent<Knights_Controller>();
		if(kproperties.fighting==false){
			kproperties.fighting=true;
			kproperties.target=target;
			kproperties.move=true;
			aux = true;
		}
		return aux;
	}
    /// <summary>
    /// It detects the first time respawn the knights, the first time is faster
    /// </summary>
	private void knightCall(){//Instantiate
		if(off==false){
			if(master.getChildFrom("Knight1",this.gameObject)&&master.getChildFrom("Knight2",this.gameObject)&&master.getChildFrom("Knight3",this.gameObject)){
				firstime=false;
			}else{
				if(firstime==true){                                                                 //First time respawn is better
					inprocess=true;
					master.Instantiate_Progressbar(4f,this.gameObject);
					Invoke ("Instantiate_Knight",4f);
				}else{
					inprocess=true;
					master.Instantiate_Progressbar(instancetime,this.gameObject);
					Invoke ("Instantiate_Knight",instancetime);
				}
			}
		}
	}
    /// <summary>
    /// Create one Knight and set properties
    /// </summary>
	private void Instantiate_Knight(){
		inprocess=false;
		if(off==false){
			GameObject Knight = Instantiate(Resources.Load("Kt/Knight"), new Vector3(spawner.transform.position.x,spawner.transform.position.y,spawner.transform.position.y), Quaternion.identity)as GameObject;
			Knight.transform.SetParent(this.gameObject.transform);
			opening = true;
			Knights_Controller KnightProperties = Knight.GetComponent<Knights_Controller>();
			KnightProperties.flag=flag;
			KnightProperties.life=life;
			KnightProperties.shield = shield;
			KnightProperties.damage=damage;
			Knight.name=getKnightName();
		}
	}
    /// <summary>
    /// Set the knight name depends of the created knights
    /// </summary>
    /// <returns></returns>
	private string getKnightName(){
		string aux_ = "";
		if(master.getChildFrom("Knight1",this.gameObject)){
			if(master.getChildFrom("Knight2",this.gameObject)){
				if(master.getChildFrom("Knight3",this.gameObject)){
				}else{
					aux_="Knight3";
				}
			}else{
				aux_="Knight2";
			}
		}else{
			aux_="Knight1";
		}
		return aux_;
	}
    /// <summary>
    /// Set Z after delay time
    /// </summary>
    /// <param name="go">knight</param>
    /// <param name="delayTime">time</param>
    /// <returns></returns>
	private IEnumerator  setZ(GameObject go, float delayTime){
		yield return new WaitForSeconds(delayTime);
		go.transform.position = new Vector3(go.transform.position.x,go.transform.position.y,0f);
	}
	/// <summary>
    /// Remove nulls from the enemy list
    /// </summary>
	void remove_null(){for(int i=0; i<enemies.Count ;i++){if(enemies[i]==null){enemies.RemoveAt(i);}}}
    /// <summary>
    /// Add enemy to the enemy list
    /// It is called by the 'zone' child Gameobject of the tower / Zone_Controller.cs script attached
    /// </summary>
    /// <param name="other">Enemy gameobject</param>
	public void enemyAdd(GameObject other){enemies.Add (other);}
    /// <summary>
    /// Remove enemy from the list
    /// </summary>
    /// <param name="other"></param>
	public void enemyRemove(string other){
		for(int i=0; i<enemies.Count ;i++){
			if(enemies[i]!=null){
				if(enemies[i].name==other){enemies.RemoveAt(i);}
			}
		}
	}
    //###############[About door]###############//
    private void doorisdoing(){
		if(opening==true){
			getOpen (0);
		}else{
			if(closing==true){getOpen(1);}
		}
	}	
	private void getOpen(int value){//0 open, 1 close
		switch(value){
		case 0:
			if(door.transform.position != opened.transform.position){
				door.transform.position = Vector3.MoveTowards(door.transform.position, opened.transform.position, Time.deltaTime/4);
                    audio.clip = door_clips[0];
                    audio.Play();                                                                   //Play sound attack
                }
                else{
				opening=false;
				Invoke("setclosing",2);
			}
			break;
		case 1:
			if(door.transform.position != closed.transform.position){
				door.transform.position = Vector3.MoveTowards(door.transform.position, closed.transform.position, Time.deltaTime/4);
                    //audio.clip = door_clips[0];
                    //audio.Play();                                                                   //Play sound attack
                }
                else{
				closing=false;
			}
			break;
		}
	}
	private void setFlagZ(){master.getChildFrom("flag",this.gameObject).transform.position=new Vector3(master.getChildFrom("flag",this.gameObject).transform.position.x,master.getChildFrom("flag",this.gameObject).transform.position.y,master.getChildFrom("flag",this.gameObject).transform.position.y+0.2f);}
	private void setclosing(){closing=true;}
    //###############[End About door]###############//
}
