using Godot;
using Godot.Collections;

namespace GodotCSharp.GdYaml;

[Tool]
public partial class Importer : EditorImportPlugin
{
#if TOOLS

    public override bool _CanImportThreaded()
        => true;
    
    public override string _GetImporterName()
        => "gdyaml.plugin";

    public override string _GetVisibleName()
        => "Gdscript Yaml Importer";

    public override string[] _GetRecognizedExtensions()
        => new string[] { "yaml" };

    public override string _GetSaveExtension()
        => "gd";

    public override string _GetResourceType()
        => "GDScript";

    public override int _GetPresetCount() => 1;

    public override string _GetPresetName(int _)
        => "Default";

    public override float _GetPriority() => 1.0f;

    public override int _GetImportOrder() => 1;

    public override Error _Import(string sourceFile, string savePath,
        Dictionary options, Array<string> _, Array<string> __)
    {
        var script = new GDScript();
        
        // TODO: generate
        script.SourceCode = "";
            
        ResourceSaver.Save(script, savePath + "." + _GetSaveExtension());

        return Error.Ok;
    }

#endif    
}