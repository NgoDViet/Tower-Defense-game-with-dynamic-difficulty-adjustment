using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using FThLib;

/// <summary>
/// It is used by the patrol instantiated by the button at left/bottom of the screen
/// It controls the soldiers properties
/// </summary>
public class MiniKT_Controller : MonoBehaviour {
	public List<GameObject> enemies;                        //Enemies detected
	public Sprite lvl2;
	private int towerlvl = 0;
	private float instancetime = 11f;                       //Respawn time
	private GameObject flag=null;
	private int damage = 2;
	private int life = 20;
	private int a=0;
	//About knights
	public bool shield =false;
	// Use this for initialization

	void Start () {
		//master = GameObject.Find("Instance_Point").GetComponent<Master_Instance>();
		master.setLayer("tower",this.gameObject);
		Init_();
		setKnights();
	}
    /// <summary>
    /// Set knights patrol properties
    /// </summary>
	private void setKnights(){
		master.getChildFrom("Knight1",this.gameObject).GetComponent<Knights_Controller>().life=life;
		master.getChildFrom("Knight1",this.gameObject).GetComponent<Knights_Controller>().damage=damage;
		master.getChildFrom("Knight2",this.gameObject).GetComponent<Knights_Controller>().life=life;
		master.getChildFrom("Knight2",this.gameObject).GetComponent<Knights_Controller>().damage=damage;
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

	// Update is called once per frame
	void Update () {
		if(!master.isFinish()){
			if(master.getChildFrom("Knight1",this.gameObject)==null&&master.getChildFrom("Knight2",this.gameObject)==null){
				Destroy (this.gameObject);
			}else{
				remove_null();
				if(enemies.Count>0){getEnemy();}                                                                                    //If enemy on area and no fighting, call a knight
			}
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
		if(k1 == true){
			aux = master.getChildFrom ("Knight1",this.gameObject);
		}else{
			if(k2==true){
				aux = master.getChildFrom ("Knight2",this.gameObject);
			}
		}
		return aux;
	}
    /// <summary>
    /// Get an 'no fighting enemy' and search one 'no fighting knight'
    /// </summary>
	void getEnemy(){
		for(int i=0; i<enemies.Count ;i++){
			if(enemies[i]!=null){
				PathFollower enemyProperties = enemies[i].GetComponent<PathFollower>();
				if (enemyProperties.fighting==false){                                                                   //This enemy is not fighting
					enemyProperties.target=getKnight(enemies[i]);                                                       //Now the target of the enemy = 'no fighting knight'
                    if (enemyProperties.target!=null){                                                                  //This enemy has target?
						enemyProperties.fighting=true;                                                                  //Then this enemy is fighting
					}else{
						enemyProperties.fighting=false;                                                                 //This enemy is not fighting
                    }
				}
			}
		}
	}

	private void Init_(){
		flag = master.getChildFrom("flag",this.gameObject);
	}
    /// <summary>
    /// Set Z relative to the Y position
    /// </summary>
    /// <param name="go">gameobject</param>
    /// <param name="delayTime"></param>
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
	
}
