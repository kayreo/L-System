using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;
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
		{"None", new RuleAttributes(Mathf.DegToRad(90f), Mathf.DegToRad(95f))}
	};

	// Init attributes
	RuleAttributes initRuleAttr;
	RoadAttributes initRoadAttr = new RoadAttributes(Vector3.Zero, Vector3.Forward, 0f, 25.0f, Vector3.Zero, RoadType.NONE, -1);

	// begin with a basic road symbol and an insertion query to determine if legal place to put road
	public List<ISymbol> generated;

	public List<List<ISymbol>> generatedSoFar = new List<List<ISymbol>>();

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

		generated = new List<ISymbol>{new Symbol("R", 0, initRuleAttr, initRoadAttr, StateType.UNASSIGNED), new Symbol("?I", 0, new RuleAttributes(), initRoadAttr, StateType.UNASSIGNED)};
		generatedSoFar.Add(generated);

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

		// If the target axiom was already generated, return it
		if (generatedSoFar.Count >= iterations + 1) {
			return generatedSoFar[iterations];
		}
		// Otherwise, need to generate a new one
		generated = generatedSoFar[generatedSoFar.Count - 1];

		// First fill symbol vals using global goals, where you call queries to update your goals
		// local constraints generation
		// Next rewrite calls local constraints, which culls or rewrites rules based on queries made in global goals
		for (int i = generatedSoFar.Count; i < iterations; i++) {
			//  GD.Print("----------------------------------------------------");
			//  GD.Print("--------------------", "ITERATION: ", i, "--------------------");
			//  GD.Print("----------------------------------------------------");
			

			localConstraints(generated);
			// Generate ideal successor for next iteration
			generate(generated);
			generatedSoFar.Add(generated);
		}
		//GD.Print("Generated: ", string.Join("\n\n", generated));

		//GD.Print("Trimmed: " + string.Join("\n\n", trimGenerated(generated)));

        return generated;
	}

	private List<ISymbol> trimGenerated(List<ISymbol> generated) {
		// Trim out the generated rules that aren't a draw rule
		Godot.Collections.Array<String> drawRules = new Godot.Collections.Array<String>{"A", "Br", "T1", "T", "T2"};
		for(int i = 0; i < generated.Count; i++) {
			if (generated[i] is Symbol) {
				if (drawRules.Contains(((Symbol)generated[i]).ID)) {
					generated.Remove(generated[i]);
				}
			} else {
				SymBranch symB = (SymBranch)generated[i];
				trimGenerated(symB.Syms);
			}
		}
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
				if (castedSym.ID == "A" || castedSym.ID == "Br" || castedSym.ID == "T" || castedSym.ID == "T1" || castedSym.ID == "T2") {
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
						globalGoals(castedSym.Del, ruA, roA);
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
	private void globalGoals(int delay, RuleAttributes curRuleAttr, RoadAttributes curRoadAttr) {
		delays.Clear();
		ruleAttrs.Clear();
		roadAttrs.Clear();

		int delayB1 = 3;
		int delayB2 = 3;
		int delayR = 3;
		
		int delayBranch = curRoadAttr.Branched;

		RoadType nextRoadType = RoadType.NONE;

		// Increment position to next position
		Vector3 nextPos = getNearestSurface(curRoadAttr.Position + curRoadAttr.RoadSize * curRoadAttr.Direction);

		Vector3 nextLookPos = curRoadAttr.Position + curRoadAttr.RoadSize * curRoadAttr.Direction * 2;

		// Adjust angle and dir for road
		float nextAngR = curRoadAttr.CurAngle;
		
		// Get next look dest
		if (curRoadAttr.Branched < 0) {
			nextLookPos = getClosestDest(nextPos).Position;
			nextAngR = getClosestDestAngle(nextPos, curRoadAttr.Direction);
		}
		else {
			delayR = 0;
			delayB1 = 1;
			delayB2 = 2;
			nextAngR = 0;
			delayBranch = curRoadAttr.Branched - 1;
			if (curRoadAttr.Branched == 0) {
				nextLookPos = getClosestDest(nextPos).Position;
				nextAngR = getClosestDestAngle(nextPos, curRoadAttr.Direction);
			}
		}

		Vector3 nextDirR = curRoadAttr.Direction.Rotated(Vector3.Up, nextAngR).Normalized();

		// Project the road onto the nearest surface normal
		Vector3 nearestNormal = getNearestNormal(nextPos);
		Vector3 projectedDir = nextDirR.Project(nearestNormal);

		// If the angle to change the direction is too steep, make a tunnel instead
		float ang = nextDirR.AngleTo(projectedDir);

		// If the next position hits terrain, turn it into a tunnel
		if ((ang < curRuleAttr.MinAngle || ang > curRuleAttr.MaxAngle) && doesGroundIntersect(nextPos, nextDirR, curRoadAttr.RoadSize)) {
			if (curRoadAttr.BuildRoadType != RoadType.TUNNELSTART) {
				//GD.Print("TunnelingStart");
				nextRoadType = RoadType.TUNNELSTART;
			} 
		}
		else {
			//nextLookPos = nextLookPos.Project(nearestNormal);
			//nextDirR = projectedDir;
		}

	
		// If the road intersects with ground but is already an existing tunnel, keep tunneling
		if (curRoadAttr.BuildRoadType == RoadType.TUNNELSTART || curRoadAttr.BuildRoadType == RoadType.TUNNEL) {
			if (doesGroundIntersect(nextPos, nextDirR, curRoadAttr.RoadSize)) {
				nextRoadType = RoadType.TUNNEL;
			}
			// If the road does not intersect the ground but is a tunnel, end tunneling
			else if (!doesGroundIntersect(nextPos, nextDirR, curRoadAttr.RoadSize)) {
				nextRoadType = RoadType.TUNNELEND;
			}
		}

		// If the next position is over water and not hitting terrain, turn it into a bridge
		else if (!doesGroundIntersect(nextPos, nextDirR, curRoadAttr.RoadSize) && isAboveWater(nextPos)) {
			//GD.Print("Need a bridge");
			nextRoadType = RoadType.BRIDGE;
		}

		// Adjust angle and dir for branch 1
		float nextAngB1 = curRoadAttr.CurAngle + (float)GD.RandRange(curRuleAttr.MinAngle, curRuleAttr.MaxAngle);
		Vector3 nextDirB1 = curRoadAttr.Direction.Rotated(Vector3.Up, nextAngB1).Normalized();

		// Adjust angle and dir for branch 2
		float nextAngB2 = curRoadAttr.CurAngle - (float)GD.RandRange(curRuleAttr.MinAngle, curRuleAttr.MaxAngle);
		Vector3 nextDirB2 = curRoadAttr.Direction.Rotated(Vector3.Up, nextAngB2).Normalized();

		// Generate delays
		// arbitrary rn
		delays.Add(delayB1);
		delays.Add(delayB2);
		delays.Add(delayR);

		// Generate roadAttr
		// Branch 1: Try branching to one direction
		RoadAttributes newBranch1 = new RoadAttributes(nextPos, nextDirB1, nextAngB1, curRoadAttr.RoadSize, curRoadAttr.Position + curRoadAttr.RoadSize * nextDirB1 * 2, RoadType.NONE, delayBranch);
		roadAttrs.Add(newBranch1);

		// Branch 2: Try branching to another direction
		RoadAttributes newBranch2 = new RoadAttributes(nextPos, nextDirB2, nextAngB2, curRoadAttr.RoadSize, curRoadAttr.Position + curRoadAttr.RoadSize * nextDirB2 * 2, RoadType.NONE, delayBranch);
		roadAttrs.Add(newBranch2);

		// Road: Try to move forward
		RoadAttributes newRoA = new RoadAttributes(nextPos, nextDirR, nextAngR, curRoadAttr.RoadSize, nextLookPos, nextRoadType, delayBranch);
		roadAttrs.Add(newRoA);

	}

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

		// Get nearest normal to the given position
	private Vector3 getNearestNormal(Vector3 pos) {
		float shortestDist = float.MaxValue;
		Vector3 closestNormal = Vector3.Zero;
		
		Godot.Collections.Array heights = (Godot.Collections.Array)heightMap.SurfaceGetArrays(0)[1];
		foreach (Variant h in heights) {
			Vector3 curH = (Vector3)h;
			float dist = pos.DistanceSquaredTo(curH);
			if (dist < shortestDist) {
				shortestDist = dist;
				closestNormal = curH;
			}
		}
		return closestNormal;
	}

	// Check if the current position is above sea level and not intersecting with land
	private bool isAboveWater(Vector3 pos) {
		return pos.Y >= SeaLevel;
	}

	// Check if the current position intersects with the terrain
	private bool doesGroundIntersect(Vector3 pos, Vector3 dir, float size) {
		// Check if a road is valid and can be placed here
		// To get vertices of surface: heightMap.SurfaceGetArrays(0)[0]
		// Get the nearest surface
		// Check if the road is intersecting with surface
		Vector3 startPos = pos - (dir * size);
		Vector3 endPos = pos + (dir * size);
		
		if (startPos.Y > getNearestSurface(startPos).Y || endPos.Y < getNearestSurface(endPos).Y) {
			// heightmap intersects with cur position road, need to be a tunnel
			//if (checkHeight.Y > pos.Y) {
				//GD.Print("Intersecting with ground");
				return true;
			//}	
		}
		return false;
	}


	// Queries whether the road can be inserted
	// checks for legal terrain, or if road will intersect with water, mountains, etc.
	private bool insertQuery(RoadAttributes roadAttr) {
		//GD.Print("Running inquery at " + roadAttr.Position + " Looking at " + roadAttr.LookPosition + " With angle : " + roadAttr.Direction);
		//GD.Print("Road locs: " +  string.Join("\n", roadLocations));
		Vector3 startPos = roadAttr.Position - (roadAttr.Direction * roadAttr.RoadSize);
		Vector3 endPos = roadAttr.Position + (roadAttr.Direction * roadAttr.RoadSize);

		for (int i = 0; i < roadLocations.Count; i++) {
            Vector3 pos = roadLocations[i];
			Vector3 dir = roadDirections[i];
			Vector3 startPos2 = pos - (dir * roadAttr.RoadSize);
			Vector3 endPos2 = pos + (dir * roadAttr.RoadSize);
			// Same position, or near position from a certain threshold
			Vector3 rStart;
			Vector3 rEnd;
			if (lineIntersectsLine(startPos, endPos, startPos2, endPos2, out rStart, out rEnd) && 
					rStart.DistanceTo(rEnd) <= Mathf.Epsilon) {
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
	public static bool lineIntersectsLine(Vector3 line1Point1, Vector3 line1Point2, 
		Vector3 line2Point1, Vector3 line2Point2, out Vector3 resultSegmentPoint1, out Vector3 resultSegmentPoint2) {
		// Algorithm is ported from the C algorithm of 
		// Paul Bourke at http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline3d/
		resultSegmentPoint1 = Vector3.Zero;
		resultSegmentPoint2 = Vector3.Zero;
		
		Vector3 p1 = line1Point1;
		Vector3 p2 = line1Point2;
		Vector3 p3 = line2Point1;
		Vector3 p4 = line2Point2;
		Vector3 p13 = p1 - p3;
		Vector3 p43 = p4 - p3;
		
		if (p43.LengthSquared() < Mathf.Epsilon) {
			return false;
		}
		Vector3 p21 = p2 - p1;
		if (p21.LengthSquared() < Mathf.Epsilon) {
			return false;
		}
		
		double d1343 = p13.X * (double)p43.X + (double)p13.Y * p43.Y + (double)p13.Z * p43.Z;
		double d4321 = p43.X * (double)p21.X + (double)p43.Y * p21.Y + (double)p43.Z * p21.Z;
		double d1321 = p13.X * (double)p21.X + (double)p13.Y * p21.Y + (double)p13.Z * p21.Z;
		double d4343 = p43.X * (double)p43.X + (double)p43.Y * p43.Y + (double)p43.Z * p43.Z;
		double d2121 = p21.X * (double)p21.X + (double)p21.Y * p21.Y + (double)p21.Z * p21.Z;
		
		double denom = d2121 * d4343 - d4321 * d4321;
		if (Math.Abs(denom) < Mathf.Epsilon) {
			return false;
		}
		double numer = d1343 * d4321 - d1321 * d4343;
		
		double mua = numer / denom;
		double mub = (d1343 + d4321 * (mua)) / d4343;
		
		resultSegmentPoint1.X = (float)(p1.X + mua * p21.X);
		resultSegmentPoint1.Y = (float)(p1.Y + mua * p21.Y);
		resultSegmentPoint1.Z = (float)(p1.Z + mua * p21.Z);
		resultSegmentPoint2.X = (float)(p3.X + mub * p43.X);
		resultSegmentPoint2.Y = (float)(p3.Y + mub * p43.Y);
		resultSegmentPoint2.Z = (float)(p3.Z + mub * p43.Z);

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