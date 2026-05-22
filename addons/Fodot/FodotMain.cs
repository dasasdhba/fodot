using Godot.Common;

namespace Godot.FodotPlugin;

[Tool]
#if TOOLS
public partial class FodotMain : EditorPlugin, ISerializationListener
#else
public partial class FodotMain : EditorPlugin
#endif
{
#if TOOLS    
    
    private static string MainSceneKey => FodotEditor.MainSceneKey;
    private const string LibraryKey = "fodot/general/library_schedule_time";
    private const string BridgeKey = "Fodot";
    private const string BridgeFile = "res://FodotEntry.cs";

    private static float LibraryScheduleTime => Plugin.GetProjectSetting(LibraryKey, 3.0f);
    
    public void OnAfterDeserialize()
    {
        Plugin.AddProjectSetting(MainSceneKey, "", Variant.Type.String, 
            PropertyHint.File, "*.tscn,*.scn,*.res");
        Plugin.AddProjectSetting(LibraryKey, 3.0, Variant.Type.Float,
            PropertyHint.Range, "0,60,0.5");
        
        LibInit();
    }
    
    public void OnBeforeSerialize()
    {
        LibExit();
    }

    public override void _EnterTree()
    {
        OnAfterDeserialize();
    }

    public override void _ExitTree()
    {
        OnBeforeSerialize();
    }

    public override void _EnablePlugin()
    {
        AddAutoloadSingleton(BridgeKey, BridgeFile);
    }

    public override void _DisablePlugin()
    {
        ProjectSettings.Clear(MainSceneKey);
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