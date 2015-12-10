using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameCube : MonoBehaviour {
	private static GameObject cube_prefab = Resources.Load ("Prefabs/cube_prefab", typeof(GameObject)) as GameObject; 

	public Coordinates myCoords;// { get; private set;}
	public Color myColor;// { get; private set; }
	public int pushNumber { get; private set;}
	public float zCameraOffset;

	//spawns a cube at coordinates, chooses a random color, and assigns a pusher number
	public static GameCube SpawnCube(Coordinates spawnSpace, List<Color> spawnColors, int pusherNumber){
		GameObject go = Instantiate (cube_prefab, GameManager.instance.origin + spawnSpace.vector * GameManager.instance.worldScale, Quaternion.identity) as GameObject;
		GameCube gc = go.GetComponent<GameCube> ();
		go.transform.localScale *= GameManager.instance.worldScale;
		gc.pushNumber = pusherNumber;
		gc.myCoords = spawnSpace;
		gc.myColor = spawnColors [Random.Range (0, spawnColors.Count)];
		go.GetComponent<Renderer> ().material.color = gc.myColor;
		go.transform.Find ("label").GetComponent<TextMesh> ().text = pusherNumber.ToString ();
		if (pusherNumber == 0)//if the pusher number is 0, don't show it, it's just a cube.
			Destroy (go.transform.Find ("label").gameObject);
		return gc;}


	public void Despawn(float duration){
		if (duration == 0)
			Destroy (gameObject);
		else {
			StartCoroutine (FadeAnim (duration));
		}
	}

	public IEnumerator FadeAnim(float animDuration){
		Color start = gameObject.GetComponent<Renderer> ().material.color;
		for (float t = 0; t < animDuration; t += Time.deltaTime) {
			gameObject.GetComponent<Renderer> ().material.color = Color.Lerp (start, new Color (1, 1, 1, 0), t / animDuration);
			yield return null;
		}
		Destroy (gameObject);
	}

	public void ChangeToBase(){
		Destroy (gameObject.transform.Find ("label").gameObject);
		pushNumber = 0;
	}

	public void Move(bool snap, float animDuration){
		if (snap)
			gameObject.transform.position = GameManager.instance.origin + myCoords.vector * GameManager.instance.worldScale;
		else{
			StopAllCoroutines ();
			StartCoroutine (MoveAnim (animDuration));
		}
	}

	IEnumerator MoveAnim(float animDuration){
		Vector3 startPos = gameObject.transform.position;
		Vector3 targetPos = GameManager.instance.origin + myCoords.vector * GameManager.instance.worldScale;
		for (float t = 0; t < animDuration; t+=Time.deltaTime) {
			gameObject.transform.position = Vector3.Lerp (startPos, targetPos, (t / animDuration));
			yield return null;
		}
		gameObject.transform.position = targetPos;
	}
	
	void OnMouseDown(){
		GameManager.instance.HandleCubeClick (this);
	}

	void OnMouseDrag(){
		GameManager.instance.HandleCubeDrag (this);
	}
}
