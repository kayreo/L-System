using Godot;
using System;

[Tool]
public partial class RoadTest : Node3D
{
	[Export]
	public PackedScene Road { get; set; }

	[Export]
	public PackedScene Destination { get; set; }

	public Node3D RoadList;

	public Node3D DestList;

	public MeshInstance3D Bounds;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Randomize();
		RoadList = GetNode<Node3D>("Roads");
		DestList = GetNode<Node3D>("Destinations");
		Bounds = GetNode<MeshInstance3D>("Bounds");
		//addRoad();
		randomizeDests();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void randomizeDests() {
		Vector3 boundsSize = Bounds.GetAabb().Size;
		for (int i = 0; i < 3; i++) {
			Vector3 placePos = new Vector3(GD.RandRange((int)-boundsSize.X/3, (int)boundsSize.X/3), 0, GD.RandRange((int)-boundsSize.Z/3, (int)boundsSize.Z/3));
			GD.Print("Placing here: ", placePos);
			addDest(placePos);
		}
	}

	private void addRoad() {
		Road newRoad = (Road)Road.Instantiate();
		RoadList.AddChild(newRoad);
	}

	private void addDest(Vector3 pos) {
		Node3D newDest = (Node3D)Destination.Instantiate();
		newDest.Translate(pos);
		GD.Print("Init here: ", newDest.Position);
		DestList.AddChild(newDest);
	}
}
