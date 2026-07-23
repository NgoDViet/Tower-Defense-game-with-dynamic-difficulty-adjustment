using UnityEngine;
using System.Collections;
using FThLib;

/// <summary>
/// It is used to rotate the runes of the sorcerer and destroy this prefab after time
/// The runes prefab is on ./Resources/Enemies/StopTime
/// </summary>
public class SorcererRunes : MonoBehaviour {
    private AudioSource audio;
    // Use this for initialization
    void Start () {
        audio = this.gameObject.AddComponent<AudioSource>();
        audio.volume = master.getEffectsVolume()/13;
        audio.clip = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().Runes;
        audio.Play();
        Invoke("Destroy_",GameObject.Find("Instance_Point").GetComponent<Master_Instance>().Sorcerer_Runes_Time);
	}
	
	// Update is called once per frame
	void Update () {
		if(this.gameObject.name=="Ring"){
			transform.Rotate(0, 0, Time.deltaTime*10);
		}
	}

	private void Destroy_(){
		Destroy(this.gameObject.transform.parent.gameObject);
	}
}
