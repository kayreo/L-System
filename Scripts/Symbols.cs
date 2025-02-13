using Godot;
using System;
using System.Collections.Generic;


// Generic symbol type to account for reg symbols and symbol containers
public interface ISymbol {
};

// Used for letters like A(X)
public struct Symbol : ISymbol
{
    public Symbol(String id, int val, Dictionary<Variant, Variant> vals)
    {
		ID = id; // ID
        Val = val; // Module
		Values = vals;
    }

    public int Val { get; }

	public Dictionary<Variant, Variant> Values;

	public String ID { get; }

    public override string ToString() => $"{ID}({Val})";
}

// Road symbol
public struct RSymbol : ISymbol
{
    public RSymbol(String id, int delay, int ruleAttribute)
    {
		ID = id; // ID
        Del = delay;
		RuleAttr = ruleAttribute;
    }

    public int Del { get; }

	public int RuleAttr { get; }

	public String ID { get; }

    public override string ToString() => $"{ID}({Del}, {RuleAttr})";
}

// Inquery symbol
public struct IQuerySymbol : ISymbol
{
    public IQuerySymbol(String id, Dictionary<Variant, Variant> val)
    {
		ID = id; // ID
        Val = val; // Module
    }

    public Dictionary<Variant, Variant> Val { get; }

	public String ID { get; }

    public override string ToString() => $"{ID}({Val})";
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