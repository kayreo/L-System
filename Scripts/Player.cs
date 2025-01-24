using Godot;

public partial class Player : CharacterBody3D
{
	public const float Speed = 50.0f;
	public const float JumpVelocity = 50.5f;

	public Camera3D cam;

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		cam = GetNode<Camera3D>("Camera3D");

		if (Input.IsActionJustPressed("Ascend"))
		{
			velocity.Y = JumpVelocity;
		}
		else if (Input.IsActionJustPressed("Descend"))
		{
			velocity.Y = -JumpVelocity;
		}
		else if (Input.IsActionJustReleased("Ascend") || Input.IsActionJustReleased("Descend")) {
			velocity.Y = 0;
		}


		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("Left", "Right", "Up", "Down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}


	public override void _Input(InputEvent @event)
{
    if (@event is InputEventMouseMotion mouseEvent)
    {
		Rotate(Vector3.Up, -mouseEvent.Relative.X * 0.005f);
		cam.RotateX(-mouseEvent.Relative.Y * 0.005f);	
    }
}
}
