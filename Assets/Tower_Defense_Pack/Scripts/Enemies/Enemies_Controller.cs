using UnityEngine;
using System.Collections;
using FThLib;

/// <summary>
/// All enemies use this script
/// It controls... attack, life, animations, get damage and show blood when arrow
/// This script does not control the path
/// </summary>
public class Enemies_Controller : MonoBehaviour {
	public int life=0;
	public float attackDelay = 2f;                              //Delay between attacks
	public int moneyWhenKill = 20;                              //This is the money added when this enemy is destroyed
	public string type = "";
	private Master_Instance masterPoint;
	private float point=0f;                                     //It is used to manage the life progress bar, its value is = lifebar.transform.localScale.x/life
    private GameObject lifebar = null;
	private bool Attack = false;
	private PathFollower properties_;
	private int damage=3;                                       //Create damage when attack
	private int auxlife=0;
	private Animator anim;
	private bool off=false;
    //About sounds
    private AudioSource audio;
    private AudioClip[] attack_clips;
    private AudioClip[] dying;
    //About hero attacking
    public bool isHero = false;                                 //About attacking to a hero
    private bool heroaux = false;
    private int value = 1;                                      //It is used in attacking to hero
    private GameObject Herogo;                                  //It is used in attacking to hero
    // Use this for initialization
    void Start () {
        audio = this.gameObject.AddComponent<AudioSource>();
        audio.volume = master.getEffectsVolume();
        attack_clips = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().Physic_attack;
        dying = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().Enemy_Dying;
        this.gameObject.tag="Respawn";
		Init();
	}
    /// <summary>
    /// Set Enemy properties, layer, animations
    /// </summary>
	private void Init(){
		masterPoint = GameObject.Find("Instance_Point").GetComponent<Master_Instance>();
		master.setLayer("enemies",this.gameObject);
		lifebar = master.getChildFrom("Lifebar",this.gameObject);
		getPoint();
		properties_ = GetComponent<PathFollower>();
		anim = this.gameObject.GetComponent<Animator> ();
	}
    /// <summary>
    /// Atack to Hero
    /// </summary>
    /// <returns></returns>
    IEnumerator AttackHero()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Attack") == true) { value = 1; } 
        yield return new WaitForSeconds(value);
        anim.SetBool("walk", false);
        heroaux = !anim.GetCurrentAnimatorStateInfo(0).IsName("Attack");
        if (heroaux == true) {
            audio.clip = attack_clips[Random.Range(0, attack_clips.Length - 1)];
            audio.Play();
            if (Herogo != null) { Herogo.SendMessage("ReduceLife", damage);}
        }
        anim.SetBool("attack", heroaux);
        value = 1;
        if (isHero == true) {
            yield return StartCoroutine(AttackHero());
        }
    }
    void OnTriggerEnter2D(Collider2D other) {
        if (other.name == "Hero"&& type != "enemy3" &&isHero==false&& properties_.target==null) {
            Herogo = other.gameObject;
            isHero = true;
            this.gameObject.SendMessage("setFightHero",other.gameObject);
            anim.SetBool("walk", false);
            anim.SetBool("dead", false);
            anim.SetBool("attack", false);
            StartCoroutine(AttackHero());
        }
		if(other.name=="Arrow"){reduceLife(other.GetComponent<Damage>().Damage_);}                                                              //Reduce life by arrow
		if(other.name=="Magic"){                                                                                                                //Reduce life by Magician
			GameObject blood = Instantiate(Resources.Load("Global/blood"), other.transform.position, Quaternion.identity)as GameObject;
			reduceLife(other.GetComponent<MT_Bullet>().Damage_);
		}
		//IF THIS ENEMY IS A SORCERER ======================
		if(type=="enemy2"&&other.name=="zone"){           
			if(getChild(other.transform.root.gameObject)==null){                                                                                //There is not a rune in this tower
				anim.SetBool ("walk", false);
				anim.SetBool ("dead", false);
				anim.SetBool ("attack", false);
				other.transform.parent.SendMessage("Turn_Off");
				this.gameObject.SendMessage("Turn_Off");
				off=true;
				Invoke("Turn_On",masterPoint.Sorcerer_Runes_Time);
				CreateRunes(other.gameObject.transform.parent.gameObject);  //Instantiate runes
            }
		}
	}
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.name == "Hero" && isHero == true)
        {
            StopAttackingHero();
        }
    }
    /// <summary>
    /// It is only used by the sorcerer, when off=true the sorcerer stops and create runes
    /// </summary>
    private void Turn_On(){off=false;}
    /// <summary>
    /// It is only used by the sorcerer to create Runes
    /// </summary>
    /// <param name="go"></param>
	private void CreateRunes(GameObject go){
		GameObject runes = Instantiate(Resources.Load("Enemies/StopTime"), new Vector3(go.transform.position.x,go.transform.position.y-0.3f,go.transform.position.z-1f), Quaternion.identity)as GameObject;
		runes.name="Runes";
		runes.transform.parent = go.transform;
	}
    /// <summary>
    /// Stop attacking hero
    /// </summary>
    private void StopAttackingHero() {
        isHero = false;
        this.gameObject.SendMessage("ByeHero");
    }
	// Update is called once per frame
	void Update () {
        if (!master.isFinish())
        {
            if (point != 0f && auxlife == 0) { auxlife = life; }
            if (type != "enemy3")                                                                                               //This unit is not an AirUnit, (Air units doesnt attack)
            {
                if (isHero == true)
                {
                    if (Herogo != null)
                    {
                        this.gameObject.SendMessage("needFlip", GameObject.Find("Hero").transform.position);
                    }
                    else {
                        StopAttackingHero();
                    }
                }
                else
                {
                    if (anim.GetCurrentAnimatorStateInfo(0).IsName("Attack")) { anim.SetBool("attack", false); }
                    if (properties_.auxfight == true)
                    {                                                                                                               //has Target?
                        anim.SetBool("walk", false);
                        if (properties_.target != null)
                        {                                                                                                           //Target != Null
                            Knights_Controller knight = properties_.target.GetComponent<Knights_Controller>();
                            if (knight.move == false && Attack == false)
                            {                                                                                                       //The target is placed, then attack
                                Attack = true;
                                anim.SetBool("attack", true);
                                audio.clip = attack_clips[Random.RandomRange(0, attack_clips.Length - 1)];
                                audio.Play();                                                                                       //Play sound
                                Invoke("enemyreduceLife", damage);
                                Invoke("attack_delay", attackDelay);                                                                //Delay between attacks
                            }
                        }
                    }
                    else
                    {
                        if (off == false)
                        {
                            anim.SetBool("walk", true);
                        }
                    }
                }                                                                                                                  
            }
        }
    }
    /// <summary>
    /// Delay time in attack
    /// </summary>
	private void attack_delay(){Attack=false;}
    /// <summary>
    /// Add life
    /// </summary>
    /// <param name="value">Value</param>
	public void increaseLife(int value){
		float aux = value * point;
		Vector3 aux_ = lifebar.transform.localScale;
		aux_.x = aux_.x + aux;
		lifebar.transform.localScale = aux_;
	}
    /// <summary>
    /// Reduce life and create blood, reduce this enemy life bar
    /// Destroy this enemy when life = 0
    /// </summary>
    /// <param name="value">Damage value</param>
	public void reduceLife(int value){
		GameObject blood = Instantiate(Resources.Load("Global/blood"), this.transform.position, Quaternion.identity)as GameObject;
		float aux = value * point;
		life = life - value;
		Vector3 aux_ = lifebar.transform.localScale;
		aux_.x = aux_.x - aux;		
		if(aux_.x<0){                                                                                                           //Life<0
			if(type=="enemy3"){                                                                                                 //IF (enemy3), DYING ANIMATION
				Invoke("DelayedDying",0.5f);
				anim.SetBool("dead",true);
                this.gameObject.SendMessage("setSpeed0");               
                audio.clip = dying[Random.Range(0, dying.Length - 1)];
                audio.Play();                                                                   //Play sound
            }
            else{
                anim.SetBool("dead", true);
                this.gameObject.SendMessage("setSpeed0");
                audio.clip = dying[Random.Range(0, dying.Length - 1)];
                audio.Play();
                Invoke("DelayedDying", 0.7f);
                //Destroy(this.gameObject);
			}
			masterPoint.addMoney(moneyWhenKill);                                                                                //Add money
		}else{
			lifebar.transform.localScale = aux_;                                                                                //Reduce Progressbar
		}
	}
    /// <summary>
    /// Reduce life to the target, right now only can reduce life to the knights
    /// </summary>
	private void enemyreduceLife(){
		if(properties_.target!=null&& properties_.target.name !="Hero")
        {
			Knights_Controller properties = properties_.target.GetComponent<Knights_Controller>();
			properties.reduceLife(damage);
		}
	}
    /// <summary>
    /// It is used to manage the progressbar
    /// </summary>
	void getPoint(){
		float aux = lifebar.transform.localScale.x;
		point = aux/life;
	}
    /// <summary>
    /// Delay for dying
    /// </summary>
	private void DelayedDying(){
		Destroy(this.gameObject);
	}
    /// <summary>
    /// Get child gameobject
    /// </summary>
    /// <param name="go">name</param>
    /// <returns>Child</returns>
	GameObject getChild(GameObject go){
		GameObject aux=null;
		foreach(Transform child in go.transform){if(child.name=="Runes"){aux=child.gameObject;}}
		return aux;
	}
  
}
