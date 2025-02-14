using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class RoadTest : Node3D
{
	public Dictionary<String, Rule> Rules = new Dictionary<String, Rule>
	{
		{"RuleRBranch", new RuleRBranch()},
		{"RuleRDel", new RuleRDel()},
		{"RuleIDel", new RuleIDel()}
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

	public Stack<float> PushedAng = new Stack<float>();

	public Vector3 CurDir = Vector3.Forward;

	public float CurAngle = 0.0f;

	//public List<ISymbol> axiom = new List<ISymbol>{new Symbol("A",1), new Symbol("B", 3), new Symbol("A", 5)};

	// begin with a basic road symbol and an insertion query to determine if legal place to put road
	public List<ISymbol> generated = new List<ISymbol>{new Symbol("R", 0, 0, -1, StateType.UNASSIGNED), new Symbol("?I", 0, 0, 0, StateType.UNASSIGNED)};
	//new List<ISymbol>{new Symbol("A", new Dictionary<Variant, Variant>(1)), new Symbol("B", new Dictionary<Variant, Variant>(3)), new Symbol("A", new Dictionary<Variant, Variant>(5))};

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

		
		// Generate ideal successor
		generate(generated);

		// Global goals generation
		// First fill symbol vals using global goals, where you call queries to update your goals
		globalGoals(generated);

		// local constraints generation
		// Next rewrite calls local constraints, which culls or rewrites rules based on queries made in global goals
		localConstraints(generated);
		
		// Interpret
		// finally, interpret rule and build road!
		interpret(generated);
		

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
			if (sym is Symbol) {				
				// General rewrites
				Symbol castedSym = (Symbol) sym;

				// Check LC and RC
				// Check if an R symbol has an ?I query to its right
				if (castedSym.ID == "R" && generated[i + 1] is Symbol) {
					Symbol genCast = (Symbol)generated[i + 1];
					if (genCast.ID == "?I") {
						// Pass inquery's state to road to check
						castedSym.State = genCast.State;
					}
				}
				// Check if an ?I query has an R symbol to the left
				else if (castedSym.ID == "?I" && generated[i - 1] is Symbol) {
					Symbol genCast = (Symbol)generated[i - 1];
					if (genCast.ID == "R") {
					}
				}
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
						// in global goals, make a query
						// based on result of the query, update your rule
						GD.Print("Query");
						break;
				}
			}
			// Branch and save position
			else if (sym is SymBranch) {
				SymBranch castedSym = (SymBranch)sym;
				branchRoad();
				GD.Print("Branch: ", castedSym);
				// Save position
				PushedPos.Push(CurPos);
				PushedAng.Push(CurAngle);
				// Interpret symbols in this branch
				interpret(castedSym.Syms);
				// Pop back to position
				CurPos = PushedPos.Pop();
			}
		}
	}

	// have roads trend toward different "goals"
	// Their attributes are initialized according to the global goals
	// which returns an array of attributes (pDel[0-2] for branch delay
	// and deletion, pRuleAttr[0-2] for rule-specific attributes and
	// pRoadAttr[0-2] for road data, e.g. length, angle, etc).
	private void globalGoals(List<ISymbol> axiom) {

	}

	private void localConstraints(List<ISymbol> axiom) {

	}

	/* --------------------------- 
	-------- Query Funcs ---------
	------------------------------ */
	
	// Queries whether the road can be inserted
	// checks for legal terrain, or if road will intersect with water, mountains, etc.
	private bool insertQuery() {
		return true;
	}

	// TODO: branches much have a minimum angle difference (ie, road branches must be 45 degrees apart)
	// Closest destination query, returns yaw needed to face thingy
	// Does NOT do any rotating on its own
	private float getClosestDestAngle(Road road) {
		Node3D firstChild = (Node3D)DestList.GetChild(0);
		Vector3 shortestDist = road.Position - firstChild.Position;
		Vector3 dir = (firstChild.Position - road.Position).Normalized();
		float yaw = road.Position.AngleTo(dir);
		// Find closest destination to road
		for (int d = 1; d < DestList.GetChildren().Count; d++) {
			Node3D curChild = (Node3D)DestList.GetChild(d);
			Vector3 curDist = road.Position - curChild.Position;
			if (curDist < shortestDist) {
				dir = (curChild.Position - road.Position).Normalized();
				yaw = road.Position.AngleTo(dir);
			}
		}
		return yaw;
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
		newRoad.Rotate(Vector3.Up, CurAngle);
		RoadList.AddChild(newRoad);

		// Increment position to next position
		CurPos += new Vector3(50, 0, 50) * CurDir;
	}
	
	// Branch a road
	private void branchRoad() {
		GD.Print("Branching road");
		// Create new road and set position
		Road newRoad = (Road)Road.Instantiate();
		newRoad.Translate(CurPos);

		// If branching, apply angle
		CurAngle = getClosestDestAngle(newRoad);
		newRoad.Rotate(Vector3.Up, CurAngle);

		CurDir = CurDir.Rotated(Vector3.Up, CurAngle).Normalized();

		//newRoad.Rotate(Vector3.Up, angle);
		RoadList.AddChild(newRoad);

		// Increment position to next position
		CurPos += new Vector3(50, 0, 50) * CurDir;
	}


	/* --------------------------- 
	--------- Dest Funcs ---------
	------------------------------ */
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
