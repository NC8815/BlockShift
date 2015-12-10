using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;


public enum ResolutionState{PUSHING, LOOTING, SCORING, DROPPING, FILLING}

public class GridManager : MonoBehaviour {

	public Color[] colors = {Color.white, Color.red, Color.yellow, Color.green, Color.blue, Color.black};
	List<GameCube> pusherCubes = new List<GameCube> ();
	public List<Coordinates> pusherTrack = new List<Coordinates> ();
	public List<Coordinates> spacesToClear = new List<Coordinates>();
	public List<Coordinates[]> moveOrders = new List<Coordinates[]>();
	public List<LootBonus> lootBonuses = new List<LootBonus>();
	ResolutionState resState;
	float roundScore;
	Coordinates min;
	Coordinates max;
	public GameCube[,] gameGrid;

	void Awake(){
		gameGrid = new GameCube[GameManager.instance.gridSize.x + 4, GameManager.instance.gridSize.y + 4];
		min = GameManager.instance.gridMin;
		max = GameManager.instance.gridMax;
		GeneratePusherTrack ();
	}

	GameCube RelativeTo(Coordinates gridSpace, int deltaX, int deltaY){
		return gameGrid [gridSpace.x + deltaX, gridSpace.y + deltaY];
	}

	public void ResetLoot(){
		lootBonuses.Clear ();
		foreach(Color type in colors){
			lootBonuses.Add (new LootBonus(type));
		}
	}

	Color GetSharedColor (GameCube first, GameCube second){//if both cubes exist and are the same color, return that color, otherwise grey.
		if (first != null && second != null && first.myColor == second.myColor) {
			return first.myColor;
		} else
			return Color.grey;//make sure none of the colors in the spawnable colors is color.grey!
	}

	public void GeneratePusherTrack(){
		for (int x = min.x; x <= max.x; x++) {
			pusherTrack.Add (new Coordinates (x, min.y - 1));
			pusherTrack.Add (new Coordinates (x, max.y + 1));
		}
		for (int y = min.y; y <= max.y; y++) {
			pusherTrack.Add (new Coordinates (min.x - 1, y));
			pusherTrack.Add (new Coordinates (max.x + 1, y));
		}
	}

	public void FillGrid(){
		for (int y = min.y; y <= max.y; y++) {
			for (int x = min.x; x <= max.x; x++) {
				if (gameGrid [x, y] == null) {
					Coordinates space = new Coordinates (x, y);
					List<Color> spawnColors = new List<Color> (colors);
					//remove any colors that would cause the cube to complete a horizontal or vertical group of three.
					spawnColors.Remove (GetSharedColor (RelativeTo (space, -1, 0), RelativeTo (space, -2, 0)));//  XXO
					spawnColors.Remove (GetSharedColor (RelativeTo (space, 1, 0), RelativeTo (space, 2, 0)));  //  OXX
					spawnColors.Remove (GetSharedColor (RelativeTo (space, -1, 0), RelativeTo (space, 1, 0))); //  XOX
					spawnColors.Remove (GetSharedColor (RelativeTo (space, 0, -1), RelativeTo (space, 0, 1))); //  ↑ 
					spawnColors.Remove (GetSharedColor (RelativeTo (space, 0, 1), RelativeTo (space, 0, 2)));  //   ↑
					spawnColors.Remove (GetSharedColor (RelativeTo (space, 0, -1), RelativeTo (space, 0, -2)));//    ↑
					gameGrid [x, y] = GameCube.SpawnCube (space, spawnColors, 0);
				}
			}
		}
		SpawnPushers (GameManager.instance.numPushers);
	}

	Coordinates DirectionToGrid(Coordinates start){
		int xDir = 0;
		int yDir = 0;
		if (Mathf.Clamp (min.x - start.x, -1, 1) == Mathf.Clamp (max.x - start.x, -1, 1)) {
			xDir = Mathf.Clamp (min.x - start.x, -1, 1);
		}
		if (Mathf.Clamp (min.y - start.y, -1, 1) == Mathf.Clamp (max.y - start.y, -1, 1)) {
			yDir = Mathf.Clamp (min.y - start.y, -1, 1);
		}
		return new Coordinates (xDir, yDir);
	}

