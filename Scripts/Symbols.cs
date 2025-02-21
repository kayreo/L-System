using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;

// Struct for road attributes
public struct RoadAttributes {


	public RoadAttributes(Vector3 pos, Vector3 dir, float ang, float len) {
		Position = pos;
		Direction = dir;
		Angle = ang;
		Length = len;
	}

	public Vector3 Position { get; set; }
	public Vector3 Direction { get; set; }
	public float Angle { get; set; }
	public float Length { get; set; }

	public override string ToString() => $"(Pos:{Position}, Dir:{Direction}, Ang:{Angle}, Len:{Length})";
}

public enum StateType {
	UNASSIGNED,
	SUCCESS,
	FAILURE
}

// Generic symbol type to account for reg symbols and symbol containers
public interface ISymbol {
};

// Symbol used in road gen
public struct Symbol : ISymbol
{
    public Symbol(String id, int delay, List<float> ruleAttribute, RoadAttributes roA, StateType state)
    {
		ID = id; // ID
        Del = delay;
		RuleAttr = ruleAttribute;
		RoadAttr = roA;
		State = StateType.UNASSIGNED;
    }
	public String ID { get; }

    public int Del { get; set; }

	public List<float> RuleAttr { get; set; }

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