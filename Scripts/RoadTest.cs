using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
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

	// Regex for pattern A(x)
	public Regex reA = new Regex(@"(\p{L})\((\d+)\)");

	// Regex for pattern A(x)B(y)A(z)
	public Regex reABA = new Regex(@"([\p{L}])\((\d+)\)[^(\1)]\((\d+)\)\1\((\d+)\)");

	private string generatedRules = "";

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
		GD.Randomize();
		RoadList = GetNode<Node3D>("Roads");
		DestList = GetNode<Node3D>("Destinations");
		Bounds = GetNode<MeshInstance3D>("Bounds");

		// Generate
		generate();
		// Interpret
		// On interpretation, call query modules
		
		//addRoad();
		//randomizeDests();
	}

	/*
	{ F: FFFF }
	*/

	private void generate() {
		List<String> result = new List<String>();
		// Evaluate A patterns first
		MatchCollection patterns = reA.Matches(axiom);
		for (int i = 0; i < patterns.Count; i++) {
			string newPattern = "";
			Match pattern = patterns[i];
			GD.Print("On pattern : ", pattern.Value[0]);
			if (pattern.Value[0] == 'A') {
			int randomNum = GD.RandRange(0, 100);
				newPattern = "A(";
				int sum = 0;
				GD.Print("On char: ", pattern.Value[2]);
				if (randomNum <= 40) {
					sum = pattern.Value[2] - '0' - 1;
				} else {
					newPattern = "B(";
					sum = pattern.Value[2] - '0' - 1;
				}
				newPattern += sum.ToString() + ")";
				GD.Print("New pattern gen: ", newPattern);
			} else if (pattern.Value[0] == 'B') {
				// Need to check left and right contexts
				if (i - 1 >= 0 && i + 1 < patterns.Count) {
					Match leftPattern = patterns[i - 1];
					Match rightPattern = patterns[i + 1];
					GD.Print("My left and rights: ", leftPattern.Value, " ", rightPattern.Value);
					int y = pattern.Value[2] - '0';
					if (y < 4) {
						GD.Print("less than y");
						int xz = leftPattern.Value[2] - '0' + rightPattern.Value[2] - '0';
						newPattern = "B(" + xz + ")[A(" + y + ")]";
					}
				}
			}
			GD.Print("New pattern is: ", newPattern);
			result.Add(newPattern);
		}
		GD.Print("Processed: ", string.Join("", result));
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
