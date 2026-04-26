using System;
using System.Linq;
using System.Reflection;
using Godot;

namespace GodotCSharp;

public partial class Bridge : Node
{
    public override void _EnterTree()
    {
        base._EnterTree();
        
        var asm = 
            ProjectSettings.GetSetting("fodot/general/assemblies", "")
                .AsString()
                .Split(["\n", "\r", "\r\n"], StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();
        
        asm.Add("Fodot");
        var loaded = asm.Select(s => Assembly.Load(s.Trim())).ToArray();
        
        Fodot.Core.FScript.setAssemblies(loaded);
        Fodot.Core.Engine.getTree();
        QueueFree();
    }
}