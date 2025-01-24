using Godot;
using System;
using System.Runtime.InteropServices;

[Tool]
public partial class TerrainCollision : CollisionShape3D
{
	[Signal]
	public delegate void UpdateCollisionEventHandler(Godot.Collections.Array data);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		UpdateCollision += OnUpdateCollision;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnUpdateCollision(Godot.Collections.Array data) {
		GD.Print("Updating data");
	}


}
