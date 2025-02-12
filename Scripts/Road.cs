using Godot;
using System;

public partial class Road : Node3D
{
	public MeshInstance3D MyMesh;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		MyMesh = (MeshInstance3D)GetNode("StaticBody3D").GetChild(0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

}
