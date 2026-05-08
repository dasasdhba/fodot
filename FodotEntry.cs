using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Godot;

public partial class FodotEntry : Node
{
#if TOOLS
    [GeneratedRegex("""
                    "[^"]*\.fsproj"
                    """)]
    private static partial Regex FsprojRegex();
#endif

    public static HashSet<string> RipFsproj()
    {
    
    #if TOOLS
        
        using var f = FileAccess.Open("res://Godot.csproj", FileAccess.ModeFlags.Read);
        var result = FsprojRegex()
            .Matches(f.GetAsText())
            .Select(r => Path.GetFileNameWithoutExtension(r.Value[1..^1]))
            .ToHashSet();
        
        using var w = FileAccess.Open("res://fodot_build.dat", FileAccess.ModeFlags.Write);
        foreach (var r in result) w.StoreLine(r);
        
        return result;
        
    #else
    
        using var f = FileAccess.Open("res://fodot_build.dat", FileAccess.ModeFlags.Read);
        return f.GetAsText().Split('\n').ToHashSet();
    
    #endif    
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        
        var asm = RipFsproj();
        var loaded = asm.Select(s => Assembly.Load(s.Trim())).ToArray();
        
        Fodot.Core.FScript.setAssemblies(loaded);
        Fodot.Core.Engine.getTree();
        QueueFree();
    }
}