using UnityEngine;
using System.Collections;
using FThLib;

/// <summary>
/// It is used by the knights instantiated by the Knights tower
/// Reduce enemy life
/// It controls knights life, damage, Shield, Enemy target and flag point
/// Knights animations controller
/// Knight sounds
/// </summary>

public class Knights_Controller : MonoBehaviour {
	public int life=0;                                                          //Life, it is configured when is instantiated by KT_Controller attached to the Knights tower
	public int damage=3;                                                        //Damage, it is configured when is instantiated by KT_Controller attached to the Knights tower
    public GameObject flag=null;
	public GameObject target=null;                                              //Enemy target, it is configured by KT_Controller.cs (bool knightCanFight(string name, GameObject target) function)
    public bool fighting=false;
	public bool move=false;
	public bool faceright = true;
	public bool shield = false;
	private Animator anim;
	private float point=0f;
	private GameObject lifebar = null;
	private bool isActive=false;
	private bool Attack = false;
	private float delay = 3f;
	private int auxlife=0;
	//About healing
	private bool healing = false;
	private float healingdelay = 2f;                                            //Change it for fast Life regeneration
	private int healingvalue = 1;
	private Vector3 auxbar = new Vector3 (0,0,0);
    //About sounds
    private AudioSource audio;                                                  //Sounds are configured on AudioManager Gameobject / Audio_Manager.cs
    private AudioClip[] attack_clips;
    private AudioClip[] dying;
	// Use this for initialization
	void Start () {
        audio = this.gameObject.AddComponent<AudioSource>();
        audio.volume = master.getEffectsVolume();
        attack_clips = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().Physic_attack;
        dying = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().Knights_Dying;
        Init();
	}

	private void Init(){
		Invoke("activation",2f);
		master.setLayer("tower",this.gameObject);
		lifebar = master.getChildFrom("Lifebar", this.gameObject);
		auxbar = lifebar.transform.localScale;
		anim = this.gameObject.GetComponent<Animator> ();
		anim.SetBool ("walk", false);
		anim.SetBool ("dead", false);
		anim.SetBool ("attack", false);
	}

