using Godot;

namespace GodotCSharp;

public partial class Bridge : Node
{
    public override void _EnterTree()
    {
        base._EnterTree();
        Fodot.Core.Engine.getTree();
        QueueFree();
    }
}