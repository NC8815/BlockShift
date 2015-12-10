using UnityEngine;
using System.Collections;

public class LootBonus {
	public Color color { get; private set; }
	public float bonus { get; private set; }
	public string bonusString { get; private set; }

	public LootBonus(Color newColor){
		color = newColor;
		bonus = 1;
		bonusString = "0%";
	}

	public void IncreaseBonus(float percent){
		bonus += percent;
		bonusString = ((int)Mathf.Floor ((this.bonus - 1) * 100)).ToString () + "%";
		Debug.Log (bonus);
	}
}