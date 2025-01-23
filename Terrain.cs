using Godot;
using Godot.NativeInterop;
using System;

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

		Godot.Collections.Array planeArrays = plane.GetMeshArrays();
		ArrayMesh arrayMesh = new ArrayMesh();
		godot_packed_vector3_array vertexArray = planeArrays[ArrayMesh.ArrayType.Vertex];
		godot_packed_vector3_array normalArray = planeArrays[ArrayMesh.ArrayType.Normal];
		godot_packed_float32_array tangentArray = planeArrays[ArrayMesh.ArrayType.Tangent];
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, planeArrays);
		Mesh = arrayMesh;
		GD.Print(Mesh._GetAabb().Size);
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
