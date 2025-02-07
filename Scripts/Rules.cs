using Godot;
using System;
using System.Collections.Generic;

// Rules to generate new string
public abstract class Rule {
    // Checks the symbol associated with the rule
	public abstract bool checkSymbol(Symbol symbol);

    // If any conditionals, evaluates
	public abstract bool checkCond();

    // Generates new string if rule is applied
	public abstract List<ISymbol> genOutput();
};


// Generates rules when A(x) symbol is found
// A(x) -> A(x + 1) : 0.4 chance
// A(x) -> B(x - 1) : 0.6 chance
class RuleA : Rule {
	private String mySymbol = "A";

	private Dictionary<char, int> vars = new Dictionary<char, int>{
		{'x', -1}
	};
	
	public override bool checkSymbol(Symbol sym) {
        // Update vars if using this rule
        if (sym.ID == mySymbol) {
		    vars['x'] = sym.Val;
            return true;
        }
		return false;
	}

    public override bool checkCond()
    {
        return true;
    }

    public override List<ISymbol> genOutput()
    {
		float prob = GD.Randf();
		if (prob <= 0.4) {
        	return new List<ISymbol>{new Symbol(mySymbol, vars['x'] + 1)};
		}
		else {
			return new List<ISymbol>{new Symbol("B", vars['x'] - 1)};
		}
    }
}

// Generates rules when B(x) symbol is found
// A(x) < B(y) > A(z) : y < 4 -> B(x+z)[A(y)]
// TODO: left and right context eval
class RuleB : Rule {
	private String mySymbol = "B";

	private Dictionary<char, int> vars = new Dictionary<char, int>{
		{'x', -1},
		{'y', -1},
		{'z', -1}
	};
	
	public override bool checkSymbol(Symbol sym) {
        // Update vars if using this rule
        if (sym.ID == mySymbol) {
            vars['x'] = 2;
            vars['y'] = sym.Val;
            vars['z'] = 1;
            return true;
        }
		return false;
	}

    public override bool checkCond()
    {
        return true;
    }

    public override List<ISymbol> genOutput()
    {
		List<ISymbol> results = new List<ISymbol>();
		SymBranch container = new SymBranch();
		int sum = vars['x'] + vars['z'];
		results.Add(new Symbol("B", sum));
		container.Syms.Add(new Symbol("A", vars['y']));
		results.Add(container);
		return results;
    }
}
