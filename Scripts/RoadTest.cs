using Godot;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.Serialization;

public partial class RoadTest : Node3D
{

    private float progress = 0.0f;  // Progress along the curve (0 to 1)
    private float speed = 0.1f;  // Speed at which the cylinder moves along the curve

	[Export]
	public PackedScene testNode { get; set; }

	[Export]
	public PackedScene Viz { get; set; }

	[Export]
	public PackedScene Destination { get; set; }

	[Export(PropertyHint.Range, "0,50,1,or_greater")]
	public int Iterations;

	[Export(PropertyHint.Enum,"None,Rule1,Rule2")]
	public string Rule = "None";

	[Export(PropertyHint.Enum, "Overhead,Player")]
	public string StartCamera = "Overhead";

	[Export]
	public bool ShowTerrain = true;

	[Export]
	public bool InstantGen = true;

	[Export(PropertyHint.Range, "25,100,1,or_greater")]
	public float RoadSize = 25.0f;

	public Node3D VizList;

	public Node3D RoadList;

	public Node3D DestList;

	public MeshInstance3D Bounds;

	public Node3D Terrain;

	public Mesh TerrainHeight;

	public LSystem L;

	private Godot.Collections.Dictionary<string, Camera3D> cameras = new Godot.Collections.Dictionary<string, Camera3D>(); 
	
	private Godot.Collections.Array<Curve3D> curves = new Godot.Collections.Array<Curve3D>();

	private Godot.Collections.Array<Vector3> curvePositions = new Godot.Collections.Array<Vector3>();

	private Godot.Collections.Array<Vector3> roadColors = new Godot.Collections.Array<Vector3>{
		new Vector3(165, 42, 42),	// Regular road
		new Vector3(0, 0, 255),		// Bridge
		new Vector3(0, 0, 0),		// Tunnel start
		new Vector3(255, 0, 0),		// Tunnel mid
		new Vector3(0, 255, 0)		// Tunnel end
	};

	private Curve3D curCurve;

	private Vector3 mostRecentPos;

	// Used when iterations change
	private int i = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// TODO: Maybe adjust this via editor and not code
		// Prep nodes
		RoadList = GetNode<Node3D>("Roads");
		DestList = GetNode<Node3D>("Destinations");
		VizList = GetNode<Node3D>("Visuals");
		Bounds = GetNode<MeshInstance3D>("Bounds");
		Terrain = GetNode<Node3D>("Terrain");
		TerrainHeight = Terrain.GetNode<StaticBody3D>("StaticBody3D").GetNode<MeshInstance3D>("Terrain").Mesh;
		cameras.Add("Overhead", GetNode<Camera3D>("Camera3D"));
		cameras.Add("Player", GetNode<Camera3D>("Player/Camera3D"));
		ToggleTerrain();
		cameras[StartCamera].MakeCurrent();

		// Other setup
		GD.Randomize();
		L = new LSystem(RoadList, DestList, TerrainHeight, Rule, GetNode<Node3D>("Terrain").GetNode<MeshInstance3D>("Water").Position.Y, RoadSize);

		randomizeDests();

