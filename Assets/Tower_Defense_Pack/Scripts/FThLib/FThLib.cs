using UnityEngine;
using System.Collections;
/// <summary>
/// Namespace with most used functions.
/// </summary>
namespace FThLib {
	public class master : MonoBehaviour {
		public bool vare=false;
        
        /// <summary>
        /// Instantiate Progress bar used as countdown, for example when it is instantiated a new tower
        /// </summary>
        /// <param name="time_">Time for countdown</param>
        /// <param name="parent_">To set position</param>
		public static void Instantiate_Progressbar(float time_, GameObject parent_){
			Vector3 barposition = getChildFrom("opened",parent_).transform.position;
			barposition.z = barposition.y-3f;
			GameObject Pb = Instantiate(Resources.Load("Buttons/ProgressBar"), barposition, Quaternion.identity)as GameObject;
			Pb.name="ProgressBar";
			GameObject bar = getChildFrom("bar",Pb);
			ProgressBar BarProperties = bar.GetComponent<ProgressBar>();
			BarProperties.time = time_;
			Pb.transform.parent = parent_.transform;
		}
        /// <summary>
        /// It is used to create animation with the scale
        /// </summary>
        /// <param name="go">gameobject</param>
        /// <param name="instance"></param>
		public static void ClickEffect(GameObject go, MonoBehaviour instance){
			go.transform.localScale = go.transform.localScale/1.2f;
			instance.StartCoroutine(ReturnEffect(go));
		}
        /// <summary>
        /// Animate localscale
        /// </summary>
        /// <param name="go">gameobject</param>
        /// <returns></returns>
		static IEnumerator ReturnEffect(GameObject go){
			yield return new WaitForSeconds(0.1f);
			go.transform.localScale = go.transform.localScale*1.2f;
		}
        /// <summary>
        /// Set Z position relative to the Y, you can add increment
        /// </summary>
        /// <param name="vec">current position</param>
        /// <param name="increment">you can add increment</param>
        /// <returns>Vector3 with z normalized</returns>
		public static Vector3 setThisZ(Vector3 vec, float increment){
			vec = new Vector3(vec.x,vec.y,vec.y+increment);
			return vec;
		}
        /// <summary>
        /// Destroy the tower and instantiate a new empty Construction Point
        /// </summary>
        /// <param name="tower">Tower to sell</param>
		public static void sellTower(GameObject tower){
			Destroy (tower);
			Instantiate_CP(tower.transform);
		}
        /// <summary>
        /// Instantiate the interface buttons, Interfaces prefab are on ./Resources/Interface
        /// </summary>
        /// <param name="interfacename">Name of interface prefab</param>
        /// <param name="parent_">Tower</param>
        /// <param name="pos">Tower Position</param>
		public static void showInterface(string interfacename , GameObject parent_, Transform pos){
			if(GameObject.Find("UI_Exit").GetComponent<Canvas>().enabled==false){
                GameObject.Find("UI").GetComponent<AudioSource>().Play();
                if (GameObject.Find("Interface")){
					other_Interfaces_off();
				}
				GameObject interface_ = Instantiate(Resources.Load("Interface/"+ interfacename), GameObject.Find("Out").transform.position, Quaternion.identity)as GameObject;
				interface_.name="Interface";
				interface_.transform.SetParent(parent_.transform);
				interface_.transform.position = new Vector3(pos.transform.position.x, pos.transform.position.y, pos.transform.position.y-3f);
			}
		}
        /// <summary>
        /// Show mouse cursor hand
        /// </summary>
        /// <param name="value">true/false</param>
		public static void showHand(bool value){//show hand cursor
			if(GameObject.Find("UI_Exit").GetComponent<Canvas>().enabled==false){
				if(value==true){
					Cursor.visible = false;
					if(Camera.main){
						Vector3 aux = Camera.main.ScreenToWorldPoint(Input.mousePosition);
						aux.z=aux.y-4f;
						GameObject hand = Instantiate(Resources.Load("Cursor/hand"), aux , Quaternion.identity)as GameObject;
						hand.name="hand";
					}else{
						Debug.Log("errr");
					}
				}else{
					Cursor.visible = true;
					Destroy (GameObject.Find("hand"));
				}
			}
		}
        /// <summary>
        /// Disable interfaces to avoid 2 interfaces at same time
        /// </summary>
		public static void other_Interfaces_off(){
			if(getChildFrom("zoneImg",GameObject.Find("Interface").transform.parent.gameObject)!=null){
				if(!GameObject.Find("circle")){
					getChildFrom("zoneImg",GameObject.Find("Interface").transform.parent.gameObject).GetComponent<SpriteRenderer>().enabled=false;
				}
				GameObject.Find("Interface").transform.parent.gameObject.GetComponent<CircleCollider2D>().enabled=true;
			}
			Destroy (GameObject.Find("Interface"));
		}
        /// <summary>
        /// Get Child
        /// </summary>
        /// <param name="name">Child name</param>
        /// <param name="from">Parent</param>
        /// <returns>Child</returns>
		public static GameObject getChildFrom(string name, GameObject from){
			GameObject aux=null;
			foreach(Transform child in from.transform){if(child.name==name){aux=child.gameObject;}}
			return aux;
		}
        /// <summary>
        /// Set layer
        /// </summary>
        /// <param name="name">layer</param>
        /// <param name="go">gameobject</param>
		public static void setLayer(string name, GameObject go){if(LayerMask.NameToLayer(name)!=-1){go.layer = LayerMask.NameToLayer(name);}}
        /// <summary>
        /// It is used on parabolic shot bettween to vectors
        /// </summary>
        /// <param name="a">pos a</param>
        /// <param name="b">pos b</param>
        /// <returns>angle</returns>
		public static float angle_(Vector3 a, Vector3 b){
			float angle = Mathf.Atan2(a.y-b.y,a.x-b.x)*180 / Mathf.PI;
			return angle;
		}
        /// <summary>
        /// It is used to configure the gameobject transparency
        /// </summary>
        /// <param name="go"></param>
		public static void isHide(GameObject go){
			Color col = go.GetComponent<SpriteRenderer>().material.color;
			col.a = 0.5f;
			go.GetComponent<SpriteRenderer>().material.color = col;
		}
		public static bool isFinish(){//Implemented for a future use
			bool aux = false;
			/*if(GameObject.Find("GameOver")){
				aux=true;
				other_Interfaces_off();
			}*/
			return aux;
		}
        /// <summary>
        /// Game finished
        /// </summary>
		public static void setGameOver(){//Disable all scripts
			GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>() ;
			foreach(GameObject go in allObjects){
				if(go.name!="GameOver"&&go.tag!="Respawn"&&go.name!="Instance_Point"&&go.name!="Canvas"){//Disable all scripts except...
					if(go.layer != LayerMask.NameToLayer("UI")){
						MonoBehaviour[] scripts = go.GetComponents<MonoBehaviour>();
						foreach(MonoBehaviour script in scripts){script.enabled = false;}
					}
				}
			}
		}
        /// <summary>
        /// Get Effects volume from Audio Manager
        /// </summary>
        /// <returns></returns>
        public static float getEffectsVolume()
        {
            float aux = 0;
            aux = GameObject.Find("AudioManager").GetComponent<Audio_Manager>().effects_volume;
            return aux;
        }
        /// <summary>
        /// Instantiate an empty construction point
        /// </summary>
        /// <param name="pos">position</param>
		private static void Instantiate_CP(Transform pos){
			Vector3 cpos = pos.position;
			cpos.y = cpos.y - 0.1f;
			GameObject CP = Instantiate(Resources.Load("CP/CP"), cpos, Quaternion.identity)as GameObject;
			CP.name="CP";
		}
	}
}