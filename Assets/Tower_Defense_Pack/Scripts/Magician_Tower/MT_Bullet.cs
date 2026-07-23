using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FThLib;

/// <summary>
/// It is used by the Bullet of the Magician Tower
/// Magic Tower Bullet sound
/// </summary>
public class MT_Bullet : MonoBehaviour {
	public GameObject target=null;                                                                              //Enemy target, It is configured when instantiated by MT_Controller.cs of the magician tower
    public int accuracy_mode=3;                                                                                 //1 the best
	public float maxLaunch = 4;
	public bool fire = false;                                                                                   //It is configured when instantiated by MT_Controller.cs of the magician tower
    public bool ice = false;
	public int Damage_=0;                                                                                       //It is configured when instantiated by MT_Controller.cs of the magician tower
	private bool activated = false;
	private bool sw =false;
	private float launch_placey=0f;
	private List<GameObject> firelist;                                                                          //Fire Object Pooling
	private bool on=false;
	private Vector3 latestpos = new Vector3(0,0,0);
    AudioSource audio;
    AudioClip[] explosion;

	// Use this for initialization
	void Start () {
        audio = this.gameObject.AddComponent<AudioSource>();
        audio.volume = master.getEffectsVolume()/5;
        explosion = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().magicExplosion;
        firelist = GameObject.Find("Instance_Point").GetComponent<Fire_Pooling>().fireList;
        
		sw=true;
		master.setLayer("tower",this.gameObject);
		if(fire==false){
			master.getChildFrom("add3",this.gameObject).GetComponent<MT_AddFire>().enabled=false;
			master.getChildFrom("add4",this.gameObject).GetComponent<MT_AddFire>().enabled=false;			
		}
	}

	void OnTriggerEnter2D(Collider2D coll) {
		sw=false;
		GetComponent<Rigidbody2D>().isKinematic=true;
		GetComponent<Collider2D>().enabled=false;
        target = null;
        audio.Play();
        Invoke("onDestroy",1);
	}
    /// <summary>
    /// It is used on Android
    /// </summary>
	void FixedUpdate(){
		if (Application.platform == RuntimePlatform.Android){
			transform.Rotate(Vector3.forward * Time.deltaTime * 400);
			if(target!=null){
				latestpos = target.transform.position;
				if(activated==false){
					activated=true;
				}else{
					if(sw==true){	
						if (fire==true&&on==false){;
							on=true;
							StartCoroutine(CreateFire_());
						}
						transform.position = Vector3.MoveTowards(transform.position, target.transform.position, Time.deltaTime*2);
					}
				}
			}else{
                if (audio.clip == null)
                {
                    audio.clip = explosion[Random.Range(0 , explosion.Length - 1)];
                    audio.Play();
                    this.transform.position = new Vector3(0, 0, 1000);
                    target = null;
                }
                Invoke("onDestroy", 0.5f);
            }
		}
	}
	/// <summary>
    /// It is used on PC
    /// </summary>
	void Update () {
		if (Application.platform != RuntimePlatform.Android){
			transform.Rotate(Vector3.forward * Time.deltaTime * 400);
			if(target!=null){
				latestpos = target.transform.position;
				if(activated==false){
					activated=true;
				}else{
					if(sw==true){	
						if (fire==true){;
							CreateFire();
						}
						transform.position = Vector3.MoveTowards(transform.position, target.transform.position, Time.deltaTime*2);
					}
				}
			}else{
                if (audio.clip == null)
                {
                    audio.clip = explosion[Random.Range(0, explosion.Length - 1)];
                    audio.Play();
                    this.transform.position = new Vector3(0, 0, 1000);
                    target = null;
                }
                Invoke("onDestroy", 0.5f);
            }
		}
	}
    /// <summary>
    /// Instantiate Bullet
    /// </summary>
    /// <param name="pos">position</param>
    /// <param name="name">bullet name</param>
	private void Istantiate_Add(GameObject pos, string name){
		GameObject Bullet = Instantiate(Resources.Load("MT/Mfire"), pos.transform.position, Quaternion.identity)as GameObject;
		Bullet.transform.parent = GameObject.Find("Environment").transform;
		MT_Bullet BulletProperties = Bullet.GetComponent<MT_Bullet>();
		Bullet.name=name;
	}
    /// <summary>
    /// Create fire from the firelist
    /// </summary>
	void CreateFire(){
		if(on==false){
			for(int i = 0;i<firelist.Count;i++){
				if(!firelist[i].activeInHierarchy){
					firelist[i].transform.localScale= new Vector3(0.03250099f,0.03250099f,0.03250099f);
					firelist[i].transform.position = this.transform.position;
					firelist[i].SetActive(true);
					break;
				}
			}
		}
	}
    /// <summary>
    /// Create fire from the firelist, it is used on Android 
    /// </summary>
    /// <returns></returns>
	IEnumerator CreateFire_(){
		yield return new WaitForSeconds(0);
			for(int i = 0;i<firelist.Count;i++){
				if(!firelist[i].activeInHierarchy){
					firelist[i].transform.localScale= new Vector3(0.04f,0.04f,0.04f);
					firelist[i].transform.position = this.transform.position;
					firelist[i].SetActive(true);
					break;
				}
			}
		on=false;
	}

	void onDestroy(){
		Destroy (this.gameObject);
	}
}
