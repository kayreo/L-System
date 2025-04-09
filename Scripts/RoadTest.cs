using Godot;
using System.Collections.Generic;

public partial class RoadTest : Node3D
{

	[Export]
	public PackedScene Road { get; set; }

	[Export]
	public PackedScene Bridge { get; set; }

	[Export]
	public PackedScene TunnelStart { get; set; }

	[Export]
	public PackedScene Tunnel { get; set; }

	[Export]
	public PackedScene TunnelEnd { get; set; }

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

	public Node3D RoadList;

	public Node3D DestList;

	public MeshInstance3D Bounds;

	public Node3D Terrain;

	public Mesh TerrainHeight;

	public LSystem L;

	private Godot.Collections.Dictionary<string, Camera3D> cameras = new Godot.Collections.Dictionary<string, Camera3D>(); 
	
	private Godot.Collections.Array<Curve3D> curves = new Godot.Collections.Array<Curve3D>();
	
	/*
	TODO: add terrain
	add more functionality in godot editor
		- add rule customization to editor (user can select which rule to run simulation on)
	add bridges (and maybe tunnels) to l system (new symbol, new instantiated scene)
		- detect terrain height, bridge spawn when above threshold
	*/

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Prep nodes
		RoadList = GetNode<Node3D>("Roads");
		DestList = GetNode<Node3D>("Destinations");
		Bounds = GetNode<MeshInstance3D>("Bounds");
		Terrain = GetNode<Node3D>("Terrain");
		TerrainHeight = Terrain.GetNode<StaticBody3D>("StaticBody3D").GetNode<MeshInstance3D>("Terrain").Mesh;
		cameras.Add("Overhead", GetNode<Camera3D>("Camera3D"));
		cameras.Add("Player", GetNode<Camera3D>("Player/Camera3D"));
		ToggleTerrain();
		cameras[StartCamera].MakeCurrent();
		// Other setup
		GD.Randomize();
		L = new LSystem(RoadList, DestList, TerrainHeight, Rule, GetNode<Node3D>("Terrain").GetNode<MeshInstance3D>("Water").Position.Y);

		randomizeDests();

