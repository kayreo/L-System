using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

public class Module {
	public int val;

	public Module(int value) {
		val = value;
	}

}

[Tool]
public partial class RoadTest : Node3D
{
	[Export]
	public PackedScene Road { get; set; }

	[Export]
	public PackedScene Destination { get; set; }

	public Node3D RoadList;

	public Node3D DestList;

	public MeshInstance3D Bounds;

	public String axiom = "A(1)B(3)A(5)";

	private Godot.Collections.Dictionary rules = new Godot.Collections.Dictionary{
				{"R(x)", "R(x+1)"},
				//{"R(x)", "W(x-1)"},
				{" A(x) < B(y) > A(z)", "B(x+z)[A(y)]"}
			};


	/*
	#define  45 branching angle
	#define MinLight 0.1 light intensity threshol
	#define MaxAge 20 lifetime of ramets and spacers
	#define Len 2.0 length of spacers
	#define ProbB(x) (0.12+x*0.42)
	#define ProbR(x) (0.03+x*0.54)
	#define Radius(x) (sqrt(15–x*5)/)
	*/

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//axiom = "R(0,initialRuleAttr)?I(initRoadAttr,x)";
		// Generate
		//generate();

     	Regex re = new Regex(@"(A+\((\d+)\))|(B+\((\d+)\))");
 		Match match = re.Match("B(1)");
		GD.Print("Trying out regex");
		GD.Print(match.Success);

        // Class Regex Represents an
        // immutable regular expression.
        //   Format                Pattern
        // xxxxxxxxxx           ^[0 - 9]{ 10}$
        // +xx xx xxxxxxxx     ^\+[0 - 9]{ 2}\s +[0 - 9]{ 2}\s +[0 - 9]{ 8}$
        // xxx - xxxx - xxxx   ^[0 - 9]{ 3} -[0 - 9]{ 4}-[0 - 9]{ 4}$

		// Interpret
		// On interpretation, call query modules

		GD.Randomize();
		RoadList = GetNode<Node3D>("Roads");
		DestList = GetNode<Node3D>("Destinations");
		Bounds = GetNode<MeshInstance3D>("Bounds");
		//addRoad();
		randomizeDests();
	}

	private void generate() {
		char[] axiomToArray = axiom.ToCharArray();
		for (int i = 0; i < axiomToArray.Length; i++) {
			char curChar = axiomToArray[i];
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void insertQuery() {

	}

	private void globalGoals() {

	}

	private void localConstraints() {

	}

	private void randomizeDests() {
		Vector3 boundsSize = Bounds.GetAabb().Size;
		for (int i = 0; i < 3; i++) {
			Vector3 placePos = new Vector3(GD.RandRange((int)-boundsSize.X/3, (int)boundsSize.X/3), 0, GD.RandRange((int)-boundsSize.Z/3, (int)boundsSize.Z/3));
			GD.Print("Placing here: ", placePos);
			addDest(placePos);
		}
	}

	private void addRoad() {
		Road newRoad = (Road)Road.Instantiate();
		RoadList.AddChild(newRoad);
	}

	private void addDest(Vector3 pos) {
		Node3D newDest = (Node3D)Destination.Instantiate();
		newDest.Translate(pos);
		GD.Print("Init here: ", newDest.Position);
		DestList.AddChild(newDest);
	}
}
