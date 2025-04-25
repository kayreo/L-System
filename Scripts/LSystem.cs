using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Runtime.InteropServices;

public partial class LSystem
{
	public Dictionary<String, Rule> Rules = new Dictionary<String, Rule>
	{
		{"RuleRBranch", new RuleRBranch()},
		{"RuleRDel", new RuleRDel()},
		{"RuleB", new RuleB()},
		{"RuleBDel", new RuleBDel()}
	};

	public Node3D RoadList;

	public Node3D DestList;

	public MeshInstance3D Bounds;

	public Vector3 CurPos = Vector3.Zero;

	public Vector3 CurDir = Vector3.Forward;

	public float SeaLevel;

	public float CurAngle = 0.0f;

	public float initSize;

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
	RoadAttributes initRoadAttr;

	// begin with a basic road symbol and an insertion query to determine if legal place to put road
	public List<ISymbol> generated;

	public List<List<ISymbol>> generatedSoFar = new List<List<ISymbol>>();

	public LSystem(Node3D rList, Node3D dList, Mesh hm, string Rule, float seaLevel, float rs) {
		DestList = dList;
		RoadList = rList;
		heightMap = hm;
		SeaLevel = seaLevel;
		initSize = rs;
		
		initRoadAttr = new RoadAttributes(Vector3.Zero, Vector3.Forward, 0f, initSize, RoadType.NONE, -1);

		if (rules.ContainsKey(Rule)) {
			initRuleAttr = rules[Rule];
		} else {
			initRuleAttr = rules["None"];
		}

		generated = new List<ISymbol>{new Symbol("R", 0, initRuleAttr, initRoadAttr, StateType.UNASSIGNED)};
		generatedSoFar.Add(generated);

		delays = new List<int>();
		ruleAttrs = new List<RuleAttributes>();
		roadAttrs = new List<RoadAttributes>();
		roadLocations = new List<Vector3>();	
        roadDirections = new List<Vector3>();
	}

	// Builds the list of symbols that will then be used to build the road system
	public List<ISymbol> buildRoads(int iterations) {
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
			roadLocations = new List<Vector3>();
			roadDirections = new List<Vector3>();
			delays = new List<int>();
			ruleAttrs = new List<RuleAttributes>();
			roadAttrs = new List<RoadAttributes>();
			//  GD.Print("----------------------------------------------------");
			//  GD.Print("--------------------", "ITERATION: ", i, "--------------------");
			//  GD.Print("----------------------------------------------------");
			

			localConstraints(generated);
			// Generate ideal successor for next iteration
			generate(generated);
			generatedSoFar.Add(generated);
		}
		//GD.Print("Length: " + generatedSoFar.Count);
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
				Symbol castedSym = (Symbol) sym;

				if (castedSym.ID == "A" || castedSym.ID == "Br" || castedSym.ID == "T" || castedSym.ID == "T1" || castedSym.ID == "T2") {
					result = new List<ISymbol>{castedSym};
				}

