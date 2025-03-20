// Code changed from https://www.youtube.com/watch?v=6qim01M1Yp0

using Godot;
using Godot.NativeInterop;
using System;
using Godot.Collections;
using System.Linq;

//[Tool]
public partial class Terrain : MeshInstance3D
{
	private float _size = 256.0f;

	private int _resolution = 32;

	private FastNoiseLite _noise = new FastNoiseLite();

	private float _height = 64.0f;

	[Export]
	public float Size
	{
		get => _size;
		set
		{
			_size = value;
			updateMesh();
		}
	}

	[Export]
	public float Height 
	{
		get => _height;
		set
		{
			_height = value;
			(MaterialOverride as ShaderMaterial).SetShaderParameter("height", _height * 2);
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
		float epsilon = _size / _resolution;
		Vector3 normal = new Vector3(
			(getHeight(x + epsilon, y) - getHeight(x - epsilon, y)) / (2.0f * epsilon),
			1.0f,
			(getHeight(x, y + epsilon) - getHeight(x, y - epsilon)) / (2.0f * epsilon)
			);
		return normal.Normalized();
	}

	private void updateMesh() {
		PlaneMesh plane = new PlaneMesh();
		plane.SubdivideDepth = _resolution;
		plane.SubdivideWidth = _resolution;
		plane.Size = new Vector2(_size, _size);

		Godot.Collections.Array planeArrays = plane.GetMeshArrays();
		ArrayMesh arrayMesh = new ArrayMesh();
		Vector3[] vertexArray = planeArrays[0].As<Vector3[]>();
		Vector3[] normalArray = planeArrays[1].As<Vector3[]>();
		float[] tangentArray = planeArrays[2].As<float[]>();

		for (int i = 0; i < vertexArray.Length; i++) {
			Vector3 vertex = vertexArray[i];
			Vector3 normal = Vector3.Up;
			Vector3 tangent = Vector3.Right;
			if (_noise != null) {
				vertex.Y = getHeight(vertex.X, vertex.Z);
				normal = getNormal(vertex.X, vertex.Z);
				tangent = normal.Cross(Vector3.Up);
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

		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, planeArrays);
		Mesh = arrayMesh;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (GetParent().GetNode<CollisionShape3D>("TerrainCollision") != null) {
			GetParent().GetNode<CollisionShape3D>("TerrainCollision").EmitSignal("UpdateCollision", Mesh.GetFaces(), _size);
		}
		if (GetParent().GetParent().GetNode<MeshInstance3D>("Water") != null) {
			BoxMesh waterPlane = new BoxMesh();
			waterPlane.Size = new Vector3(_size, 15, _size);
			GetParent().GetParent().GetNode<MeshInstance3D>("Water").Mesh = waterPlane;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