	public void SpawnPushers(int numPushers){
		pusherCubes.Clear ();
		for (int n = 1; n <= numPushers; n++) {
			Coordinates spawnSpace = pusherTrack [n - 1];
			Coordinates toGrid = DirectionToGrid (spawnSpace);
			List<Color> spawnColors = new List<Color> (colors);
			spawnColors.Remove (GetSharedColor (RelativeTo (spawnSpace, toGrid.x, toGrid.y), RelativeTo (spawnSpace, toGrid.x * 2, toGrid.y * 2)));
			gameGrid [spawnSpace.x, spawnSpace.y] = GameCube.SpawnCube (spawnSpace, spawnColors, n);
			//Debug.Log (gameGrid [spawnSpace.x, spawnSpace.y] != null);
			pusherCubes.Add (gameGrid [spawnSpace.x, spawnSpace.y]);
		}
	}

	public void ClearGrid(){
		foreach (GameCube cube in gameGrid) {
			if(cube != null)
				spacesToClear.Add (cube.myCoords);
		}
		ClearCubes (0);
	}

	public void ClearCubes(float duration){
		spacesToClear.TrimExcess ();
		foreach (Coordinates space in spacesToClear) {
			GameCube cube = gameGrid [space.x, space.y];
			if (cube != null) {
				cube.Despawn(duration);
			}
		}
		spacesToClear.Clear ();
	}

	//first take all the cubes that are supposed to move and set them aside, we can reference them with gameGrid
	//then go through the orders one by one. If the target space is available, make the move. If it isn't, put the cube back
	//where it was and undo any moves that filled that space. keep going until all the move orders have been carried out or canceled.
	/**/
	public void MoveCubes(bool snap, float animDuration){
		//Debug.Log ("moving cubes");
		List<Coordinates> startingSpaces = moveOrders.ConvertAll (new Converter<Coordinates[], Coordinates> (move => move.First ()));
		GameCube[,] temp = gameGrid.Clone() as GameCube[,];
		foreach (Coordinates space in startingSpaces) {
			temp [space.x, space.y] = null;
		}
		while (moveOrders.Count > 0) {
			Coordinates[] move = moveOrders.First ();
			Coordinates start = move.First ();
			Coordinates end = move.Last ();
			GameCube cubeToMove = gameGrid [start.x, start.y];
			if (temp [end.x, end.y] == null) {//if we can go there
				cubeToMove.myCoords = end;//tell the cube it moved
				temp [end.x, end.y] = cubeToMove;//tell the grid the cube moved
			} else {
				temp [start.x, start.y] = cubeToMove;//otherwise, tell the grid the cube didn't move.
			}
			moveOrders.Remove (move);
		}
		foreach (Coordinates space in startingSpaces) {
			gameGrid [space.x, space.y].Move (snap,animDuration);
		}
		gameGrid = temp;//update the grid with the changes.
	}