				if (curRule.checkSymbol(castedSym) && curRule.checkCond()) {
					// General rewrites
					result = curRule.genOutput();

					// If this is the branch rule, call global goals and populate params
					if (r == "RuleRBranch") {
						// Use these for global goals param calls
						RoadAttributes roA = castedSym.RoadAttr;
						RuleAttributes ruA = castedSym.RuleAttr;
						globalGoals(castedSym.Del, ruA, roA);

						Symbol drawRoad = (Symbol)result[0];				// Set the road to be drawn's attributes
						drawRoad.RoadAttr = castedSym.RoadAttr;
						drawRoad.RuleAttr = castedSym.RuleAttr;
						result[0] = drawRoad;

						for (int j = 1; j < result.Count; j++) {			// Set attributes for B1, B2, and R
							Symbol castedOutput = (Symbol)result[j];
							castedOutput.Del = delays[j - 1];
							castedOutput.RoadAttr = roadAttrs[j - 1];
							result[j] = castedOutput;
						}
					}

				}
			} else if (sym is SymBranch) {									// Branch rewrites
				SymBranch castedSym = (SymBranch)sym;						// Sym to rewrite
				SymBranch newBranch = new SymBranch();						// Resulting branch
				List<ISymbol> branchResults;								// Store rewritten symbols here
				for (int j = 0; j < castedSym.Syms.Count; j++) {			// Recurisvely rewrite syms in branch
					branchResults = rewrite(castedSym.Syms, castedSym.Syms[j], j);
					if (branchResults != null) {
						foreach (ISymbol genSym in branchResults) {			// Add to resulting branch's syms
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

		// Increment position to next nearest surface position
		Vector3 nextPos = getNearestSurface(curRoadAttr.Position + curRoadAttr.RoadSize * curRoadAttr.Direction);

		// Adjust angle and dir for road to conform to surface
		// Project the road onto the nearest surface normal
		Vector3 nearestNormal = getNearestNormal(curRoadAttr.Position);
		Vector3 projectedDir = curRoadAttr.Direction.Project(nearestNormal);

		// First generate regular moving forward angle
		float nextAngR = curRoadAttr.Direction.AngleTo(projectedDir);
		
		// If the road is not going to branch, angle the next road to the nearest destination
		if (curRoadAttr.Branched < 0) {
			nextAngR = getClosestDestAngle(nextPos, curRoadAttr.Direction);
		}
		// If the road is branched, modify delays for roads and have road ignore nearest destinations
		else {
			delayR = 0;
			delayB1 = 1;
			delayB2 = 2;
			nextAngR = 0;
			delayBranch = curRoadAttr.Branched - 1;
			// If the road's delay is up, start angling to next destination
			if (curRoadAttr.Branched == 0) {
				nextAngR = getClosestDestAngle(nextPos, curRoadAttr.Direction);
			}
		}

		// Adjust direction with updated angle
		Vector3 nextDirR = curRoadAttr.Direction.Rotated(nearestNormal, nextAngR).Normalized();

		// Adjust angle and dir for branch 1 by randomly picking from angle range
		float nextAngB1 = curRoadAttr.CurAngle + (float)GD.RandRange(curRuleAttr.MinAngle, curRuleAttr.MaxAngle);
		Vector3 nextDirB1 = curRoadAttr.Direction.Rotated(Vector3.Up, nextAngB1).Normalized();

		// Adjust angle and dir for branch 2 by randomly picking from angle range
		float nextAngB2 = curRoadAttr.CurAngle - (float)GD.RandRange(curRuleAttr.MinAngle, curRuleAttr.MaxAngle);
		Vector3 nextDirB2 = curRoadAttr.Direction.Rotated(Vector3.Up, nextAngB2).Normalized();

		// Add delays to array
		delays.Add(delayB1);
		delays.Add(delayB2);
		delays.Add(delayR);

		// Generate roadAttrs
		// Branch 1: Try branching to one direction
		RoadAttributes newBranch1 = new RoadAttributes(nextPos, nextDirB1, nextAngB1, curRoadAttr.RoadSize, RoadType.NONE, delayBranch);
		roadAttrs.Add(newBranch1);

		// Branch 2: Try branching to another direction
		RoadAttributes newBranch2 = new RoadAttributes(nextPos, nextDirB2, nextAngB2, curRoadAttr.RoadSize, RoadType.NONE, delayBranch);
		roadAttrs.Add(newBranch2);

		// Road: Try to move forward
		RoadAttributes newRoA = new RoadAttributes(nextPos, nextDirR, nextAngR, curRoadAttr.RoadSize, curRoadAttr.BuildRoadType, delayBranch);
		roadAttrs.Add(newRoA);
	}

	// Place down a test road and check environment. If valid environment, set to successfully built
	private void localConstraints(List<ISymbol> axiom) {
		for (int i = 0; i < axiom.Count; i++) {
			if (axiom[i] is Symbol) {
				Symbol castedSym = (Symbol)axiom[i];
				if (castedSym.ID == "R") { 
					castedSym = adjustAttrs(castedSym);
					if (castedSym.Del != -1 && insertQuery(castedSym.RoadAttr)) {
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
	// First environment query
	// Adjust road attribute values based on local environment
	private Symbol adjustAttrs(Symbol sym) {
		// Get the attribute from the passed in symbol
		RoadAttributes roadAttr = sym.RoadAttr;

		// If the current road is in the middle of tuenneling, check if tunnel needs to continue or stop
		if (roadAttr.BuildRoadType == RoadType.TUNNELSTART || roadAttr.BuildRoadType == RoadType.TUNNEL) {
			roadAttr.CurAngle = 0;
			if (doesGroundIntersect(roadAttr.Position, roadAttr.Direction, roadAttr.RoadSize)) {
				roadAttr.BuildRoadType = RoadType.TUNNEL;
			}
			// If the road does not intersect the ground but is a tunnel, end tunneling
			else {
				roadAttr.BuildRoadType = RoadType.TUNNELEND;
			}
		}

		// If the next position hits terrain and the angle is too steep, turn it into a tunnel
		else if (doesGroundIntersect(roadAttr.Position, roadAttr.Direction, roadAttr.RoadSize) && (roadAttr.CurAngle < initRuleAttr.MinAngle || roadAttr.CurAngle > initRuleAttr.MaxAngle)) {
			if (roadAttr.BuildRoadType != RoadType.TUNNELSTART) {
				//roadAttr.CurAngle = 0;
				//GD.Print("TunnelingStart");
				roadAttr.BuildRoadType = RoadType.TUNNELSTART;
			} 
		}
		// If the next position is over water and not hitting terrain, turn it into a bridge
		else if (!doesGroundIntersect(roadAttr.Position, roadAttr.Direction, roadAttr.RoadSize) && isAboveWater(roadAttr.Position)) {
			roadAttr.BuildRoadType = RoadType.BRIDGE;
		}
		// /*
		// Invalid environment scenarios:
		// 	Ground does not intersect, angle too steep
		// 	Ground not above water and not intersecting with ground
		// */
		else if (!doesGroundIntersect(roadAttr.Position, roadAttr.Direction, roadAttr.RoadSize) && (roadAttr.CurAngle < initRuleAttr.MinAngle || roadAttr.CurAngle > initRuleAttr.MaxAngle)) {
			roadAttr.Branched = -1;
		}
		else if (!doesGroundIntersect(roadAttr.Position, roadAttr.Direction, roadAttr.RoadSize) && !isAboveWater(roadAttr.Position)) {
			roadAttr.Branched = -1;
		}
		else {
			roadAttr.BuildRoadType = RoadType.NONE;
		}
		sym.RoadAttr = roadAttr;
		return sym;
	}

	// Second environment query
	// Checks if the road will intersect with another road
	private bool insertQuery(RoadAttributes roadAttr) {
		//GD.Print("Running inquery at " + roadAttr.Position + " Looking at " + roadAttr.LookPosition + " With angle : " + roadAttr.Direction);
		//GD.Print("Road locs: " +  string.Join("\n", roadLocations));
		if (roadLocations.Contains(roadAttr.Position)) {
			return false;
		}
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
			if (roadAttr.Position.DistanceTo(pos) <= Mathf.Epsilon) {
				return false;
			}
			if (lineIntersectsLine(startPos, endPos, startPos2, endPos2, out rStart, out rEnd) && 
					rStart.DistanceTo(rEnd) <= Mathf.Epsilon) {
				return false;
			}
		}
		getNearestSurface(roadAttr.Position);
        roadDirections.Add(roadAttr.Direction);
		return true;
		
	}


	/* --------------------------- 
	--------- Util Funcs ---------
	------------------------------ */

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
		
		if (pos.DistanceTo(getNearestSurface(startPos)) <= Mathf.Epsilon) {
			// heightmap intersects with cur position road, need to be a tunnel
			//if (checkHeight.Y > pos.Y) {
				//GD.Print("Intersecting with ground");
				return true;
			//}	
		}
		return false;
	}

	// Line intersection code by by Ronald Holthuizen from: https://paulbourke.net/geometry/pointlineplane/calclineline.cs
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