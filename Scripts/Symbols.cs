using Godot;
using System;
using System.Collections.Generic;
using System.Data;


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
    public Symbol(String id, int delay, int ruleAttribute, int roadAttribute, StateType state)
    {
		ID = id; // ID
        Del = delay;
		RuleAttr = ruleAttribute;
		RoadAttr = roadAttribute;
		State = StateType.UNASSIGNED;
    }
	public String ID { get; }

    public int Del { get; set; }

	public int RuleAttr { get; set; }

	public int RoadAttr { get; set; }

	public StateType State { get; set; }

    public override string ToString() => $"{ID}({Del}, {RuleAttr}, {RoadAttr})";
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