	IEnumerator Resolution(){
		resState = ResolutionState.PUSHING;
		Debug.Log ("PUSHING");
		pusherCubes.RemoveAll (cube => cube == null);//don't try to move any pushers we deleted.
		pusherCubes = pusherCubes.OrderBy (cube => cube.pushNumber).ToList ();
		foreach (GameCube pusher in pusherCubes) {
			pusher.ChangeToBase ();
			Coordinates searchDir = DirectionToGrid (pusher.myCoords);
			for(Coordinates space = pusher.myCoords; gameGrid[space.x,space.y] != null; space = new Coordinates(space.x + searchDir.x,space.y + searchDir.y)){
				moveOrders.Add(new Coordinates[] {space, new Coordinates(space.x + searchDir.x, space.y + searchDir.y)});
			}
			MoveCubes (false, GameManager.instance.resolutionPushDuration);
			yield return new WaitForSeconds(GameManager.instance.resolutionPushDuration + 0.25f);
		}

		resState = ResolutionState.LOOTING;
		Debug.Log ("LOOTING");
		foreach (Coordinates space in pusherTrack) {
			GameCube cube = gameGrid [space.x, space.y];
			if (cube != null) {
				lootBonuses.Find (o=>o.color == cube.myColor).IncreaseBonus(0.10f);
				Debug.Log (lootBonuses.Find (o=> o.color == cube.myColor).bonusString);
				spacesToClear.Add (cube.myCoords);
			}
		}
		ClearCubes (GameManager.instance.resolutionLootFadeDuration);
		yield return new WaitForSeconds (GameManager.instance.resolutionLootFadeDuration);


		float resolutionScore = 0;
		int roundMultiplierIndex = 0;

		resState = ResolutionState.SCORING;
		Debug.Log ("SCORING");
		do {StartCoroutine(ScoreRound());//calculate base roundScore and animate the despawn, wait until done.
			while(resState == ResolutionState.SCORING){
				yield return null;}//wait until ScoreRound Coroutine finishes (it outs scoring as false when it finishes)

			//modify base score depending on what combo round it is.
			roundMultiplierIndex = Mathf.Clamp (roundMultiplierIndex,0,GameManager.instance.comboBonuses.Length);
			float roundMultiplier = GameManager.instance.comboBonuses[roundMultiplierIndex];
			resolutionScore += roundScore * roundMultiplier;
			roundMultiplierIndex++;
			Debug.Log (resolutionScore);
			
			DropCubes ();//drop remaining cubes to the bottom of the grid.
			FillFall ();//fill the grid, wait until cubes are done falling.
			yield return new WaitForSeconds(GameManager.instance.resolutionFillDuration + 0.25f);
		} while(roundScore > 0);
		GameManager.instance.ChangeScore (resolutionScore);
	}

	IEnumerator ScoreRound(){
		//initialize our list of spaces to check for scoring.
		roundScore = 0;
		List<Coordinates> uncheckedSpaces = new List<Coordinates> ();
		foreach (GameCube cube in gameGrid) {
			if (cube != null)
				uncheckedSpaces.Add (cube.myCoords);
		}

		while(uncheckedSpaces.Count>0){//check spaces until there aren't any to check.
			Coordinates checkSpace = uncheckedSpaces.First ();
			List<Coordinates> scoreGroup = CompleteScoreGroup(checkSpace);//will bare minimum be checkSpace
			//Debug.Log (checkSpace.x + " " + checkSpace.y);
			if(scoreGroup.Count > 2){//scoregroup will always have at least checkspace. If it has more, score the spaces.
				roundScore += (10 * (scoreGroup.Count - 2)) * lootBonuses.Find (o=>o.color == gameGrid[checkSpace.x,checkSpace.y].myColor).bonus;
				foreach(Coordinates space in scoreGroup){
					spacesToClear.Add (space);
				}
				ClearCubes (GameManager.instance.resolutionScoreFadeDuration);
				yield return new WaitForSeconds(GameManager.instance.resolutionScoreFadeDuration +0.2f);
			}
			uncheckedSpaces = uncheckedSpaces.Except (scoreGroup).ToList();
		}
		//let the game know we're done scoring.
		resState = ResolutionState.DROPPING;
		Debug.Log ("DROPPING");
	}

	List<Coordinates> CompleteScoreGroup(Coordinates start){
		List<Coordinates> spacesInGroup = new List<Coordinates> ();
		spacesInGroup.Add (start);
		List<Coordinates> spacesToCheck = new List<Coordinates> (spacesInGroup);
		while (spacesToCheck.Count > 0) {
			Coordinates space = spacesToCheck.First ();
			List<Coordinates> horizontalCheck = CheckNeighbors (space, Vector2.left).Union (CheckNeighbors (space, Vector2.right)).ToList ();
			List<Coordinates> verticalCheck = CheckNeighbors (space, Vector2.up).Union (CheckNeighbors (space, Vector2.down)).ToList ();
			if (horizontalCheck.Count >= 3) {
				spacesToCheck.AddRange (horizontalCheck.Except (spacesInGroup).ToList ());
				spacesInGroup = spacesInGroup.Union (horizontalCheck).ToList ();
			}
			if (verticalCheck.Count >= 3) {
				spacesToCheck.AddRange (verticalCheck.Except (spacesInGroup).ToList ());
				spacesInGroup = spacesInGroup.Union (verticalCheck).ToList ();
			}
			spacesToCheck.Remove (space);
		}
		return spacesInGroup;
	}

