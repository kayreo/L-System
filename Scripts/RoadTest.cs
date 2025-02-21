using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class RoadTest : Node3D
{
	public Dictionary<String, Rule> Rules = new Dictionary<String, Rule>
	{
		{"RuleRBranch", new RuleRBranch()},
		{"RuleRDel", new RuleRDel()},
		{"RuleB", new RuleB()},
		{"RuleBDel", new RuleBDel()},
		{"RuleI", new RuleI()},
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

	// begin with a basic road symbol and an insertion query to determine if legal place to put road
	public List<ISymbol> generated = new List<ISymbol>{new Symbol("R", 0, 0, -1, StateType.UNASSIGNED), new Symbol("?I", 0, 0, 0, StateType.UNASSIGNED)};

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

		addRoad();

		branchRoad();

		addRoad();

		branchRoad();


		//for (int i = 0; i < 3; i++) {
		// 	// Global goals generation
		// 	// First fill symbol vals using global goals, where you call queries to update your goals
		// 	globalGoals(generated);

		// 	// local constraints generation
		// 	// Next rewrite calls local constraints, which culls or rewrites rules based on queries made in global goals
		// 	localConstraints(generated);
		// 	GD.Print("Local Constraints: ", string.Join(", ", generated));

		// 	// Generate ideal successor for next iteration
		// 	generate(generated);
		// }
		// // Interpret
		// // finally, interpret rule and build road!
		// interpret(generated);
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
			List<ISymbol> rewroteSym = rewrite(axiom, curSymbol, i);
			if (rewroteSym!= null) {
				results.Add(rewroteSym);
			}
		}

		// Store flattened
		generated = results.SelectMany(subList => subList).ToList();
		GD.Print("Generated: ", string.Join("", generated));
	}

	// Rewrites the given symbol based on its corresponding rule
	private List<ISymbol> rewrite(List<ISymbol> axiom, ISymbol sym, int i) {
		List<ISymbol>result = new List<ISymbol>();
		foreach (string r in Rules.Keys) {
			Rule curRule = Rules[r];
			// Check if symbol matches rule and any conditions
			if (sym is Symbol) {				
				// General rewrites
				Symbol castedSym = (Symbol) sym;

				// Check LC and RC
				// Check if an R symbol has an ?I query to its right
				if (castedSym.ID == "R" && axiom[i + 1] is Symbol) {
					Symbol genCast = (Symbol)axiom[i + 1];
					if (genCast.ID == "?I") {
						// Pass inquery's state to road to check
						castedSym.State = genCast.State;
					}
				}
				// Check if an ?I query has an R symbol to the left
				else if (castedSym.ID == "?I" && axiom[i - 1] is Symbol) {
					Symbol genCast = (Symbol)axiom[i - 1];
					if (genCast.ID == "R") {
					}
				}
				if (curRule.checkSymbol(castedSym) && curRule.checkCond()) {
					result = curRule.genOutput();

					//globalGoals(castedSym.RoadAttr, castedSym.RuleAttr)
					// If this is the branch rule, call global goals and populate params
					if (r == "RuleBBranch") {
						// Use these for global goals param calls
						int roA = castedSym.RoadAttr;
						int ruA = castedSym.RuleAttr;
						// Index 0: +F
						// Index 1: B1, attrs 1
						// Index 2: B2, attrs 2
						// Index 3: R, attrs 0

						foreach (ISymbol outSym in result) {
							Symbol castedOutput = (Symbol)outSym;
							// grab from global vars
						}

					}

				}
			} else if (sym is SymBranch) {								// Branch rewrites
				SymBranch castedSym = (SymBranch)sym;					// Sym to rewrite
				SymBranch newBranch = new SymBranch();					// Resulting branch
				List<ISymbol> branchResults;							// Store rewritten symbols here
				for (int j = 0; j < castedSym.Syms.Count; j++) {		// Recurisvely rewrite syms in branch
					branchResults = rewrite(castedSym.Syms, castedSym.Syms[j], j);
					if (branchResults != null) {
						foreach (ISymbol genSym in branchResults) {		// Add to resulting branch's syms
							newBranch.Syms.Add(genSym);
						}
					}
				}
				if (newBranch.Syms.Count > 0) {
					result.Add(newBranch);
				}
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
				//GD.Print("ID: ", castedSym.ID);
				switch (castedSym.ID) {
					// Create a road
					case "A":
						//GD.Print("Add a road");
						addRoad();
						break;
					case "B":
						//GD.Print("Branch road");
						branchRoad();
						break;
					// Insertion query
					case "?I":
						// in global goals, make a query
						// based on result of the query, update your rule
						//GD.Print("Query");
						break;
				}
			}
			// Branch and save position
			else if (sym is SymBranch) {
				SymBranch castedSym = (SymBranch)sym;
				branchRoad();
				// Save position
				PushedPos.Push(CurPos);
				PushedAng.Push(CurAngle);
				// Interpret symbols in this branch
				interpret(castedSym.Syms);
				// Pop back to position and angle
				CurPos = PushedPos.Pop();
				CurAngle = PushedAng.Pop();
			}
		}
	}

	// have roads trend toward different "goals"
	// Their attributes are initialized according to the global goals
	// which returns an array of attributes (pDel[0-2] for branch delay
	// and deletion, pRuleAttr[0-2] for rule-specific attributes and
	// pRoadAttr[0-2] for road data, e.g. length, angle, etc).

	// TODO: where do i call thisssssss how do i update symbol params
	// can i put this in the rule for branch gen
	// I do not think i understand this
	// populate the global vars
	private void globalGoals(List<ISymbol> axiom) {
		// Branch delay and deletion
		List<int> pDel = new List<int>{
			0, 0, 0
		};

		// Rule-specific attributes
		List<int> pRuleAttr = new List<int>{
			0, 0, 0
		};

		// Road data (length, angle)
		List<float> pRoadAttr = new List<float>{
			50f, getClosestDestAngle(CurPos), 0f
		};

	}

	// TODO: need to add state changing and param adjustment based on goals
	// For now, just set everything to true
	private void localConstraints(List<ISymbol> axiom) {
		for (int i = 0; i < axiom.Count; i++) {
			if (axiom[i] is Symbol) {
				Symbol castedSym = (Symbol)axiom[i];
				if (castedSym.ID == "?I") {
					castedSym.State = StateType.SUCCESS;
				}
				axiom[i] = castedSym;
			} else if (axiom[i] is SymBranch) {
				// If branch, recursively update symbols in branch
				SymBranch castedBranch = (SymBranch)axiom[i];
				localConstraints(castedBranch.Syms);
			}
		}
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
	private float getClosestDestAngle(Vector3 pos) {
		GD.Print("Checking position: ", pos);
		GD.Print("Current direction: ", CurDir);

		Node3D firstChild = (Node3D)DestList.GetChild(0);
		float shortestDist = pos.DistanceTo(firstChild.Position);
		GD.Print("dest pos: ", firstChild.Position);
		float angle = CurDir.SignedAngleTo(firstChild.Position, Vector3.Up);
		GD.Print("init angle: ", angle);
		
		// Find closest destination to road
		for (int d = 1; d < DestList.GetChildren().Count; d++) {
			Node3D curChild = (Node3D)DestList.GetChild(d);
			float curDist = pos.DistanceTo(curChild.Position);
			if (curDist < shortestDist) {
				GD.Print("Getting closest : ", d);
				angle = CurDir.SignedAngleTo(curChild.Position, Vector3.Up);
				GD.Print("closest angle: ", angle);
				shortestDist = curDist;
			}
		}
		return angle;
	}

	/* --------------------------- 
	--------- Road Funcs ---------
	------------------------------ */

	// Draw a road forward
	private void addRoad() {
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
		// Create new road and set position
		Road newRoad = (Road)Road.Instantiate();
		newRoad.Translate(CurPos);

		// If branching, apply angle
		CurAngle = getClosestDestAngle(CurPos);
		GD.Print("Angle: ", CurAngle);
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
