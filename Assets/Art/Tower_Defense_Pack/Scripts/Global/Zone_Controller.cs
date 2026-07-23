using UnityEngine;
using System.Collections;

/// <summary>
/// It is used by Archer, Knights and Magician tower to detect the enemies
/// </summary>
public class Zone_Controller : MonoBehaviour {
	private GameObject parent_;
	private AT_Controller ATProperties;
	private KT_Controller KTProperties;
	private MiniKT_Controller MiniKTProperties;
	private MT_Controller MTProperties;
	
    /// <summary>
    /// It is used by Tower / Child gameobject (Zone or TargetedZone)
    /// Get the properties depending of the tower name
    /// </summary>
	void Start () {
		parent_ = this.transform.parent.gameObject;
        //Get tower name
		if(parent_.name=="AT"+0||parent_.name=="AT"+1||parent_.name=="AT"+2){                           //Archer tower
			ATProperties = parent_.GetComponent<AT_Controller>();
		}
		if(parent_.name=="KT"+0||parent_.name=="KT"+1||parent_.name=="KT"+2){                           //Knight tower
			KTProperties = parent_.GetComponent<KT_Controller>();
		}
		if(parent_.name=="MT0"){                                                                        //Magician tower
			MTProperties = parent_.GetComponent<MT_Controller>();
		}
		if(parent_.name=="MiniKT0"){                                                                    //2 Knights patrol
			MiniKTProperties = parent_.GetComponent<MiniKT_Controller>();
		}
	}

    /// <summary>
    /// Here you set what enemy can be detected by tower
    /// Add the enemy detected to the tower enemies list
    /// </summary>
    /// <param name="other">enemy detected</param>
	void OnTriggerEnter2D(Collider2D other) {
		if(other.tag=="Respawn"){
			if(parent_.name=="AT"+0||parent_.name=="AT"+1||parent_.name=="AT"+2){                       //Archer tower can detect all enemies
				ATProperties.enemyAdd(other.gameObject);
			}
			if(parent_.name=="KT"+0||parent_.name=="KT"+1||parent_.name=="KT"+2){                       //Knight tower cant detect enemy3
				if(other.gameObject.GetComponent<Enemies_Controller>().type!="enemy3"){
					KTProperties.enemyAdd(other.gameObject);
				}
			}
			if(parent_.name=="MT0"){                                                                    //Magician tower can detect all enemies
				MTProperties.enemyAdd(other.gameObject);
			}
			if(parent_.name=="MiniKT0"){                                                                //2 knights patrol cant detect enemy3
				if(other.gameObject.GetComponent<Enemies_Controller>().type!="enemy3"){
					MiniKTProperties.enemyAdd(other.gameObject);
				}
			}
		}
	}
    /// <summary>
    /// Remove enemy from the tower enemies list
    /// </summary>
    /// <param name="other">enemy detected</param>
	void OnTriggerExit2D(Collider2D other) {
		if(other.tag=="Respawn"){
			if(parent_.name=="AT"+0||parent_.name=="AT"+1||parent_.name=="AT"+2){                       //Archer tower
				ATProperties.enemyRemove(other.gameObject.name);
			}
			if(parent_.name=="KT"+0||parent_.name=="KT"+1||parent_.name=="KT"+2){                       //Knight tower
				if(other.gameObject.GetComponent<Enemies_Controller>().type!="enemy3"){
					KTProperties.enemyRemove(other.gameObject.name);
				}
			}
			if(parent_.name=="MT0"){                                                                    //Magician tower
				MTProperties.enemyRemove(other.gameObject.name);
			}
			if(parent_.name=="MiniKT0"){                                                                //2 knights patrol
				if(other.gameObject.GetComponent<Enemies_Controller>().type!="enemy3"){
					MiniKTProperties.enemyRemove(other.gameObject.name);
				}
			}
		}
	}
}
