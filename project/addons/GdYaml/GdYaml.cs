using Godot;

namespace GodotCSharp.GdYaml;

[Tool]
public partial class GdYaml : EditorPlugin
{
#if TOOLS

    private Importer _importer;

    public override void _EnterTree()
    {
        base._EnterTree();
        _importer = new Importer();
        AddImportPlugin(_importer);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        RemoveImportPlugin(_importer);
    }
    
#endif    
}