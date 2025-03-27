using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LSystem
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

	public Node3D RoadList;

	public Node3D DestList;

	public MeshInstance3D Bounds;

	public Vector3 CurPos = Vector3.Zero;

	public Stack<Vector3> PushedPos = new Stack<Vector3>();

	public Stack<float> PushedAng = new Stack<float>();

	public Vector3 CurDir = Vector3.Forward;

	public float SeaLevel;

	public float CurAngle = 0.0f;

	// Parameter lists to be populated by global goals
	List<int> delays;
	List<RuleAttributes> ruleAttrs;
	List<RoadAttributes> roadAttrs;
	List<Vector3> roadLocations;
    List<Vector3> roadDirections;
	Mesh heightMap;


	Dictionary<string, RuleAttributes> rules = new Dictionary<string, RuleAttributes> {
		{"None", new RuleAttributes(Mathf.DegToRad(-90f), Mathf.DegToRad(90f))}
	};

	// Init attributes
	RuleAttributes initRuleAttr;
	RoadAttributes initRoadAttr = new RoadAttributes(Vector3.Zero, Vector3.Forward, 0f, new Vector3(50.0f, 0.0f, 50.0f), Vector3.Zero, RoadType.NONE);

	// begin with a basic road symbol and an insertion query to determine if legal place to put road
	public List<ISymbol> generated;

	public LSystem(Node3D rList, Node3D dList, Mesh hm, string Rule, float seaLevel) {
		DestList = dList;
		RoadList = rList;
		heightMap = hm;
		SeaLevel = seaLevel;
		if (rules.ContainsKey(Rule)) {
			initRuleAttr = rules[Rule];
		} else {
			initRuleAttr = rules["None"];
		}
		delays = new List<int>();
		ruleAttrs = new List<RuleAttributes>();
		roadAttrs = new List<RoadAttributes>();
		roadLocations = new List<Vector3>();	
        roadDirections = new List<Vector3>();
	}

	// Builds the list of symbols that will then be used to build the road system
	public List<ISymbol> buildRoads(int iterations) {
		roadLocations = new List<Vector3>();
        roadDirections = new List<Vector3>();
		delays = new List<int>();
		ruleAttrs = new List<RuleAttributes>();
		roadAttrs = new List<RoadAttributes>();

		//GD.Print("Running with an iteration of : " + Iterations);
		generated = new List<ISymbol>{new Symbol("R", 0, initRuleAttr, initRoadAttr, StateType.UNASSIGNED), new Symbol("?I", 0, new RuleAttributes(), initRoadAttr, StateType.UNASSIGNED)};

		// First fill symbol vals using global goals, where you call queries to update your goals
		// local constraints generation
		// Next rewrite calls local constraints, which culls or rewrites rules based on queries made in global goals
		for (int i = 0; i < iterations; i++) {
			// GD.Print("----------------------------------------------------");
			// GD.Print("--------------------", "ITERATION: ", i, "--------------------");
			// GD.Print("----------------------------------------------------");
			

			localConstraints(generated);
			// Generate ideal successor for next iteration
			generate(generated);
			
		}
		//GD.Print("Generated: ", string.Join("\n\n", generated));
        return generated;
	}

	/* --------------------------- 
	------- L-system Funcs -------
	------------------------------ */
	// Generates a new string from a given axiom
	private void generate(List<ISymbol> axiom) {
		List<List<ISymbol>> results = new List<List<ISymbol>>();
		// Go through each symbol and evaluate
		for (int i = 0; i < axiom.Count; i++) {
			ISymbol curSymbol = axiom[i];
			if (curSymbol is Symbol && ((Symbol)curSymbol).ID == "A") {
				results.Add(new List<ISymbol>{curSymbol});
			}
			else {
				List<ISymbol> rewroteSym = rewrite(axiom, curSymbol, i);
				if (rewroteSym!= null) {
					results.Add(rewroteSym);
				}
			}
		}

		// Store flattened
		generated = results.SelectMany(subList => subList).ToList();	
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
				if (castedSym.ID == "A" || castedSym.ID == "Br" || castedSym.ID == "T") {
					result = new List<ISymbol>{castedSym};
				}
				// Check LC and RC
				// Check if an R symbol has an ?I query to its right
				if (castedSym.ID == "R" && axiom[i + 1] is Symbol) {
					Symbol genCast = (Symbol)axiom[i + 1];
					if (genCast.ID == "?I") {
						// Pass inquery's state to road to check
						castedSym.State = genCast.State;
					}
				}
				if (curRule.checkSymbol(castedSym) && curRule.checkCond()) {
					result = curRule.genOutput();
					//globalGoals(castedSym.RoadAttr, castedSym.RuleAttr)
					// If this is the branch rule, call global goals and populate params
					if (r == "RuleRBranch") {
						// Use these for global goals param calls
						RoadAttributes roA = castedSym.RoadAttr;
						RuleAttributes ruA = castedSym.RuleAttr;
						globalGoals(ruA, roA);
						Symbol drawRoad = (Symbol)result[0];
						drawRoad.RoadAttr = castedSym.RoadAttr;
						drawRoad.RuleAttr = castedSym.RuleAttr;
						result[0] = drawRoad;
						for (int j = 1; j < result.Count - 1; j++) {
							Symbol castedOutput = (Symbol)result[j];
							castedOutput.Del = delays[j - 1];
							castedOutput.RoadAttr = roadAttrs[j - 1];
							result[j] = castedOutput;
						}
						Symbol iModule = (Symbol)result[result.Count - 1];
						iModule.RoadAttr = roadAttrs[2];
						result[result.Count - 1] = iModule;
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

	/*
	Road attributes and rule attributes and delay attributes r attributes for 
	ONE ROAD OBJECT so road attribute [0] would get attributes for road [0]

	Call global goals and when find an inquiry module call it in global goals to populate thing
	Call local constraints and delete and adjust modules
	Interpret fully trimmed and edited modules and build road
	*/

	// have roads trend toward different "goals"
	// Their attributes are initialized according to the global goals
	// which returns an array of attributes (pDel[0-2] for branch delay
	// and deletion, pRuleAttr[0-2] for rule-specific attributes and
	// pRoadAttr[0-2] for road data, e.g. length, angle, etc).
	
	// At this set of attributes, generate a set of 3 delays, ruleAttrs, and roadAttrs
	// Then, use these values back in genGoals
	private void globalGoals(RuleAttributes curRuleAttr, RoadAttributes curRoadAttr) {
		delays.Clear();
		ruleAttrs.Clear();
		roadAttrs.Clear();

		int delayB1 = 3;
		int delayB2 = 3;
		int delayR = 1;

		RoadType nextRoadType = RoadType.NONE;

		// Adjust angle and dir for road
		float nextAngR = getClosestDestAngle(curRoadAttr.Position, curRoadAttr.Direction);
		
		if (nextAngR < curRuleAttr.MinAngle || nextAngR > curRuleAttr.MaxAngle) {
			delayR = -1;
		}
		Vector3 nextDirR = curRoadAttr.Direction.Rotated(Vector3.Up, nextAngR).Normalized();

		// Increment position to next position
		Vector3 nextPos = curRoadAttr.Position + curRoadAttr.RoadSize * nextDirR;

		// If the next position hits terrain, turn it into a tunnel
		if (doesGroundIntersect(nextPos)) {
			//GD.Print("Need a tunnel");
			nextRoadType = RoadType.TUNNEL;
		}
		// If the next position is over water, turn it into a bridge
		else if (isAboveWater(nextPos)) {
			//GD.Print("Need a bridge");
			nextRoadType = RoadType.BRIDGE;
		}

		// Adjust angle and dir for branch 1
		float nextAngB1 = curRoadAttr.CurAngle + (float)GD.RandRange(curRuleAttr.MinAngle, curRuleAttr.MaxAngle);
		Vector3 nextDirB1 = curRoadAttr.Direction.Rotated(Vector3.Up, nextAngB1).Normalized();

		if (nextAngB1 < curRuleAttr.MinAngle || nextAngB1 > curRuleAttr.MaxAngle) {
			delayB1 = -1;
		}

		// Adjust angle and dir for branch 2
		float nextAngB2 = curRoadAttr.CurAngle - (float)GD.RandRange(curRuleAttr.MinAngle, curRuleAttr.MaxAngle);
		Vector3 nextDirB2 = curRoadAttr.Direction.Rotated(Vector3.Up, nextAngB2).Normalized();

		if (nextAngB2 < curRuleAttr.MinAngle || nextAngB2 > curRuleAttr.MaxAngle) {
			delayB2 = -1;
		}

		// Generate delays
		// arbitrary rn
		delays.Add(delayB1);
		delays.Add(delayB2);
		delays.Add(delayR);

		// Generate ruleAttr
		for (int i = 0; i < 3; i++) {
			RuleAttributes ruleAttr = new RuleAttributes();
			ruleAttrs.Add(ruleAttr);
		}

		// Generate roadAttr
		// Branch 1: Try branching to one direction
		RoadAttributes newBranch1 = new RoadAttributes(curRoadAttr.Position + curRoadAttr.RoadSize * nextDirB1, nextDirB1, nextAngB1, curRoadAttr.RoadSize, curRoadAttr.Position + curRoadAttr.RoadSize * nextDirB1 * 2, RoadType.NONE);
		roadAttrs.Add(newBranch1);

		// Branch 2: Try branching to another direction
		RoadAttributes newBranch2 = new RoadAttributes(curRoadAttr.Position + curRoadAttr.RoadSize * nextDirB2, nextDirB2, nextAngB2, curRoadAttr.RoadSize, curRoadAttr.Position + curRoadAttr.RoadSize * nextDirB2 * 2, RoadType.NONE);
		roadAttrs.Add(newBranch2);

		// Road: Try to move forward
		RoadAttributes newRoA = new RoadAttributes(nextPos, nextDirR, nextAngR, curRoadAttr.RoadSize, getClosestDest(nextPos).Position, nextRoadType);
		roadAttrs.Add(newRoA);

	}

	// TODO: need to add state changing and param adjustment based on goals
	// For now, just set everything to true
	private void localConstraints(List<ISymbol> axiom) {
		for (int i = 0; i < axiom.Count; i++) {
			if (axiom[i] is Symbol) {
				Symbol castedSym = (Symbol)axiom[i];
				if (castedSym.ID == "?I") { 
					if (insertQuery(castedSym.RoadAttr)) {
						castedSym.State = StateType.SUCCESS;
					} else {
						castedSym.State = StateType.FAILURE;
					}
					axiom[i] = castedSym;
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

	// Get nearest surface to the given position
	private Vector3 getNearestSurface(Vector3 pos) {
		float shortestDist = float.MaxValue;
		Vector3 closestSurface = Vector3.Zero;
		
		Godot.Collections.Array heights = (Godot.Collections.Array)heightMap.SurfaceGetArrays(0)[0];
		foreach (Variant h in heights) {
			Vector3 curH = (Vector3)h;
			float dist = pos.DistanceSquaredTo(curH);
			if (dist < shortestDist) {
				shortestDist = dist;
				closestSurface = curH;
			}
		}
		return closestSurface;
	}

	// Check if the current position is above sea level and not intersecting with land
	private bool isAboveWater(Vector3 pos) {
		Vector3 checkSurface = getNearestSurface(pos);
		return pos.Y >= SeaLevel;
	}

	// Check if the current position intersects with the terrain
	private bool doesGroundIntersect(Vector3 pos) {
		// Check if a road is valid and can be placed here
		// To get vertices of surface: heightMap.SurfaceGetArrays(0)[0]
		// Get the nearest surface
		Vector3 checkHeight = getNearestSurface(pos);

		// heightmap intersects with cur position road, need to be a tunnel
		if (checkHeight.Y > pos.Y) {
			return true;
		}
		return false;
	}


	// Queries whether the road can be inserted
	// checks for legal terrain, or if road will intersect with water, mountains, etc.
	private bool insertQuery(RoadAttributes roadAttr) {
		//GD.Print("Running inquery at " + roadAttr.Position + " Looking at " + roadAttr.LookPosition + " With angle : " + roadAttr.Direction);
		//GD.Print("Road locs: " +  string.Join("\n", roadLocations));
		Vector3 startPos = roadAttr.Position - (roadAttr.Direction * new Vector3(25f, 0f, 25f));
		Vector3 endPos = roadAttr.Position + (roadAttr.Direction * new Vector3(25f, 0f, 25f));

		for (int i = 0; i < roadLocations.Count; i++) {
            Vector3 pos = roadLocations[i];
			Vector3 dir = roadDirections[i];
			// Same position, or near position from a certain threshold
			Vector3 intersectPos;
			if (lineIntersectsLine(roadAttr.Position, roadAttr.Direction, pos, dir, out intersectPos) && 
					intersectPos > startPos &&
					intersectPos < endPos) {
				//GD.Print("Invalid location");
				return false;
			}
		}
		roadLocations.Add(roadAttr.Position);
        roadDirections.Add(roadAttr.Direction);
		return true;
		
	}

	/*
	float denom = pDirB.Z * pDirA.X - pDirB.X * pDirA.Z; 
			rResult = Vector3.Inf;
			if (denom <= 0.00001f) { // Parallel?
				return false;
			}
			Vector3 v = pFromA - pFromB;
			float t = (pDirB.X * v.Z - pDirB.Z * v.X) / denom;
			rResult = pFromA + t * pDirA;
			return true;
	*/
	static bool lineIntersectsLine(Vector3 pFromA, Vector3 pDirA, Vector3 pFromB, Vector3 pDirB, out Vector3 rResult) {
		// See http://paulbourke.net/geometry/pointlineplane/
		rResult = Vector3.Inf;

		// Compute the cross product to check if the lines are parallel
		Vector3 crossDir = pDirA.Cross(pDirB);
		float denom = crossDir.LengthSquared(); // Equivalent to determinant in 3D

		if (denom <= 0.00001f) { // Parallel or nearly parallel?
			return false;
		}

		// Solve for t using determinant-based approach
		Vector3 v = pFromA - pFromB;
		float t = v.Cross(pDirB).Dot(crossDir) / denom;

		rResult = pFromA + t * pDirA;
		return true;
	}


	// Get the closest destination node the road is near
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


	// TODO: branches much have a minimum angle difference (ie, road branches must be 45 degrees apart)
	// Closest destination query, returns yaw needed to face thingy
	// Does NOT do any rotating on its own
	private float getClosestDestAngle(Vector3 pos, Vector3 dir) {
		Node3D firstChild = (Node3D)DestList.GetChild(0);
		float shortestDist = pos.DistanceTo(firstChild.Position);

		Vector3 dirToObject = (firstChild.Position - pos).Normalized();
		float angle = dir.SignedAngleTo(dirToObject, Vector3.Up);

		// Find closest destination to road
		for (int d = 1; d < DestList.GetChildren().Count; d++) {
			Node3D curChild = (Node3D)DestList.GetChild(d);
			float curDist = pos.DistanceTo(curChild.Position);
			if (curDist < shortestDist) {
				shortestDist = curDist;
				dirToObject = (curChild.Position - pos).Normalized();
				angle = dir.SignedAngleTo(dirToObject, Vector3.Up);
			}
		}
		return angle;
	}
}