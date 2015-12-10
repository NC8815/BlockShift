using UnityEngine;

public struct Coordinates {
	public int x { get; private set; }
	public int y { get; private set; }
	public Vector2 vector {
		get {
			return new Vector2 (x, y);
		}
	}
	
	public Coordinates(int X, int Y) :this() {
		this.x = X;
		this.y = Y;
	}
	//the following code lets me compare the equality of coordinates and apply the == and != operators
	public override int GetHashCode ()
	{
		return x ^ y;
	}

	public override bool Equals(object obj)
	{
		if (obj is Coordinates)
		{
			return this.Equals((Coordinates)obj);
		}
		return false;
	}
	
	public bool Equals(Coordinates C){
		return (C.x == x) && (C.y == y);
	}
	
	public static bool operator ==(Coordinates first, Coordinates second){
		return first.Equals (second);
	}
	
	public static bool operator !=(Coordinates first, Coordinates second){
		return !(first.Equals (second));
	}
}