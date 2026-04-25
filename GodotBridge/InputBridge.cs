using Godot;

namespace GodotBridge;

public partial class InputBridge : Node
{
    [Signal]
    public delegate void InputEventHandler(InputEvent @event);

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        EmitSignalInput(@event);
    }
}