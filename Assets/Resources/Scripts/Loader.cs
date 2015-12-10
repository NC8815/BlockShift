using UnityEngine;
using System.Collections;

public class Loader : MonoBehaviour {
	public GameManager gameManager;
	void Awake () {
		if (GameManager.instance == null)
			Instantiate (gameManager);
	}
}
