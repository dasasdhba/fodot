using System;
using System.Collections.Generic;
using System.Linq;
using Godot.Common;

namespace Godot;

[Tool]
public partial class FodotMain : EditorPlugin
{
    private const string AssemblyKey = "fodot/general/assemblies";
    
    public static HashSet<string> ProjectAssemblies => 
        Plugin.GetProjectSetting(AssemblyKey, "")
            .Split(["\n", "\r", "\r\n"], StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

#if TOOLS    
    
    private static string MainSceneKey => FodotEditor.MainSceneKey;
    private const string LibraryKey = "fodot/general/library_schedule_time";
    private const string BridgeKey = "Fodot";
    private const string BridgeFile = "FodotEntry.cs";

    private static float LibraryScheduleTime => Plugin.GetProjectSetting(LibraryKey, 3.0f);
    
    private FodotYamlImporter _importer;
    
    public override void _EnterTree()
    {
        Plugin.AddProjectSetting(MainSceneKey, "", Variant.Type.String, 
            PropertyHint.File, "*.tscn,*.scn,*.res");
        Plugin.AddProjectSetting(AssemblyKey, "", Variant.Type.String,
            PropertyHint.MultilineText);
        Plugin.AddProjectSetting(LibraryKey, 3.0, Variant.Type.Float,
            PropertyHint.Range, "0,60,0.5");
            
        _importer = new();
        AddImportPlugin(_importer);
        
        LibInit();
    }

    public override void _ExitTree()
    {
        RemoveImportPlugin(_importer);
        
        LibExit();
    }

    public override void _EnablePlugin()
    {
        AddAutoloadSingleton(BridgeKey, BridgeFile);
    }

    public override void _DisablePlugin()
    {
        ProjectSettings.Clear(MainSceneKey);
        ProjectSettings.Clear(AssemblyKey);
        ProjectSettings.Clear(LibraryKey);
        RemoveAutoloadSingleton(BridgeKey);
    }

    public override void _Process(double delta)
    {
        UpdateDebugScene();
        ProcessLib(delta);
    }

#endif    
}