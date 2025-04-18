using Godot;
using System;
using System.Collections.Generic;

public enum StateType {
	UNASSIGNED,
	SUCCESS,
	FAILURE
}

public enum RoadType {
	NONE,
	BRIDGE,
	TUNNELSTART,
	TUNNEL,
	TUNNELEND 
}

// Struct for rule attributes
public struct RuleAttributes {
	public RuleAttributes(float minAng, float maxAng) {

		MinAngle = minAng;
		MaxAngle = maxAng;
		
	}
public float MinAngle { get; set; }
	public float MaxAngle { get; set; }
	
	public override string ToString() => $"(Angle range:{MinAngle} - {MaxAngle})";
}

// Struct for road attributes
public struct RoadAttributes {
	public RoadAttributes(Vector3 pos, Vector3 dir, float curAng, float roadSize, RoadType rt, int branchDelay) {
		Position = pos;
		Direction = dir;
		CurAngle = curAng;
		RoadSize = roadSize;
		BuildRoadType = rt;
		Branched = branchDelay;
	}

	public Vector3 Position { get; set; }
	public Vector3 Direction { get; set; }
	public float CurAngle { get; set; }
	public float RoadSize { get; set; }
	public RoadType BuildRoadType { get; set; }
	public int Branched { get; set; }

	public override string ToString() => $"(Pos:{Position}, Dir:{Direction}, Angle: {CurAngle}, Road:{BuildRoadType})";
}

// Generic symbol type to account for reg symbols and symbol containers
public interface ISymbol {
};

// Symbol used in road gen
public struct Symbol : ISymbol
{
    public Symbol(String id, int delay, RuleAttributes ruleAttribute, RoadAttributes roA, StateType state)
    {
		ID = id; // ID
        Del = delay;
		RuleAttr = ruleAttribute;
		RoadAttr = roA;
		State = StateType.UNASSIGNED;
    }
	public String ID { get; }

    public int Del { get; set; }

	public RuleAttributes RuleAttr { get; set; }

	public RoadAttributes RoadAttr { get; set; }

	public StateType State { get; set; }
    public override string ToString() => $"{ID}({Del}, {RuleAttr}, {RoadAttr}, {State})";
}

// Used for brackets, like in [A(x)]
public struct SymBranch : ISymbol
{
	public SymBranch() {
		Syms = new List<ISymbol>();
	}

	public List<ISymbol> Syms { get; }

	public override string ToString() => $"[{String.Join(", ", Syms)}]";
}