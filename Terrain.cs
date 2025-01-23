// Code changed from https://www.youtube.com/watch?v=6qim01M1Yp0

using Godot;
using Godot.NativeInterop;
using System;
using Godot.Collections;
using System.Linq;

[Tool]
public partial class Terrain : MeshInstance3D
{
	const float size = 256.0f;

	private int _resolution = 32;

	private FastNoiseLite _noise;

	private float _height = 64.0f;

	[Export]
	public float Height 
	{
		get => _height;
		set
		{
			_height = value;
			updateMesh();
		}
	}

	[Export]
	public FastNoiseLite Noise
	{
		get => _noise;
		set
		{
			_noise = value;
			updateMesh();
			if (_noise != null) {
				_noise.Changed += updateMesh;
			}
		}
	}

    [Export]
    public int Resolution
    {
        get => _resolution;
        set
        {
            _resolution = value;
			updateMesh();
        }
    }

	private float getHeight(float x, float y) {
		return _noise.GetNoise2D(x, y) * _height;
	}

	private Vector3 getNormal(float x, float y) {
		float epsilon = size / _resolution;
		Vector3 normal = new Vector3(
			getHeight(x + epsilon, y) - getHeight(x - epsilon, y) / (2.0f * epsilon),
			1.0f,
			getHeight(x, y + epsilon) - getHeight(x, y - epsilon) / (2.0f * epsilon)
			);
		return normal.Normalized();
	}

	private void updateMesh() {
		PlaneMesh plane = new PlaneMesh();
		plane.SubdivideDepth = _resolution;
		plane.SubdivideWidth = _resolution;
		plane.Size = new Vector2(size, size);

		Godot.Collections.Array planeArrays = plane.GetMeshArrays();
		ArrayMesh arrayMesh = new ArrayMesh();
		Vector3[] vertexArray = planeArrays[0].As<Vector3[]>();
		Vector3[] normalArray = planeArrays[1].As<Vector3[]>();
		float[] tangentArray = planeArrays[2].As<float[]>();
		GD.Print();
		GD.Print("old: ", planeArrays[0].As<Vector3[]>()[0]);

		for (int i = 0; i < vertexArray.Length; i++) {
			Vector3 vertex = vertexArray[i];
			if (i == 10) {
				GD.Print("Old vertex y: ", vertex.Y);
			}
			Vector3 normal = Vector3.Up;
			Vector3 tangent = Vector3.Right;
			if (_noise != null) {
				vertex.Y = getHeight(vertex.X, vertex.Z);
				normal = getNormal(vertex.X, vertex.Z);
				tangent = normal.Cross(Vector3.Up);
			}
			if (i == 10) {
				GD.Print("New vertex y: ", vertex.Y);
			}
			vertexArray[i] = vertex;
			normalArray[i] = normal;
			tangentArray[4 * i] = tangent.X;
			tangentArray[4 * i + 1] = tangent.Y;
			tangentArray[4 * i + 2] = tangent.Z;
		}
		
		planeArrays[0] = vertexArray;
		planeArrays[1] = normalArray;
		planeArrays[2] = tangentArray;
		GD.Print("New2: ", planeArrays[0].As<Vector3[]>()[10]);
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, planeArrays);
		Mesh = arrayMesh;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
