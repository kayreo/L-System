using Godot;
using System;
using System.Collections.Generic;


// Generic symbol type to account for reg symbols and symbol containers
public interface ISymbol {
};

// Used for letters like A(X)
public struct Symbol : ISymbol
{
    public Symbol(String id, int val)
    {
		ID = id; // ID
        Val = val; // Module
    }

    public int Val { get; }

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