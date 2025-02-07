using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class RoadTest : Node3D
{
	public Dictionary<String, Rule> Rules = new Dictionary<String, Rule>
	{
		{"RuleA", new RuleA()},
		{"RuleB", new RuleB()}
	};

	[Export]
	public PackedScene Road { get; set; }

	[Export]
	public PackedScene Destination { get; set; }

	public Node3D RoadList;

	public Node3D DestList;

	public MeshInstance3D Bounds;

	public List<Symbol> axiom = new List<Symbol>{new Symbol("A",1), new Symbol("B", 3), new Symbol("A", 5)};

	public List<ISymbol> generated;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Randomize();
		RoadList = GetNode<Node3D>("Roads");
		DestList = GetNode<Node3D>("Destinations");
		Bounds = GetNode<MeshInstance3D>("Bounds");

		randomizeDests();

		// Generate
		generate();
		// Interpret
		// On interpretation, call query modules
		interpret();

		//addRoad();
	}

	private void generate() {
		List<List<ISymbol>> results = new List<List<ISymbol>>();

		// Go through each symbol and evaluate
		for (int i = 0; i < axiom.Count; i++) {
			Symbol curSymbol = axiom[i];
			foreach (string r in Rules.Keys) {
				Rule curRule = Rules[r];
				// Check if symbol matches rule and conditions
				if (curRule.checkSymbol(curSymbol) && curRule.checkCond()) {
					GD.Print("Using rule");
					results.Add(curRule.genOutput());
				}
			}
		}

		// Print new rule
		foreach (List<ISymbol> curLevel in results) {
			GD.Print("Level: ", String.Join(", ", curLevel));
		}

		// Store flattened
		generated = results.SelectMany(subList => subList).ToList();
		GD.Print("Flattened: ", String.Join("", generated));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	// Go through generated symbols and interperet
	// TODO: make this better lol
	// i want to make this a tree nav or something
	private void interpret() {
		foreach (ISymbol sym in generated) {
			//GD.Print("Interpreting: ", sym);
			if (sym is Symbol) {
				Symbol castedSym = (Symbol)sym;
				GD.Print("ID: ", castedSym.ID);
				if (castedSym.ID == "B") {
					GD.Print("Add a road");
					addRoad();
				}
				else if (castedSym.ID == "A") {
					GD.Print("Query");
				}
			}
			else if (sym is SymBranch) {
				SymBranch castedSym = (SymBranch)sym;
				GD.Print("Branch: ", castedSym);
			}
		}
	}

	private void insertQuery() {

	}

	private void globalGoals() {

	}

	private void localConstraints() {

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