		if (InstantGen) {
			generateRoads(Iterations);
		}
	}

	// Clears object lists and reruns L-system
	private void generateRoads(int i) {
		// clear road list
		foreach (Node roadChild in RoadList.GetChildren()) {
			roadChild.QueueFree();
		}
		foreach (Node vizChild in VizList.GetChildren()) {
			vizChild.GetNode<Path3D>("Path3D").Curve.ClearPoints();
			vizChild.QueueFree();
		}
		curves.Clear();
		curCurve = null;
		interpret(L.buildRoads(i));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustReleased("Reset"))
		{
			GD.Print("------");
			GetTree().ReloadCurrentScene();
		}
		if (Input.IsActionJustReleased("Progress")) {
			i = 0;
			Iterations++;
			if (InstantGen) {
				generateRoads(Iterations);
			}
		}
		if (Input.IsActionJustReleased("Regress")) {
			i = 0;
			Iterations--;
			if (InstantGen) {
				generateRoads(Iterations);
			}
		}
		if (Input.IsActionJustReleased("ChangeCam")) {
			if (StartCamera == "Overhead") {
				StartCamera = "Player";
			} else {
				StartCamera = "Overhead";
			}
			cameras[StartCamera].MakeCurrent();
		}
		if (Input.IsActionJustReleased("ToggleTerrain")) {
			ShowTerrain = !ShowTerrain;
			ToggleTerrain();
		}

		if (!InstantGen && i <= Iterations) {
			generateRoads(i);
			i++;
		}
	}

	private void ToggleTerrain() {
		if (ShowTerrain) {
			Terrain.Visible = true;
		} else {
			Terrain.Visible = false;
		}
	}

	private void makeNewCurve() {

	}

	// Go through generated symbols and interperet
	private void interpret(List<ISymbol> axiom) {
		CsgPolygon3D newRoadViz = (CsgPolygon3D)Viz.Instantiate();
		newRoadViz.GetNode<Path3D>("Path3D").Curve = new Curve3D();
		Curve3D curve = newRoadViz.GetNode<Path3D>("Path3D").Curve;
		VizList.AddChild(newRoadViz);
		for (int i = 0; i < axiom.Count; i++) {
			ISymbol sym = axiom[i];
			//GD.Print("Interpreting: ", sym);
			if (sym is Symbol) {
				Symbol castedSym = (Symbol)sym;
				//GD.Print("ID: ", castedSym.ID);
				if (castedSym.ID == "A" || castedSym.ID == "Br" || castedSym.ID == "T" || castedSym.ID == "T1" || castedSym.ID == "T2") {
					if (i > 0 && axiom[i - 1] is Symbol && castedSym.RoadAttr.BuildRoadType != ((Symbol)axiom[i - 1]).RoadAttr.BuildRoadType) {
						//GD.Print("Changing: " + castedSym.ID + " from: " + ((Symbol)axiom[i - 1]).ID);
						newRoadViz = (CsgPolygon3D)Viz.Instantiate();
						newRoadViz.GetNode<Path3D>("Path3D").Curve = new Curve3D();
						curve = newRoadViz.GetNode<Path3D>("Path3D").Curve;
						VizList.AddChild(newRoadViz);
						newRoadViz.SetInstanceShaderParameter("color", roadColors[(int)castedSym.RoadAttr.BuildRoadType]);
					}
					addCurve(newRoadViz, castedSym.RoadAttr);
				}
			}
			// Branch
			else if (sym is SymBranch) {
				SymBranch castedSym = (SymBranch)sym;
				// Interpret symbols in this branch
				interpret(castedSym.Syms);
			}
		}
	}
	

	private RoadType curType = RoadType.NONE;
	/* --------------------------- 
	--------- Road Funcs ---------
	------------------------------ */
	private void addCurve(CsgPolygon3D poly, RoadAttributes roadAttr) {
		//GD.Print("Placing at: " + roadAttr.Position);
		Node3D viewTest = (Node3D)testNode.Instantiate();

		//RoadList.AddChild(viewTest);
		//viewTest.Position = roadAttr.Position;

		Path3D path = poly.GetNode<Path3D>("Path3D");
		Curve3D curve = path.Curve;
	
		Vector3 localPos = path.ToLocal(roadAttr.Position);
	
		// Don't add duplicates
		for (int i = 0; i < curve.PointCount; i++) {
			if (localPos.DistanceTo(curve.GetPointPosition(i)) <= Mathf.Epsilon) {
				return;
			}
		}

		Vector3 start;
		Vector3 end;

		// Placing point
		start = getNearestNormal(roadAttr.Position - (roadAttr.Direction * roadAttr.RoadSize));
		end = getNearestNormal(roadAttr.Position + (roadAttr.Direction * roadAttr.RoadSize));

		
		curve.AddPoint(localPos);
		
		curvePositions.Add(roadAttr.Position);
	}

	/* --------------------------- 
	--------- Dest Funcs ---------
	------------------------------ */
	/*
	Hardcoded values for position for debugging:
		int x = -100 * i;
		int y = 200 * i;
		int z = -300 * i;

	Regular values
		int x = GD.RandRange((int)-boundsSize.X/3, (int)boundsSize.X/3);
		int z = GD.RandRange((int)-boundsSize.Z/3, (int)boundsSize.Z/3);
		int y = (int)getNearestSurface(new Vector3(x, 0, z)).Y;
	*/
	// Randomly place 3 destinations in scene
	private void randomizeDests() {
		Vector3 boundsSize = Bounds.GetAabb().Size;
		for (int i = 0; i < 3; i++) {
			int x = GD.RandRange((int)-boundsSize.X/3, (int)boundsSize.X/3);
			int z = GD.RandRange((int)-boundsSize.Z/3, (int)boundsSize.Z/3);
			int y = (int)getNearestSurface(new Vector3(x, 0, z)).Y;
			Vector3 placePos = new Vector3(x, y, z);
			//GD.Print("Placing at: ", placePos);
			addDest(placePos);
		}
	}

	private void addDest(Vector3 pos) {
		Node3D newDest = (Node3D)Destination.Instantiate();
		newDest.Translate(pos);
		DestList.AddChild(newDest);
	}

	/* --------------------------- 
	--------- Util Funcs ---------
	------------------------------ */

	// Get nearest surface normal to the given position
	private Vector3 getNearestNormal(Vector3 pos) {
		float shortestDist = float.MaxValue;
		Vector3 closestNormal = Vector3.Zero;
		
		Godot.Collections.Array heights = (Godot.Collections.Array)TerrainHeight.SurfaceGetArrays(0)[1];
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

	// Get nearest surface vertex to the given position
	private Vector3 getNearestSurface(Vector3 pos) {
		float shortestDist = float.MaxValue;
		Vector3 closestSurface = Vector3.Zero;
		
		Godot.Collections.Array heights = (Godot.Collections.Array)TerrainHeight.SurfaceGetArrays(0)[0];
		foreach (Variant h in heights) {
			Vector3 curH = (Vector3)h;
			float dist = Mathf.Sqrt(pos.DistanceSquaredTo(curH));
			if (dist < shortestDist) {
				shortestDist = dist;
				closestSurface = curH;
			}
		}
		return closestSurface;
	}
}
