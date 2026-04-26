using Godot;

namespace GodotBridge;

public partial class DeleteBridge : Node
{
    [Signal]
    public delegate void DeletedEventHandler();

    public override void _Notification(int what)
    {
        base._Notification(what);
        
        if ((ulong)what == NotificationPredelete)
        {
            EmitSignalDeleted();
        }
    }
}