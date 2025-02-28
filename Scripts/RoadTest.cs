using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml.XPath;

public partial class RoadTest : Node3D
{

	[Export]
	public PackedScene Road { get; set; }

	[Export]
	public PackedScene Destination { get; set; }

	[Export]
	public int Iterations { get; set; }

	public Node3D RoadList;

	public Node3D DestList;

	public MeshInstance3D Bounds;

	public LSystem L;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Prep nodes
		RoadList = GetNode<Node3D>("Roads");
		DestList = GetNode<Node3D>("Destinations");
		Bounds = GetNode<MeshInstance3D>("Bounds");

		// Other setup
		GD.Randomize();
		L = new LSystem(Road, RoadList, DestList);

		randomizeDests();

		interpret(L.buildRoads(Iterations));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustReleased("Reset"))
		{
			GetTree().ReloadCurrentScene();
		}
		if (Input.IsActionJustReleased("Progress")) {
			Iterations++;
			interpret(L.buildRoads(Iterations));
		}
		if (Input.IsActionJustReleased("Regress")) {
			Iterations--;
			interpret(L.buildRoads(Iterations));
		}
	}

	// Go through generated symbols and interperet
	// TODO: make this better lol
	// i want to make this a tree nav or something
	private void interpret(List<ISymbol> axiom) {
		foreach (ISymbol sym in axiom) {
			//GD.Print("Interpreting: ", sym);
			if (sym is Symbol) {
				Symbol castedSym = (Symbol)sym;
				//GD.Print("ID: ", castedSym.ID);
				switch (castedSym.ID) {
					// Create a road
					case "A":
						//GD.Print("Add a road");
						addRoad(castedSym.RoadAttr.Angle, castedSym.RoadAttr.Position);
						break;
				}
			}
			// Branch and save position
			else if (sym is SymBranch) {
				SymBranch castedSym = (SymBranch)sym;
				// Interpret symbols in this branch
				interpret(castedSym.Syms);
			}
		}
	}
	

	/* --------------------------- 
	--------- Road Funcs ---------
	------------------------------ */

	// Draw a road forward
	// Same as rule +F (Rotate by angle, draw a forward line by length)
	private void addRoad(float angle, Vector3 pos) {
		// Create new road and set position
		//GD.Print("Adding a road at: ", pos);
		Road newRoad = (Road)Road.Instantiate();
		//newRoad.Translate(pos);
		newRoad.LookAtFromPosition(pos, getClosestDest(pos).Position);
		RoadList.AddChild(newRoad);
	}


	/* --------------------------- 
	--------- Dest Funcs ---------
	------------------------------ */
	private Node3D getClosestDest(Vector3 pos) {
		Node3D firstChild = (Node3D)DestList.GetChild(0);
		float shortestDist = pos.DistanceTo(firstChild.Position);

		Node3D result = firstChild;

		for (int d = 1; d < DestList.GetChildren().Count; d++) {
			Node3D curChild = (Node3D)DestList.GetChild(d);
			float curDist = pos.DistanceTo(curChild.Position);
			if (curDist < shortestDist) {
				shortestDist = curDist;
				result = curChild;
			}
		}
		return result;
	}
	private void randomizeDests() {
		Vector3 boundsSize = Bounds.GetAabb().Size;
		for (int i = 0; i < 3; i++) {
			Vector3 placePos = new Vector3(GD.RandRange((int)-boundsSize.X/3, (int)boundsSize.X/3), 0, GD.RandRange((int)-boundsSize.Z/3, (int)boundsSize.Z/3));
			addDest(placePos);
		}
	}

	private void addDest(Vector3 pos) {
		Node3D newDest = (Node3D)Destination.Instantiate();
		newDest.Translate(pos);
		DestList.AddChild(newDest);
	}
}
