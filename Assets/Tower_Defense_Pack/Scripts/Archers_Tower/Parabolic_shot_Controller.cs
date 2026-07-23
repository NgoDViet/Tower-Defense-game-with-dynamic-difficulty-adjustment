using UnityEngine;
using System.Collections;
using System.Collections.Generic;   //------------------------------------------------<<
using System;
using FThLib;

/// <summary>
/// Arrow Parabolic Shot Controller
/// It is used to set fire and get it from firelist 
/// </summary>

public class Parabolic_shot_Controller : MonoBehaviour {
	public GameObject target=null;
	public int accuracy_mode=3;                                     //1 the best
	public float maxLaunch = 4;                                     
	private bool activated = false;
	private bool sw =false;
	public bool fire = false;
	private Vector3 latestpos = new Vector3(0,0,0);
	private List<GameObject> firelist;	                            //Fire ObjectPooling                   
	
    /// <summary>
    /// Get Fire Object list and set the layer
    /// </summary>
	void Start () {
		firelist = GameObject.Find("Instance_Point").GetComponent<Fire_Pooling>().fireList;
		sw=true;
		master.setLayer("tower",this.gameObject);
	}
	
	void OnTriggerEnter2D(Collider2D coll) {
		sw=false;
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        GetComponent<Collider2D>().enabled=false;
		this.transform.position = master.setThisZ(this.transform.position,+0.3f);
		Invoke("onDestroy",1);
	}
    /// <summary>
    /// CreateFire
    /// </summary>
	void FixedUpdate(){
		if(sw==true){	
			if (fire==true){
				CreateFire();
			}
		}
	}
	// Update is called once per frame
	void Update () {       
        if (target!=null){           
			latestpos = target.transform.position;
			if(activated==false){
				activated=true;
				PreLaunch();
			}else{
				if(sw==true){	
					if(GetComponent<Rigidbody2D>().isKinematic==false){simulateRotation();}
					transform.position = Vector2.MoveTowards(transform.position, target.transform.position, Time.deltaTime/accuracy_mode);
					if(GetComponent<Rigidbody2D>().linearVelocity.y<0){isFalling();}
                    this.transform.position = master.setThisZ(this.transform.position, -0.3f);
                }
			}
		}else{
            //this.transform.position = master.setThisZ(this.transform.position, -0.3f);
            find_error();
		}       
    }
    /// <summary>
    /// Active Fire and set position
    /// </summary>
	void CreateFire(){
		for(int i = 0;i<firelist.Count;i++){
			if(!firelist[i].activeInHierarchy){
				firelist[i].transform.localScale= new Vector3(0.03250099f,0.03250099f,0.03250099f);
				firelist[i].transform.position = this.transform.position;
				firelist[i].SetActive(true);
				break;
			}
		}
	}
    /// <summary>
    /// It is used to stop falling when the target has more 'Y' than the arrow
    /// </summary>
	void isFalling(){
        if (this.transform.position.y < target.transform.position.y) {
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }
		float auxv = 0f;
		float speedy = target.GetComponent<Rigidbody2D>().linearVelocity.y;
		float speedx = target.GetComponent<Rigidbody2D>().linearVelocity.x;
		if(Math.Sqrt(speedy*speedy)>Math.Sqrt(speedx*speedx)){
			auxv=speedy;
		}else{
			auxv=speedx;
		}
		if(auxv<0){auxv = -auxv;}
		if(accuracy_mode==1){
			transform.position = Vector2.MoveTowards(transform.position, target.transform.position, Time.deltaTime*1);
		}
		find_error();//Not Hit Detection
	}
    /// <summary>
    /// It is used to stop falling when the latest position of a target has more 'Y' than the arrow
    /// </summary>
	void isFallingNoTarget()
    {
        if (this.transform.position.y < latestpos.y)
        {
            Invoke("onDestroy", 0.3f);
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            this.transform.position = master.setThisZ(this.transform.position, +0.3f);
        }
    }
    /// <summary>
    /// It prevents arrows from sliding on the ground
    /// </summary>
    private void find_error(){
        if (target != null)
        {
            if (this.transform.position.y < target.transform.position.y)
            {
                sw = false;
                Vector3 rotation_ = this.gameObject.transform.eulerAngles;
                GetComponent<Rigidbody2D>().isKinematic = true;
                GetComponent<Collider2D>().enabled = false;
                this.gameObject.transform.localEulerAngles = rotation_;
                Invoke("onDestroy", 1);
            }
        }
        else
        {
            if (sw == true)
            {
                if (GetComponent<Rigidbody2D>().bodyType != RigidbodyType2D.Static) { simulateRotation(); }
                transform.position = Vector2.MoveTowards(transform.position, latestpos, Time.deltaTime / accuracy_mode);
                if (GetComponent<Rigidbody2D>().linearVelocity.y < 0) { isFallingNoTarget(); }
                this.transform.position = master.setThisZ(this.transform.position, -0.3f);
            }
        }
    }
    /// <summary>
    /// It predicts the next position of the target
    /// </summary>
    /// <param name="speedy">Speed Y axis</param>
	private void Calculation(float speedy){
		next_position(Time.time%((speedy/9.81f)*2));
	}
	/// <summary>
    /// Get the next position
    /// </summary>
    /// <param name="airtime"></param>
	void next_position(float airtime){
		float xTarget = target.transform.position.x;
		float yTarget = target.transform.position.y;
		float speedy = target.GetComponent<Rigidbody2D>().linearVelocity.y;
		float speedx = target.GetComponent<Rigidbody2D>().linearVelocity.x;
		Launch (xTarget+(speedx*airtime),yTarget+(speedy*airtime));
	}
	/// <summary>
    /// Set the arrow properties relative to the predicted position
    /// </summary>
    /// <param name="xTarget">Target X</param>
    /// <param name="yTarget">Target Y</param>
	private void Launch(float xTarget, float yTarget){ 
		GetComponent<Rigidbody2D>().isKinematic=false;
		float xCurrent = transform.position.x;
		float yCurrent = transform.position.y;
		float xDistance = Math.Abs(xTarget - xCurrent);
		float yDistance = yTarget - yCurrent;
		float fireAngle = 1.57075f - (float)(Math.Atan((Math.Pow(maxLaunch, 2f)+ Math.Sqrt(Math.Pow(maxLaunch, 4f) - 9.8f * (9.8f * Math.Pow(xDistance, 2f) + 2f * yDistance * Math.Pow(maxLaunch, 2f) )))/(9.8f * xDistance)));
		float xSpeed = (float)Math.Sin(fireAngle) * maxLaunch;
		float ySpeed = (float)Math.Cos(fireAngle) * maxLaunch;
		if ((xTarget - xCurrent) < 0f){xSpeed = - xSpeed;}              //Target is on left or Right (-xspeed or xspeed)
		if(!float.IsNaN(xSpeed)&&!float.IsNaN(ySpeed)){    
			GetComponent<Rigidbody2D>().linearVelocity = new Vector3(xSpeed,ySpeed,0f);
		}else{      //Error!!!, It is needed more maxLaunch, Repeat the process
			maxLaunch = maxLaunch + 0.3f;
			PreLaunch();
		}
	}
	/// <summary>
    /// It is needed more maxLaunch, Repeat the process
    /// </summary>
	private void PreLaunch(){ 
		float xTarget = target.transform.position.x;
		float yTarget = target.transform.position.y;
		float xCurrent = transform.position.x;
		float yCurrent = transform.position.y;
		float xDistance = Math.Abs(xTarget - xCurrent);
		float yDistance = yTarget - yCurrent;
		float fireAngle = 1.57075f - (float)(Math.Atan((Math.Pow(maxLaunch, 2f)+ Math.Sqrt(Math.Pow(maxLaunch, 4f) - 9.8f * (9.8f * Math.Pow(xDistance, 2f) + 2f * yDistance * Math.Pow(maxLaunch, 2f) )))/(9.8f * xDistance)));
		float xSpeed = (float)Math.Sin(fireAngle) * maxLaunch;
		float ySpeed = (float)Math.Cos(fireAngle) * maxLaunch;
		if ((xTarget - xCurrent) < 0f){xSpeed = - xSpeed; }             //Target is on left or Right (-xspeed or xspeed)
        Calculation (ySpeed);                                           //Repeat the Launch Process
		sw=true;
	}
    /// <summary>
    /// It is used to simulate the arrow rotation relative to its trajectory
    /// </summary>
	void simulateRotation(){
		Vector3 velocity = GetComponent<Rigidbody2D>().linearVelocity;
		float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
		Quaternion tempRotation = Quaternion.AngleAxis(angle, Vector3.forward);
		Quaternion newRotation = this.gameObject.transform.rotation;
		newRotation.x = Mathf.LerpAngle(newRotation.x, tempRotation.x, 1000000);
		newRotation.y = Mathf.LerpAngle(newRotation.y, tempRotation.y, 1000000);
		newRotation.z = Mathf.LerpAngle(newRotation.z, tempRotation.z, 1000000);
		newRotation.w = Mathf.LerpAngle(newRotation.w, tempRotation.w, 1000000);
		this.gameObject.transform.rotation = newRotation;
	}
    /// <summary>
    /// Get Child Gameobject
    /// </summary>
    /// <param name="name">Child name</param>
    /// <returns>Child</returns>
	private GameObject getChild(string name){
		GameObject aux=null;
		foreach(Transform child in gameObject.transform){if(child.name==name){aux=child.gameObject;}}
		return aux;
	}
	/// <summary>
    /// Destroy arrow
    /// </summary>
	void onDestroy(){
		Destroy (this.gameObject);
	}
}
