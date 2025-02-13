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

	public Vector3 CurPos = Vector3.Zero;

	public Stack<Vector3> PushedPos = new Stack<Vector3>();

	public Vector3 CurDir = Vector3.Forward;

	//public List<ISymbol> axiom = new List<ISymbol>{new Symbol("A",1), new Symbol("B", 3), new Symbol("A", 5)};

	public List<ISymbol> generated = new List<ISymbol>{new Symbol("A", 1), new Symbol("B", 3), new Symbol("A", 5)};

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Prep nodes
		RoadList = GetNode<Node3D>("Roads");
		DestList = GetNode<Node3D>("Destinations");
		Bounds = GetNode<MeshInstance3D>("Bounds");

		// Other setup
		GD.Randomize();
		randomizeDests();

		/*
		// Generate
		// Gen 1
		generate(generated);
		// Gen 2
		generate(generated);
		// Interpret
		// On interpretation, call query modules
		interpret(generated);
		*/

		// Testing road functions
		// Add a few forward roads
		for (int i = 0; i < 3; i++) {
			addRoad();
		}
		// Branch from last road
		branchRoad();
		addRoad();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	// TODO: maybe move some of these to a dif file when finished
	/* --------------------------- 
	------- L-system Funcs -------
	------------------------------ */
	// Generates a new string from a given axiom
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

	// Rewrites the given symbol based on its corresponding rule
	private List<ISymbol> rewrite(ISymbol sym, int i) {
		List<ISymbol>result = new List<ISymbol>();
		foreach (string r in Rules.Keys) {
			Rule curRule = Rules[r];
			// Check if symbol matches rule and any conditions
			if (sym is Symbol) {										// General rewrites
				Symbol castedSym = (Symbol) sym;
				if (curRule.checkSymbol(castedSym) && curRule.checkCond()) {
					result = curRule.genOutput();
				}
			} else if (sym is SymBranch) {								// Branch rewrites
				SymBranch castedSym = (SymBranch)sym;					// Sym to rewrite
				SymBranch newBranch = new SymBranch();					// Resulting branch
				List<ISymbol> branchResults;							// Store rewritten symbols here
				for (int j = 0; j < castedSym.Syms.Count; j++) {		// Recurisvely rewrite syms in branch
					branchResults = rewrite(castedSym.Syms[j], j);
					foreach (ISymbol genSym in branchResults) {			// Add to resulting branch's syms
						newBranch.Syms.Add(genSym);
					}
				}
				result.Add(newBranch);
				return result;
			}
		}
		return result;
	}

	// Go through generated symbols and interperet
	// TODO: make this better lol
	// i want to make this a tree nav or something
	private void interpret(List<ISymbol> axiom) {
		foreach (ISymbol sym in axiom) {
			//GD.Print("Interpreting: ", sym);
			if (sym is Symbol) {
				Symbol castedSym = (Symbol)sym;
				GD.Print("ID: ", castedSym.ID);
				switch (castedSym.ID) {
					// Create a road
					case "B":
						GD.Print("Add a road");
						addRoad();
						break;
					// Insertion query
					case "?I":
						GD.Print("Query");
						break;
				}
			}
			// Branch and save position
			else if (sym is SymBranch) {
				SymBranch castedSym = (SymBranch)sym;
				GD.Print("Branch: ", castedSym);
				// Save position
				PushedPos.Push(CurPos);
				// Interpret symbols in this branch
				interpret(castedSym.Syms);
				// Pop back to position
				CurPos = PushedPos.Pop();
			}
		}
	}

	private void globalGoals() {

	}

	private void localConstraints() {

	}

	// Closest destination query, returns yaw needed to face thingy
	// Does NOT do any rotating on its own
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
		return yaw;
	}

	private void insertQuery() {
		
	}

	/* --------------------------- 
	--------- Road Funcs ---------
	------------------------------ */

	// Draw a road forward
	private void addRoad() {
		GD.Print("Adding road forward");
		// Create new road and set position
		Road newRoad = (Road)Road.Instantiate();
		newRoad.Translate(CurPos);

		//newRoad.Rotate(Vector3.Up, angle);
		RoadList.AddChild(newRoad);
		GD.Print("My forward dir: ", CurDir);
		GD.Print("My forward pos: ", CurPos);
		// Increment position to next position
		CurPos = CurPos + (newRoad.MyMesh.GetAabb().Size * CurDir);
	}
	
	// Branch a road
	private void branchRoad() {
		GD.Print("Branching road");
		// Create new road and set position
		Road newRoad = (Road)Road.Instantiate();
		newRoad.Translate(CurPos);

		// TODO: make new angled version for add road
		// If branching, apply angle
		float angle = getClosestDestAngle(newRoad);
		newRoad.Rotate(Vector3.Up, angle);

		CurDir = CurDir.Rotated(Vector3.Up, angle);
		GD.Print("My branching dir: ", CurDir);
		GD.Print("My branching pos: ", CurPos);


		//newRoad.Rotate(Vector3.Up, angle);
		RoadList.AddChild(newRoad);

		// Increment position to next position
		CurPos = CurPos + (newRoad.MyMesh.GetAabb().Size * CurDir);
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
