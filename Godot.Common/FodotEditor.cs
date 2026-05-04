namespace Godot.Common;

public static class FodotEditor
{
    public const string MainSceneKey = "fodot/general/main_scene";
    public static string ProjectMainScene => Plugin.GetProjectSetting(MainSceneKey, "");
    
#if TOOLS

    public const string DebugScenePath = "res://fodot_debug_scene";
    
#endif
}