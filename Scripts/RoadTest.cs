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
		L = new LSystem(RoadList, DestList, Road);

		randomizeDests();

		generateRoads();
	}

	private void generateRoads() {
		for (int i = 0; i < Iterations; i++) {
			// clear road list
			foreach (Node roadChild in RoadList.GetChildren()) {
				roadChild.QueueFree();
			}
			interpret(L.buildRoads(i));
		}
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
			generateRoads();
		}
		if (Input.IsActionJustReleased("Regress")) {
			Iterations--;
			generateRoads();
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
						addRoad(castedSym.RoadAttr.Position, castedSym.RoadAttr.LookPosition);
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
	private void addRoad(Vector3 pos, Vector3 lookPos) {
		// Create new road and set position
		//GD.Print("Adding a road at: ", pos);
		Road newRoad = (Road)Road.Instantiate();
		//newRoad.Translate(pos);

		if (!pos.Equals(lookPos)) {
			newRoad.LookAtFromPosition(pos, lookPos);
		} else {
			newRoad.Translate(pos);
		}
		RoadList.AddChild(newRoad);
	}


	/* --------------------------- 
	--------- Dest Funcs ---------
	------------------------------ */

	private void randomizeDests() {
		Vector3 boundsSize = Bounds.GetAabb().Size;
		for (int i = 0; i < 1; i++) {
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
