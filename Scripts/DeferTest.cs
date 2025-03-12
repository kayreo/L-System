using Godot;
using System;

public partial class DeferTest : Node3D
{
    [Signal]
    public delegate void DeferredMethodCompletedEventHandler();

    public override async void _Ready()
    {
        GD.Print("Start deferred call...");

        // Call the deferred method
        CallDeferred("DeferredMethod");

        // Wait for the DeferredMethodCompleted signal to be emitted
        await ToSignal(this, "DeferredMethodCompleted");

        GD.Print("Deferred method completed!");
    }

    private void DeferredMethod()
    {
        GD.Print("Inside deferred method!");

        // Emit signal after the deferred method is executed
        EmitSignal("DeferredMethodCompleted");
    }
}
