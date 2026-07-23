/*
    Waves creator
 	Here action when finish the scene, by default enable "UI_Exit" canvas.
*/


using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FThLib;
using UnityEngine.EventSystems;

/// <summary>
/// It is used to read the Wave Prefab and it is attached to Instance_Point gameobject
/// Start sound
/// </summary>
public class Waves_Creator_Controller : MonoBehaviour {
	public GameObject customWavePrefab;                                                                             //Wave prefab created with Waves_Editor scene
	private Master_Instance masterPoint;
	private int wavesIndex = 0;                                                                                     //Number of waves, it is read from the wave prefab
	private int[] delayBetweenWaves;                                                                                // = new int[] {25, 20, 0}; -> Wave 1 (0 secs), Wave 2 (20 secs), Wave 3 (25 secs)   /// In this array the latest is the first to appear ///It is read from the wave prefab
	private bool playing=false;
	private bool auxplaying=false;
	public bool end=false;
	private bool sw=false;
	private Text waves;
    private AudioSource audio;
    private AudioClip start;
    /// <summary>
    /// Read wave prefab
    /// </summary>
	void Start () {
        audio = this.gameObject.AddComponent<AudioSource>();
        audio.volume = master.getEffectsVolume();
        audio.clip = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().start;
        wavesIndex =customWavePrefab.GetComponentsInChildren<Transform>().Length - 1;
		delayBetweenWaves = new int[customWavePrefab.GetComponentsInChildren<Transform>().Length - 1];
		Transform[] aux = customWavePrefab.GetComponentsInChildren<Transform>();
		for(int i = 0; i< delayBetweenWaves.Length;i++){
			delayBetweenWaves[i] = aux[i+1].GetComponent<waves>().delay;
		}
		masterPoint = GameObject.Find("Instance_Point").GetComponent<Master_Instance>();
		waves = GameObject.Find("Waves").GetComponent<Text>();
		waves.text = wavesIndex + "/" + (customWavePrefab.GetComponentsInChildren<Transform>().Length - 1);
		wavesIndex--;
	}
	
	// Update is called once per frame
	void Update () {
		if (GetComponent<Master_Instance>().Finish==false){
			if (Input.GetButtonDown("Jump")){
				StartPlaying();
			}
			if(playing==true&&wavesIndex>=0){
				if(auxplaying==false){
					auxplaying=true;
					if(delayBetweenWaves[wavesIndex]>0){
						master.Instantiate_Progressbar(delayBetweenWaves[wavesIndex],this.gameObject);
						master.getChildFrom("ProgressBar",this.gameObject).transform.localScale=new Vector3(2,2,1);
					}
					Invoke("Wave_Creator",delayBetweenWaves[wavesIndex]);
				}
			}
			if(wavesIndex<0){playing=false;}
		}
		if(wavesIndex<0&&playing==false&&end==false&&GameObject.FindGameObjectsWithTag("Respawn").Length==0){end=true;}	        //Finish the game, you win
		if(end==true&&sw==false){								
			sw=true;
			Invoke("FinishScene",3f);									//Load finish event
		}
	}
	/// <summary>
    /// Start playing
    /// </summary>     	
	public void StartPlaying(){
        audio.Play();
		Destroy (GameObject.Find("pressSpace"));
		playing=true;
	}
    /// <summary>
    /// Start exit process
    /// </summary>
	public void Exit(){
        if (EventSystem.current.currentSelectedGameObject.name == "Exit")
        {
            Invoke("CrossfadeDelayed", 0.5f);
        }
        else {
            if (PlayerPrefs.HasKey("sound")) {
                switch (PlayerPrefs.GetInt("sound")){
                    
                    case 0: AudioListener.volume = 1.0f; PlayerPrefs.SetInt("sound", 1); break;
                    case 1: AudioListener.volume = 0.0f; PlayerPrefs.SetInt("sound", 0); break;
                }
            } else {
                PlayerPrefs.SetInt("sound", 0);
                AudioListener.volume = 0.0f;
            }            
        }
    }
    /// <summary>
    /// Crossfade Effect
    /// </summary>
	private void CrossfadeDelayed(){
		GameObject.Find("Crossfade").GetComponent<Animator>().SetBool("out",true);
		Invoke("ExitDelayed",2);
	}
    /// <summary>
    /// Delay when exit
    /// </summary>
	private void ExitDelayed(){Application.Quit(); }
    /// <summary>
    /// Create the wave by calling "CreateWave" on "Master_Instance.cs" and send the send the childs of the wave prefab (each child contains Waves.cs with its properties like enemies of this wave and path letter a,b,c ...)
    /// </summary>
	private void Wave_Creator(){
		auxplaying=false;
		if(wavesIndex>=0){
			masterPoint.createWave(customWavePrefab.GetComponentsInChildren<Transform>(),wavesIndex+1);                                 //Here the wave is created
			waves.text = wavesIndex + "/" + (customWavePrefab.GetComponentsInChildren<Transform>().Length - 1);
			wavesIndex--;
		}
	}
	/// <summary>
    /// Action when finished, Destroy all enemies and Enable UI_Exit Canvas
    /// </summary>
	private void FinishScene(){
		if(GameObject.FindGameObjectsWithTag("Respawn").Length==0||GameObject.Find("Life").GetComponent<Text>().text=="0"){
			GetComponent<Master_Instance>().wavePlaying=false;
			GetComponent<Master_Instance>().waveCount=0;
			GetComponent<Master_Instance>().Finish=true;
			GameObject.Find("UI_Exit").GetComponent<Canvas>().enabled=true;
			GameObject[] enemies_ = GameObject.FindGameObjectsWithTag("Respawn");
			if(enemies_.Length>0){															                                            //Destroy all enemies
				for(int i=0;i<enemies_.Length;i++){
					Destroy(enemies_[i]);
				}
			}
		}else{
			sw=false;
		}
	}
}
