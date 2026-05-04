using Godot;

namespace GodotCSharp.FodotYaml;

[Tool]
public partial class FodotYaml : EditorPlugin
{
#if TOOLS && DEBUG

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