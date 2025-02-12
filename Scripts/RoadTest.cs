using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;

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

	public Vector3 CurPos = Vector3.Zero;

	//public List<ISymbol> axiom = new List<ISymbol>{new Symbol("A",1), new Symbol("B", 3), new Symbol("A", 5)};

	public List<ISymbol> generated = new List<ISymbol>{new Symbol("A",1), new Symbol("B", 3), new Symbol("A", 5)};

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Randomize();
		RoadList = GetNode<Node3D>("Roads");
		DestList = GetNode<Node3D>("Destinations");
		Bounds = GetNode<MeshInstance3D>("Bounds");

		randomizeDests();

		// Generate
		// Gen 1
		generate(generated);
		// Gen 2
		generate(generated);
		// Interpret
		// On interpretation, call query modules
		interpret();

		//addRoad();
	}

	private List<ISymbol> rewrite(ISymbol sym, int i) {
		List<ISymbol>result = new List<ISymbol>();
		foreach (string r in Rules.Keys) {
			Rule curRule = Rules[r];
			// Check if symbol matches rule and conditions
			// Evaluate if a symbol or a branch
			if (sym is Symbol) {
				Symbol castedSym = (Symbol) sym;
				if (curRule.checkSymbol(castedSym) && curRule.checkCond()) {
					result = curRule.genOutput();
				}
			} else if (sym is SymBranch) {
				SymBranch castedSym = (SymBranch)sym;
				List<ISymbol> branchResults = new List<ISymbol>();
				for (int j = 0; j < castedSym.Syms.Count; j++) {
					branchResults = (rewrite(castedSym.Syms[j], j));
				}
				// buggy, need to return a branch
				return branchResults;
			}
		}
		return result;
	}

	private void generate(List<ISymbol> axiom) {
		List<List<ISymbol>> results = new List<List<ISymbol>>();

		// Go through each symbol and evaluate
		for (int i = 0; i < axiom.Count; i++) {
			ISymbol curSymbol = axiom[i];
			results.Add(rewrite(curSymbol, i));
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
					addRoad(CurPos);
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

	private float getClosestDestAngle(Road road) {
		Node3D firstChild = (Node3D)DestList.GetChild(0);
		Vector3 shortestDist = road.Position - firstChild.Position;
		Vector3 dir = (firstChild.Position - road.Position).Normalized();
		float yaw = Mathf.Atan2(dir.X, dir.Z);
		Node3D closestChild = (Node3D)DestList.GetChild(0);
		// Find closest destination to road
		for (int d = 1; d < DestList.GetChildren().Count; d++) {
			Node3D curChild = (Node3D)DestList.GetChild(d);
			Vector3 curDist = road.Position - curChild.Position;
			if (curDist < shortestDist) {
				dir = (curChild.Position - road.Position).Normalized();
				yaw = Mathf.Atan2(dir.X, dir.Z);
				GD.Print("Closest child is: ", d);
				GD.Print("Rotating: ", yaw);
				closestChild = curChild;
			}
		}
		//road.Rotate(Vector3.Up, yaw);
		//road.LookAtFromPosition(road.Position, closestChild.Position);
		return yaw;
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

	private void addRoad(Vector3 pos) {
		// Create new road and set position
		Road newRoad = (Road)Road.Instantiate();
		newRoad.Translate(pos);

		// TODO: make new angled version for add road
		// If branching, apply angle
		float angle = getClosestDestAngle(newRoad);
		newRoad.Rotate(Vector3.Up, angle);


		//newRoad.Rotate(Vector3.Up, angle);
		RoadList.AddChild(newRoad);


		// Increment position to next position
		CurPos = new Vector3(0, 0, pos.Z + newRoad.MyMesh.GetAabb().Size.Z);
	}

	private void addDest(Vector3 pos) {
		Node3D newDest = (Node3D)Destination.Instantiate();
		newDest.Translate(pos);
		GD.Print("Init here: ", newDest.Position);
		DestList.AddChild(newDest);
	}
}
