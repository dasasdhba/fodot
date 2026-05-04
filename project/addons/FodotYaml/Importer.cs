using System.IO;
using Godot;
using Godot.Collections;

namespace GodotCSharp.FodotYaml;

[Tool]
public partial class Importer : EditorImportPlugin
{
#if TOOLS && DEBUG

    public override bool _CanImportThreaded()
        => true;
    
    public override string _GetImporterName()
        => "fodot.yaml.plugin";

    public override string _GetVisibleName()
        => "Fodot Yaml Importer";

    public override string[] _GetRecognizedExtensions()
        => [ "yaml" ];

    public override string _GetSaveExtension()
        => "gd";

    public override string _GetResourceType()
        => "GDScript";

    public override int _GetPresetCount() => 1;

    public override string _GetPresetName(int _)
        => "Default";

    public override float _GetPriority() => 1.0f;

    public override int _GetImportOrder() => 1;

    public override Array<Dictionary> _GetImportOptions(string path, int presetIndex)
        => [];
    
    public override Error _Import(string sourceFile, string savePath,
        Dictionary options, Array<string> _, Array<string> __)
    {
        var path = ProjectSettings.GlobalizePath(sourceFile);
        var final = savePath + "." + _GetSaveExtension();
        var code = Fodot.Generator.Parser.createGdString(path);
        
        var script = new GDScript();
        script.SourceCode = code;
        script.ResourcePath = final;
        var err = script.Reload();
        if (err != Error.Ok)
        {
            GD.PrintErr($"YAML import failed: ({err})");
            return err;
        }
        
        var root = ProjectSettings.GlobalizePath("res://project.godot").GetBaseDir();
        var dir = Directory.GetParent(root)?.FullName;
        var binding = $@"{dir}\Fodot.Bind\Bind.fs";
        Fodot.Generator.Parser.createFsBinding(root, binding);
            
        ResourceSaver.Save(script, final);

        return Error.Ok;
    }

#endif    
}