		generateRoads();
	}

	private void generateRoads() {
		for (int i = 0; i < Iterations; i++) {
			// clear road list
			foreach (Node roadChild in RoadList.GetChildren()) {
				roadChild.QueueFree();
			}
			interpret(L.buildRoads(i));
		}
		interpret(L.buildRoads(Iterations));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustReleased("Reset"))
		{
			GetTree().ReloadCurrentScene();
		}
		if (Input.IsActionJustReleased("Progress")) {
			Iterations++;
			generateRoads();
		}
		if (Input.IsActionJustReleased("Regress")) {
			Iterations--;
			generateRoads();
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
	}

	private void ToggleTerrain() {
		if (ShowTerrain) {
			Terrain.Visible = true;
		} else {
			Terrain.Visible = false;
		}
	}

	// Go through generated symbols and interperet
	// TODO: make this better lol
	// i want to make this a tree nav or something
	private void interpret(List<ISymbol> axiom) {
		foreach (ISymbol sym in axiom) {
			//GD.Print("Interpreting: ", sym);
			if (sym is Symbol) {
				Symbol castedSym = (Symbol)sym;
				//GD.Print("ID: ", castedSym.ID);
				switch (castedSym.ID) {
					// Create a road
					case "A":
						//GD.Print("Add a road");
						addRoad(castedSym.RoadAttr.Position, castedSym.RoadAttr.LookPosition);
						break;
					case "Br":
						addBridge(castedSym.RoadAttr.Position, castedSym.RoadAttr.LookPosition);
						break;
					case "T1":
						addTunnelStart(castedSym.RoadAttr.Position, castedSym.RoadAttr.LookPosition);
						break;
					case "T2":
						addTunnelEnd(castedSym.RoadAttr.Position, castedSym.RoadAttr.LookPosition);
						break;
					case "T":
						addTunnel(castedSym.RoadAttr.Position, castedSym.RoadAttr.LookPosition);
						break;
				}
			}
			// Branch and save position
			else if (sym is SymBranch) {
				SymBranch castedSym = (SymBranch)sym;
				// Interpret symbols in this branch
				interpret(castedSym.Syms);
			}
		}
	}
	

	/* --------------------------- 
	--------- Road Funcs ---------
	------------------------------ */

	// Draw a road forward
	// Same as rule +F (Rotate by angle, draw a forward line by length)
	private void addRoad(Vector3 pos, Vector3 lookPos) {
		// Create new road and set position
		//GD.Print("Adding a road at: ", pos);
		Node3D newRoad = (Node3D)Road.Instantiate();
		newRoad.Translate(pos);

		if (!pos.Equals(lookPos)) {
			newRoad.LookAtFromPosition(pos, lookPos);
		} else {
		//	newRoad.Translate(pos);
		}
		RoadList.AddChild(newRoad);
	}

	// Draw a bridge forward
	// Same as rule +F (Rotate by angle, draw a forward line by length)
	private void addBridge(Vector3 pos, Vector3 lookPos) {
		// Create new road and set position
		//GD.Print("Adding a road at: ", pos);
		Node3D newBridge = (Node3D)Bridge.Instantiate();
		//newRoad.Translate(pos);

		if (!pos.Equals(lookPos)) {
			newBridge.LookAtFromPosition(pos, lookPos);
		} else {
			newBridge.Translate(pos);
		}
		RoadList.AddChild(newBridge);
	}


	// Draw a tunnel forward
	// Same as rule +F (Rotate by angle, draw a forward line by length)
	private void addTunnel(Vector3 pos, Vector3 lookPos) {
		// Create new road and set position
		//GD.Print("Adding a road at: ", pos);
		Node3D newTunnel = (Node3D)Tunnel.Instantiate();
		//newRoad.Translate(pos);

		if (!pos.Equals(lookPos)) {
			newTunnel.LookAtFromPosition(pos, lookPos);
		} else {
			newTunnel.Translate(pos);
		}
		RoadList.AddChild(newTunnel);
	}

	// Draw a tunnel forward
	// Same as rule +F (Rotate by angle, draw a forward line by length)
	private void addTunnelStart(Vector3 pos, Vector3 lookPos) {
		// Create new road and set position
		//GD.Print("Adding a road at: ", pos);
		Node3D newTunnelStart = (Node3D)TunnelStart.Instantiate();
		//newRoad.Translate(pos);

		if (!pos.Equals(lookPos)) {
			newTunnelStart.LookAtFromPosition(pos, lookPos);
		} else {
			newTunnelStart.Translate(pos);
		}
		RoadList.AddChild(newTunnelStart);
	}

		// Draw a tunnel forward
	// Same as rule +F (Rotate by angle, draw a forward line by length)
	private void addTunnelEnd(Vector3 pos, Vector3 lookPos) {
		// Create new road and set position
		//GD.Print("Adding a road at: ", pos);
		Node3D newBridge = (Node3D)TunnelEnd.Instantiate();
		//newRoad.Translate(pos);

		if (!pos.Equals(lookPos)) {
			newBridge.LookAtFromPosition(pos, lookPos);
		} else {
			newBridge.Translate(pos);
		}
		RoadList.AddChild(newBridge);
	}

	/* --------------------------- 
	--------- Dest Funcs ---------
	------------------------------ */

	private void randomizeDests() {
		Vector3 boundsSize = Bounds.GetAabb().Size;
		for (int i = 0; i < 3; i++) {
			int x = GD.RandRange((int)-boundsSize.X/3, (int)boundsSize.X/3);
			int z = GD.RandRange((int)-boundsSize.Z/3, (int)boundsSize.Z/3);
			int y = 0;//(int)getNearestSurface(new Vector3(x, 0, z)).Y;
			Vector3 placePos = new Vector3(x, y, z);
			GD.Print("Placing at: ", placePos);
			addDest(placePos);
		}
	}

	private void addDest(Vector3 pos) {
		Node3D newDest = (Node3D)Destination.Instantiate();
		newDest.Translate(pos);
		DestList.AddChild(newDest);
	}

	// Get nearest surface to the given position
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