	List<Coordinates> CheckNeighbors(Coordinates start, Vector2 direction){
		List<Coordinates> sameColorSpaces = new List<Coordinates> ();
		for (Coordinates space = start; AreSameColor(start,space); space = new Coordinates(space.x + (int)direction.x,space.y + (int)direction.y)) {
			sameColorSpaces.Add (space);
		}
		return sameColorSpaces;
	}

	bool AreSameColor(Coordinates first, Coordinates second){
		GameCube cubeOne = gameGrid[first.x,first.y];
		GameCube cubeTwo = gameGrid[second.x,second.y];
		return (cubeOne != null && cubeTwo != null && cubeOne.myColor == cubeTwo.myColor);
	}

	void DropCubes(){

		for (int y = min.y; y <= max.y; y++) {
			for (int x = min.x; x <= max.x; x++) {
				int checkHeight = y;
				GameCube lowestInColumn = gameGrid [x, checkHeight];
				Coordinates dropTo = new Coordinates(x,y);
				while (lowestInColumn == null && checkHeight <= max.y) {
					checkHeight++;
					lowestInColumn = gameGrid [x, checkHeight];
				}
				if(lowestInColumn != null && lowestInColumn.myCoords != dropTo){
					moveOrders.Add (new Coordinates[] {lowestInColumn.myCoords,dropTo});
				}
			}
			MoveCubes (false, GameManager.instance.resolutionFillDuration);
		}
		resState = ResolutionState.FILLING;
		Debug.Log ("FILLING");
	}

	void FillFall(){
		for (int y = min.y; y <= max.y; y++) {
			for(int x = min.x; x <= max.x; x++){
				if(gameGrid[x,y] == null){
					Coordinates fallTo = new Coordinates(x,y);
					Coordinates fallFrom = new Coordinates(x,max.y+y-min.y);
					gameGrid[x,y] = GameCube.SpawnCube (fallFrom, new List<Color>(colors),0);
					gameGrid[x,y].myCoords = fallTo;
					gameGrid[x,y].Move (false,GameManager.instance.resolutionFillDuration);
					roundScore++;//this looks a little funny, but it ensures that even if nothing is scored, the scoring process is 
					//repeated if cubes got spawned. the actual value of roundscore is useless by the time FillFall is called
				}
			}
		}
		resState = ResolutionState.SCORING;
		Debug.Log ("SCORING");
	}
}

		/*
	 public void MoveCubes(bool snap){
		moveOrders.TrimExcess ();
		List<Coordinates[]> previousMoves = new List<Coordinates[]> ();
		List<Coordinates> startingSpaces = moveOrders.ConvertAll (new Converter<Coordinates[], Coordinates> (move => move.First ()));
		GameCube[,] tempGrid = gameGrid.Clone () as GameCube[,];
		foreach (Coordinates space in startingSpaces) {
			tempGrid [space.x, space.y] = null;
		}
		while (moveOrders.Count > 0) {
			Coordinates[] move = moveOrders.First ();
			moveOrders.Remove (move);
			Coordinates startSpace = move.First ();
			Coordinates endSpace = move.Last ();
			GameCube movingCube = gameGrid [startSpace.x, startSpace.y];
			GameCube inTheWay = tempGrid [endSpace.x, endSpace.y];
			if (inTheWay == null) {
				movingCube.myCoords = endSpace;
				tempGrid [endSpace.x, endSpace.y] = movingCube;
			}//move the cube
			else {
				Coordinates[] badMove = previousMoves.Find (wrong => wrong.Last () == startSpace);
				if (badMove != null) {//undo the bad move
					previousMoves.Remove (badMove);
					moveOrders.Add (badMove);
				}
				tempGrid [startSpace.x, startSpace.y] = movingCube;
			}//the cube keeps its old spot
			previousMoves.Add (move);
		}
		foreach (Coordinates space in startingSpaces) {
			gameGrid [space.x, space.y].Move (snap);
		} //physically move the cubes
		gameGrid = tempGrid;
	}/**/














