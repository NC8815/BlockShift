using UnityEngine;

public class IntroState : MonoBehaviour {

	public void ChangeToPlanning(){
		GameManager.instance.ChangeGameState (GameState.PLANNING);
		//Destroy (gameObject);
	}
}

