using Godot;

namespace Mono;

public partial class CoreSingleton : Node
{
    public static CoreSingleton Instance { get; private set; }

    public CoreSingleton() : base()
    {
        Instance = this;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        GetTree().NodeAdded += node =>
        {
            Lib.Core.FScript.init(node);
        };
    }
}