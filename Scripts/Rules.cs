using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

// Rules to generate new string
public abstract class Rule {
    // Checks the symbol associated with the rule
	public abstract bool checkSymbol(Symbol symbol);

    // If any conditionals, evaluates
	public abstract bool checkCond();

    // Generates new string if rule is applied
	public abstract List<ISymbol> genOutput();
};

/* --------------------------- 
---------- R Rules -----------
------------------------------ */
// If true, branch this road
/* p2: R(del, ruleAttr) > ?I(roadAttr,state) : state==SUCCEED
{globalGoals(ruleAttr,roadAttr) creates the parameters
    for: pDel[0-2], pRuleAttr[0-2], pRoadAttr[0-2]}
        → +(roadAttr.angle)F(roadAttr.length)
        B(pDel[1],pRuleAttr[1],pRoadAttr[1]),
        B(pDel[2],pRuleAttr[2],pRoadAttr[2]),
        R(pDel[0],pRuleAttr[0]) ?I(pRoadAttr[0],UNASSIGNED)
}*/

class RuleRBranch : Rule {
    private string mySymbol = "R";
        
	private Dictionary<string, int> vars = new Dictionary<string, int>{
		{"del", -1},
		{"ruleAttr", -1},
        {"state", (int)StateType.UNASSIGNED}
	};


    public override bool checkSymbol(Symbol symbol)
    {
        if (mySymbol == symbol.ID) {
            vars["del"] = symbol.Del;
            vars["ruleAttr"] = symbol.RuleAttr;
            vars["state"] = (int)symbol.State;
            return true;
        }
        return false;
    }

    public override bool checkCond()
    {
        return vars["state"] == (int)StateType.SUCCESS;
    }

    public override List<ISymbol> genOutput() {
        GD.Print("Generating branch");
        //Two branch modules, B and a road module R plus the insertion query ?I are created.
        List<ISymbol> result = new List<ISymbol>{
            new Symbol("B", 0, 0, 0, StateType.UNASSIGNED),       // Branch 1
            new Symbol("B", 0, 0, 0, StateType.UNASSIGNED),       // Branch 2
            new Symbol("R", 0, 0, 0, StateType.UNASSIGNED),       // Road
            new Symbol("?I", 0, 0, 0, StateType.UNASSIGNED)       // Insertion query
        };
        return result;
    }
}

// If true, mark this road module to be deleted
// p1: R(del, ruleAttr) : del<0 → ε
// p3: R(del, ruleAttr) > ?I(roadAttr, state) : state==FAILED → ε
class RuleRDel : Rule {
    private string mySymbol = "R";
    
	private Dictionary<string, int> vars = new Dictionary<string, int>{
		{"del", -1},
		{"ruleAttr", -1},
        {"state", (int)StateType.UNASSIGNED}
	};


    public override bool checkSymbol(Symbol symbol)
    {
        if (mySymbol == symbol.ID) {
            vars["del"] = symbol.Del;
            vars["ruleAttr"] = symbol.RuleAttr;
            vars["state"] = (int)symbol.State;
            return true;
        }
        return false;
    }

    public override bool checkCond()
    {
        // Check del flag and if inquery module state is set to failed
        return vars["del"] < 0 || vars["state"] == (int)StateType.FAILURE;
    }

    public override List<ISymbol> genOutput() {
        List<ISymbol> result = new List<ISymbol>();
        Symbol delSym = new Symbol("D", -1, -1, -1, StateType.UNASSIGNED);
        result.Add(delSym);
        return result;
    }
}

/* --------------------------- 
---------- B Rules -----------
------------------------------ */


/* --------------------------- 
---------- I Rules -----------
------------------------------ */

// Insertion query deletion rule
// p9: ?I(roadAttr,state) : state!=UNASSIGNED → ε
class RuleIDel : Rule {
    private String mySymbol = "?I";
	private Dictionary<string, int> vars = new Dictionary<string, int>{
		{"state", -1}
	};

    public override bool checkSymbol(Symbol symbol)
    {
        if (mySymbol == symbol.ID) {
            vars["state"] = (int)symbol.State;
            return true;
        }
        return false;
    }

    public override bool checkCond()
    {
        return vars["state"] != (int)StateType.UNASSIGNED;
    }

    public override List<ISymbol> genOutput() {
        List<ISymbol> result = new List<ISymbol>();
        Symbol delSym = new Symbol("D", -1, -1, -1, StateType.UNASSIGNED);
        result.Add(delSym);
        return result;
    }
}
