using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using FThLib;

/// <summary>
/// This script is used by the enemies to read the path
/// Sounds when enemies reach the limit
/// Exit sound
/// </summary>
public class PathFollower : MonoBehaviour {
	public Transform[] path;	    //Path with points
	public float speed = 0f;
	public int currentPoint = 0;    //Path point index into path array 
	public bool fighting=false; 
	public bool faceright=true;
	public bool auxfight=false;
	public GameObject target=null;
	private float auxspeed=0f;
	private Text life;				//Text on Canvas
	private Text money;				//Text on Canvas
	private GameObject LifeBtn;		//Button on Canvas
	private GameObject MoneyBtn;	//Button on Canvas
	public Vector3[] custom;		//Generated with path points (adding noise to the path)
	private bool Step=false;		//in direction to target
	private float seed = 0.2f;
	private bool randomized = false;
	private float rand=0f;
	private bool off=false;         //It is used by the Sorcerer, to stop the movement when create Runes
    private AudioSource audio;
    void Start () {
        audio = this.gameObject.AddComponent<AudioSource>();
        audio.volume = master.getEffectsVolume() / 5;
        life = GameObject.Find("Life").GetComponent<Text>();
		money = GameObject.Find("Money").GetComponent<Text>();
		LifeBtn = GameObject.Find("Button");
	}

	// Update is called once per frame
	void Update () {
		if(!master.isFinish()){
			if(fighting==false){auxfight=false;}
			if(auxfight!=fighting){
				rand = Random.Range(0.001f, 2F);
				float randb = Random.Range(rand, rand+2f);
				Invoke("setFight",Random.Range(rand, randb));                                               //Randomize the time to "stop and fight" when have a target...
            }
			if(speed!=0f&&auxfight==false&&off==false){
				if(randomized==false){
					auxspeed=speed;
					custom = new Vector3[path.Length];
					randomized=true;
					randomizePath();                                                
				}
				needFlip(custom[currentPoint]);
				transform.position = Vector2.MoveTowards (transform.position, custom[currentPoint], Time.deltaTime*speed);              //Go to the next path point
				this.transform.position=new Vector3(this.transform.position.x,this.transform.position.y,this.transform.position.y);     //Configure Z
				Vector2 patchPos = new Vector2 (this.transform.position.x,this.transform.position.y);
				Vector2 patchCustomPos = new Vector2 (custom[currentPoint].x,custom[currentPoint].y);
				if(patchPos==patchCustomPos){                                                               //Path Point reached, then go to the next path point
					if(currentPoint == path.Length-1){                                                      //This path point is the last point?
						int value = int.Parse (life.text);
						if(value>0){                                                                        //Player life > 0
							Animator anim = LifeBtn.GetComponent<Animator>();
							anim.Play("Size");
							value--;
							life.text = "" + value;
						}else{
							End();                                                                          //Player life = 0, Finish
                        }
					}
					currentPoint++;
				}
				if(currentPoint>=path.Length){                                                              //Last point reached
                    speed = 0;
                    Invoke("DestroyDelay",2);
                    audio.clip = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().enemyOnLimit;
                    audio.Play();                  
                    this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, 1000);
                    GetComponent<PathFollower>().enabled = false;
                }                                    //On Latest point + 1
			}
			if(target==null){fighting=false;}
			if(fighting==true&&faceright==false&&GetComponent<Enemies_Controller>().isHero==false){Flip();}
		}
	}
    public void setSpeed0() {
        speed = 0;
    }
    private void DestroyDelay() {
        Destroy(this.gameObject);
    }
    /// <summary>
    /// It is used by the Sorcerer to stop the movement
    /// </summary>
	public void Turn_Off(){
		off=true;
		Invoke("Turn_On",GameObject.Find("Instance_Point").GetComponent<Master_Instance>().Sorcerer_Runes_Time);
	}
    /// <summary>
    /// Re enable sorcerer movement
    /// </summary>
	private void Turn_On(){
		off=false;
	}
    /// <summary>
    /// Game Life value is 0, The game must to finish
    /// </summary>
	private void End(){
		GameObject.Find("Instance_Point").GetComponent<Waves_Creator_Controller>().end=true;
    }

    public void ByeHero() {
        target = null;
        auxfight = false;
        fighting = false;
    }

    public void setFightHero(GameObject hero) {
        target = hero;
        auxfight = true;
        fighting = true;
    }

	public void setFight(){
		auxfight=true;
		fighting=true;
	}
    /// <summary>
    /// Reduce speed, it is used by Magician Tower Trap
    /// </summary>
	public void reduceSpeed(){
		speed = speed/2;
		Invoke("setSpeed",2);
	}
    /// <summary>
    /// Configure enemy speed
    /// </summary>
	private void setSpeed(){speed=auxspeed;}
    /// <summary>
    /// Configure flip depending of the next path position
    /// </summary>
    /// <param name="customPos"></param>
	public void needFlip(Vector3 customPos){
		if(customPos.x>=this.transform.position.x&&faceright==false){Flip();}
		if(customPos.x<this.transform.position.x&&faceright==true){Flip();}
	}
	/// <summary>
    /// Create noise into the path
    /// </summary>
	void randomizePath(){
		for(int i = 0;i < path.Length;i++){
			if(path[i].gameObject.name!="End"){
				custom[i] = new Vector3(path[i].position.x + Random.Range(-seed, seed),path[i].position.y + Random.Range(-seed, seed),path[i].position.y);
			}else{
				custom[i] = new Vector3(path[i].position.x ,path[i].position.y ,path[i].position.y);
			}
		}
	}

	void Stop(){Step=true;}
    /// <summary>
    /// Flip Character
    /// </summary>
	void Flip(){
		faceright=!faceright;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
