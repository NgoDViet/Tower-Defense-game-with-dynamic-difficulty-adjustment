using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Create crossfade effect when loading MainMenu scene
/// </summary>
public class Go_Main : MonoBehaviour {
	void OnMouseDown() {
		Invoke("CrossfadeDelayed",0.5f);
	}
		
	private void CrossfadeDelayed(){
		GameObject.Find("Crossfade").GetComponent<Animator>().SetBool("out",true);
		Invoke("ExitDelayed",2);
	}

	private void ExitDelayed(){
		if (Application.platform == RuntimePlatform.Android){
            SceneManager.LoadScene("MainMenuPhone");
		}else{
            SceneManager.LoadScene("MainMenu");
		}
	}

}
