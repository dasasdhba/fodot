using Godot;

namespace GodotBridge;

public partial class PreDeleteBridge : Node
{
    [Signal]
    public delegate void PreDeletedEventHandler();

    public override void _Notification(int what)
    {
        base._Notification(what);
        
        if ((ulong)what == NotificationPredelete)
        {
            EmitSignalPreDeleted();
        }
    }
}