	// Update is called once per frame
	void Update () {
		if(!master.isFinish()){
			this.transform.position=new Vector3(this.transform.position.x,this.transform.position.y,this.transform.position.y);
			if(shield==true){anim.SetLayerWeight(1, 1);}                                                        //Change the animation layer if has shield
			if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Attack")) {anim.SetBool ("attack", false);}
			if(life!=0&&auxlife==0){getPoint();}
			if(point!=0f&&auxlife==0){auxlife = life;}
			if(isActive==true){
				Vector3 customPos = master.getChildFrom(this.gameObject.name + "p", flag).transform.position;   //On flag each knight has him position, example: Knight1 position on flag is -> KT0/flag/Knight1p gameobject
				if(fighting==false){                                                                            //No fighting, go to him position under flag
					Vector2 patchPos = new Vector2 (this.transform.position.x,this.transform.position.y);
					Vector2 patchCustomPos = new Vector2 (customPos.x,customPos.y);
					if(patchPos!=patchCustomPos){
						needFlip(customPos);
						transform.position = Vector2.MoveTowards(patchPos, patchCustomPos, Time.deltaTime/3);
						this.transform.position=new Vector3(this.transform.position.x,this.transform.position.y,this.transform.position.y);
					}else{
						anim.SetBool ("walk", false);
					}
				}
                //########################[Fighting]########################//
                if (fighting==true){                                                                            //This knight is fighting
                    if (target != null)
                    {                                                                       //By default the knight fighting place is at right of enemy
                        PathFollower enemyProperties = target.GetComponent<PathFollower>();
                        enemyProperties.fighting = true;
                        GameObject rightp = master.getChildFrom("RightPoint", target);
                        GameObject leftp = master.getChildFrom("LeftPoint", target);
                        if (enemyProperties.faceright == true)
                        {                                                //go to right point
                            Vector2 patchPos = new Vector2(this.transform.position.x, this.transform.position.y);
                            Vector2 patchCustomPos_ = new Vector2(rightp.transform.position.x, rightp.transform.position.y);
                            if (patchPos != patchCustomPos_)
                            {
                                needFlip(rightp.transform.position);
                                transform.position = Vector2.MoveTowards(patchPos, patchCustomPos_, Time.deltaTime / 3);
                                this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.y);
                            }
                            else
                            {
                                needFlip(target.transform.position);
                                anim.SetBool("walk", false);
                                move = false;
                            }
                            if (move == false && Attack == false)
                            {
                                anim.SetBool("attack", true);
                                audio.clip = attack_clips[Random.Range(0, attack_clips.Length - 1)];
                                audio.Play();                                                                   //Play sound attack
                                Attack = true;
                                Invoke("enemyreduceLife", 0.1f);
                                Invoke("attack_delay", delay);
                            }
                        }
                        else
                        {
                            if (transform.position != leftp.transform.position)
                            {
                            }
                            else
                            {
                                move = false;
                            }
                        }
                    }
                    else{
							fighting=false;
							move=false;
							Attack=false;
						}
				}else{                                                                                          //If no fighting, regenerate life
					if(life<auxlife&&healing==false){
						healing=true;
						Invoke("increaseLife",healingdelay);
					}
				}
			}
		}
	}
    /// <summary>
    /// Reset life when respawn
    /// </summary>
    /// <param name="newlife">Value</param>
	public void resetLife(int newlife){
		lifebar.transform.localScale = auxbar;
		auxlife = newlife;
		life = newlife;
		getPoint();
	}
    /// <summary>
    /// It is used to manage the progressbar
    /// </summary>
	void getPoint(){
		float aux = lifebar.transform.localScale.x;
		point = aux/life;
	}
    /// <summary>
    /// Life regeneration
    /// </summary>
    /// <param name="value">Value</param>
	public void increaseLife(){
		life=life+healingvalue;
		float aux = healingvalue * point;
		Vector3 aux_ = lifebar.transform.localScale;
		aux_.x = aux_.x + aux;
		lifebar.transform.localScale = aux_;
		healing = false;
	}
    /// <summary>
    /// Reduce life and create blood, reduce this enemy life bar
    /// Destroy this enemy when life = 0
    /// </summary>
    /// <param name="value">Damage value</param>
	public void reduceLife(int value){
		GameObject blood = Instantiate(Resources.Load("Global/blood"), this.transform.position, Quaternion.identity)as GameObject;
		life=life-value;
		float aux = value * point;
		Vector3 aux_ = lifebar.transform.localScale;
		aux_.x = aux_.x - aux;
		if(aux_.x<0){
			aux_.x=0;
			lifebar.transform.localScale = aux_;
			anim.SetBool("dead",true);
            audio.clip = dying[Random.Range(0, dying.Length - 1)];
            audio.Play();                                                                   //Play sound
            Destroy (master.getChildFrom("Shadow",this.gameObject));
			isActive=false;
			Invoke("onDestroy",1.5f);
		}else{
			lifebar.transform.localScale = aux_;
		}
	}
    /// <summary>
    /// Reduce Enemy life
    /// </summary>
	private void enemyreduceLife(){
		if(target!=null){
			Enemies_Controller properties = target.GetComponent<Enemies_Controller>();
			properties.reduceLife(damage);
		}
	}
    /// <summary>
    /// Delay when attack
    /// </summary>
	private void attack_delay(){Attack=false;}
	void activation(){isActive=true;}
	void onDestroy(){Destroy (this.gameObject);}
    /// <summary>
    /// Determine if flip is necessary
    /// </summary>
    /// <param name="customPos"></param>
	void needFlip(Vector3 customPos){
		if(customPos.x>=this.transform.position.x&&faceright==false){Flip();}
		if(customPos.x<this.transform.position.x&&faceright==true){Flip();}
		anim.SetBool ("walk", true);
	}
    /// <summary>
    /// Flip the character
    /// </summary>
	void Flip(){
		faceright=!faceright;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
