using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
//code from https://www.youtube.com/watch?v=rWeQ30h25Yg
public partial class World : Node3D
{
	const int chunkSize = 256;
	const int chunkAmount = 16;

	Player player;

	FastNoiseLite noise = new FastNoiseLite();

	Godot.Collections.Dictionary chunks = new Godot.Collections.Dictionary {

	};

	Godot.Collections.Dictionary unreadyChunks = new Godot.Collections.Dictionary {

	};

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetNode<Player>("Player");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//updateChunks();
	}

	private void addChunk(float x, float z) {
		string key = x.ToString() + ", " + z.ToString();
		if (chunks.ContainsKey(key) || unreadyChunks.ContainsKey(key)) {
			return;
		}
		Godot.Collections.Array arr = new Godot.Collections.Array();
		arr.Add(x);
		arr.Add(z);
		loadChunk(arr);
		unreadyChunks[key] = 1;
	}

	private void loadChunk(Godot.Collections.Array arr) {
		int x = (int)arr[0];
		int z = (int)arr[1];

		Terrain newChunk = new Terrain();
		newChunk.Translate(new Vector3(x * chunkSize, 0, z * chunkSize));
		CallDeferred("loadComplete", newChunk);
	}

	private void loadComplete(Terrain newChunk) {
		AddChild(newChunk);
		var key = (newChunk.Position.X / chunkSize).ToString() + ", " + (newChunk.Position.Z / chunkSize).ToString();
		chunks[key] = newChunk;
		unreadyChunks.Remove(key);
	}

	private Terrain getChunk(float x, float z) {
		string key = x.ToString() + ", " + z.ToString();
		if (chunks.ContainsKey(key)) {
			return (Terrain)chunks[key];
		}
		return null;
	}

	private void updateChunks() {
		Vector3 playerPos = player.Position;
		float xStart = playerPos.X - chunkAmount * 0.5f;
		float xEnd = playerPos.X + chunkAmount * 0.5f;

		float zStart = playerPos.Z - chunkAmount * 0.5f;
		float zEnd = playerPos.Z + chunkAmount * 0.5f;

		for (float x = xStart; x < xEnd; x++) {
			for (float z = zStart; z < zEnd; z++) {
				addChunk(x, z);
			}
		}
	}
}
