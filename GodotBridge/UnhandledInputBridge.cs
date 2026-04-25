using Godot;

namespace GodotBridge;

public partial class UnhandledInputBridge : Node
{
    [Signal]
    public delegate void UnhandledInputEventHandler(InputEvent @event);

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        EmitSignalUnhandledInput(@event);
    }
}