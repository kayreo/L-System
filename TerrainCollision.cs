using Godot;
using System;
using System.Runtime.InteropServices;

[Tool]
public partial class TerrainCollision : CollisionShape3D
{
	[Signal]
	public delegate void UpdateCollisionEventHandler(Vector3[] data, float size);

	public TerrainCollision() {
		UpdateCollision += OnUpdateCollision;
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnUpdateCollision(Vector3[] data, float size) {
		ConcavePolygonShape3D newShape = new ConcavePolygonShape3D();
		newShape.SetFaces(data);
		Shape = newShape;
	}


}
