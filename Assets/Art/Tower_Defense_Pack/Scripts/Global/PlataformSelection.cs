using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Select scene by Plataform
/// </summary>
public class PlataformSelection : MonoBehaviour {

	void Start () {
		if (Application.platform == RuntimePlatform.Android){
			SceneManager.LoadScene("MainMenuPhone");
		}else{
            SceneManager.LoadScene("MainMenu");
		}
	}

}
