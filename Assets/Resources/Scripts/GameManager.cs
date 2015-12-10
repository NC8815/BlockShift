using UnityEngine;
using System.Collections.Generic; 
using System.Linq;
using UnityEngine.UI;

public enum GameState {INTRO, PLANNING, ACTION, RESOLUTION, SUMMARY}

public class GameManager : MonoBehaviour {
	//these are variables which other classes need to see, but which I don't want them setting, and which I need an inital value for.
	//so I declare them here and set them get only, and they return a different set of variables...
	public Vector2 origin { get { return _origin; } }
	public Coordinates gridSize { get { return _gridSize; } }
	public Coordinates gridMax { get { return new Coordinates(_gridSize.x + 1,_gridSize.y + 1); } }
	public Coordinates gridMin { get { return new Coordinates (2, 2); } }
	public float resolutionPushDuration { get { return _resolutionPushDuration; } }
	public float resolutionScoreFadeDuration { get { return _resolutionScoreFadeDuration; } }
	public float resolutionLootFadeDuration { get { return _resolutionLootFadeDuration; } }
	public float resolutionFillDuration { get { return _resolutionFillDuration; } }
	public float worldScale { get { return Mathf.Min (12 / (_gridSize.x + 4f), 9 / (_gridSize.y + 4)); } }
	public int numPushers { get { return _numPushers; } }
	public float[] comboBonuses { get { return _comboBonuses; } }
	public static GameManager instance { get; private set; }

	//which I initialize here. These are private.
	Vector2 _origin = new Vector2 (-5.5f, -5.5f);
	Coordinates _gridSize = new Coordinates (8,5);
	float _resolutionPushDuration = 1.5f;
	float _resolutionScoreFadeDuration = 0.5f;
	float _resolutionLootFadeDuration = 1.0f;
	float _resolutionFillDuration = 1.0f;
	float actionPhaseDuration = 4f;
	int _numPushers = 2;
	int numRounds = 15;
	float[] _comboBonuses = {1f,2f,3f,4f,5f,6f,7f,8f,9f,10f};


	public GameState gameState;
	public GridManager gridControl { get; private set; }
	public GameObject gameDisplay;
	float actionTimer;
	float score = 0;
	int round = 1;

	void Awake () {
		if (GameManager.instance == null) { //this initializes instance. If we already have a value for instance, delete this object, otherwise
			GameManager.instance = this;
		} //set instance to this object.
		else if (GameManager.instance != this) {
			Destroy (gameObject);
		}
		_numPushers = Mathf.Clamp (_numPushers, 0, gridSize.x * 2 + gridSize.y * 2);
		gridControl = GetComponent<GridManager> ();//the grid controller.
		ChangeGameState (GameState.INTRO);
		Debug.Log ("Game Manager Awake");
		Debug.Log (worldScale);
	}

	public void ChangeGameState(GameState newState){
		gameState = newState;
		if (gameDisplay != null)
			Destroy (gameDisplay);
		switch (gameState) {
		case GameState.INTRO:
			gameDisplay = Instantiate (Resources.Load ("prefabs/intro_canvas")) as GameObject;
			gridControl.ResetLoot ();
			break;
		case GameState.PLANNING:
			gridControl.FillGrid ();
			gameDisplay = Instantiate (Resources.Load ("prefabs/planning_canvas")) as GameObject;
			break;
		case GameState.ACTION:
			actionTimer = actionPhaseDuration;
			gameDisplay = Instantiate (Resources.Load ("prefabs/action_canvas")) as GameObject;
			break;
		case GameState.RESOLUTION:
			gridControl.StartCoroutine("Resolution");
			//PushCubes scores the cubes when it's done pushing.
			break;
		case GameState.SUMMARY:
			gameDisplay = Instantiate (Resources.Load ("prefabs/summary_canvas")) as GameObject;
			gameDisplay.transform.Find ("score_text").gameObject.GetComponent<Text>().text = "Score: " + Mathf.Round(score * 100)/100;
			gridControl.ClearGrid();
			gridControl.ResetLoot ();
			score = 0;
			round = 1;
			break;
		}
	}

	public void ChangeScore(float deltaScore){
		score += deltaScore;
		if (++round > numRounds) {
			ChangeGameState (GameState.SUMMARY);
		} else {
			ChangeGameState (GameState.PLANNING);
		}
	}

	public void HandleCubeClick(GameCube cube){
		switch (gameState) {
		case GameState.ACTION:
			gridControl.spacesToClear.Add (cube.myCoords);//Debug.Log (cube.myCoords.x +" " +cube.myCoords.y);
			gridControl.ClearCubes (0);//Debug.Log ("Clicked a Cube!");
			break;
		case GameState.PLANNING:
			if(cube.pushNumber > 0)
				cube.zCameraOffset = Camera.main.WorldToScreenPoint (gameObject.transform.position).z;
			break;
		default:
			break;
		}
	}

	public void HandleCubeDrag(GameCube cube){
		switch (gameState) {
		case GameState.PLANNING:
			if (cube.pushNumber > 0) {
				Coordinates[] snapTrack = gridControl.pusherTrack.ToArray ();
				Vector3 cursorPoint = Camera.main.ScreenToWorldPoint (new Vector3 (Input.mousePosition.x, Input.mousePosition.y, cube.zCameraOffset));
				IEnumerable<Coordinates> sortedTrack = snapTrack.OrderBy (spot => Vector3.Distance (spot.vector * worldScale + origin, cursorPoint));
				if(cube.myCoords != sortedTrack.First ()){
					gridControl.moveOrders.Add (new Coordinates[] {cube.myCoords, sortedTrack.First()});
					gridControl.MoveCubes (true, 0);
				}
			}
			break;
		default:
			break;
		}
	}

	void Update () {
		switch (gameState) {
		case GameState.PLANNING:
			if(Input.GetKeyDown(KeyCode.Space))
				ChangeGameState(GameState.ACTION);
			break;
		case GameState.ACTION:
			actionTimer -= Time.deltaTime;
			actionTimer = Mathf.Clamp (actionTimer,0,4f);
			float seconds = Mathf.Floor (actionTimer);
			float hundredths = Mathf.Round ((100 * actionTimer) % 100);
			gameDisplay.transform.Find("timer_text").gameObject.GetComponent<Text>().text = string.Format ("{0:0}:{1:00}",seconds,hundredths);
			if(actionTimer <= 0)
				ChangeGameState(GameState.RESOLUTION);
			break;
		default:
			break;
		}
	}
}
