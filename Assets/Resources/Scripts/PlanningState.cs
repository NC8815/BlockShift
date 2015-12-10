using UnityEngine;

public class PlanningState : MonoBehaviour {

	public void ChangeToAction(){
		GameManager.instance.ChangeGameState (GameState.ACTION);
		//Destroy (gameObject);
	}
}
