using UnityEngine;
using System.Collections;

/// <summary>
/// It is used when creating trap, set trap as cursor
/// </summary>
public class TrapCur : MonoBehaviour {
    
	void Update () {
		if (Input.GetMouseButtonDown(0)&&GameObject.Find("hand")){
			Destroy (this.gameObject);
		}
		if (Input.GetKey(KeyCode.Escape)){
			Cursor.visible = true;
			Destroy (this.gameObject);
		}
		if (Input.GetMouseButtonUp(0)&&!GameObject.Find("hand")){
			GameObject trap_ = Instantiate(Resources.Load("MT/BaseTrap"), this.transform.position , Quaternion.identity)as GameObject;
			trap_.name="trap";
			trap_.gameObject.transform.parent = this.transform.parent.transform;
			Cursor.visible = true;
			Destroy (this.gameObject);
		}
		if(Camera.main){
			Vector3 aux = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			aux.z=-0.5f;
			this.transform.position= aux;
		}
	}
}
