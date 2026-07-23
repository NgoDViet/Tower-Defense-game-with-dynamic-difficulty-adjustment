using UnityEngine;
using System.Collections;
using FThLib;

/// <summary>
/// It is used by the trap created by the Magician Tower
/// Trap animations, damage and delay
/// Trap sound
/// </summary>
public class Trap_Controller : MonoBehaviour {
	public int duration = 13;
	public int damage = 1;
	private Animator anim;                      //Trap animation controller
	private bool attack=false;
	private float delay=2f;                     //Delay time between attacks
	private bool on=false;
    private AudioSource audio;                                                  //Sounds are configured on AudioManager Gameobject / Audio_Manager.cs
    private AudioClip trapclip;
    // Use this for initialization
    void Start () {
        audio = this.gameObject.AddComponent<AudioSource>();
        audio.volume = master.getEffectsVolume();
        trapclip = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().trap;
        audio.clip = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().PlaceTrap;
        audio.Play();
        this.gameObject.name="trap_";
		this.gameObject.transform.position=master.setThisZ(this.transform.position,0.00f);
		master.setLayer("tower",this.gameObject);
		setChild(attack);
		Invoke("onDestroy",duration);
	}

	void OnTriggerEnter2D(Collider2D other) {
		if(other.tag=="Respawn"&&other.GetComponent<Enemies_Controller>().type!="enemy3"){
			enemyreduceLife(other.gameObject);
			other.gameObject.GetComponent<PathFollower>().reduceSpeed();
		}
	}
	// Update is called once per frame
	void Update () {
		if(on==false){
			on=true;
			Invoke("setTrap",delay);
		}
	}
    /// <summary>
    /// Reduce enemy life
    /// </summary>
    /// <param name="target">Enemy gameobject</param>
	private void enemyreduceLife(GameObject target){
		if(target!=null){
			Enemies_Controller properties = target.GetComponent<Enemies_Controller>();
			properties.reduceLife(damage);
		}
	}
    /// <summary>
    /// Set animator var value
    /// </summary>
    /// <param name="value"></param>
	private void setChild(bool value){
        if (value == true) {
            audio.volume = 0.05f;
            audio.clip = trapclip;
            audio.Play();
        }
		foreach(Transform child in gameObject.transform){
			child.gameObject.GetComponent<Animator> ().SetBool ("attack", value);
		}
	}
    /// <summary>
    /// Enable / Disable damage
    /// </summary>
	void setTrap(){
		attack = !attack;
		setChild(attack);
		on=false;
		GetComponent<BoxCollider2D>().enabled=attack;
		Invoke ("disableTrigger",1f);
	}

	void disableTrigger(){
		GetComponent<BoxCollider2D>().enabled=false;
	}
	void onDestroy(){
		Destroy(this.gameObject);
	}
}
