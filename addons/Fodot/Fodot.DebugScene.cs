using Godot.Common;

namespace Godot.FodotPlugin;

#if TOOLS

public partial class FodotMain
{

    private static string DebugScenePath => FodotEditor.DebugScenePath;
    private string _lastPath = "";

    private void UpdateDebugScene()
    {
        var root = EditorInterface.Singleton.GetEditedSceneRoot();
        if (root == null) return;
        
        var path = root.SceneFilePath;
        if (path == _lastPath) return;
        _lastPath = path;
        
        using var f = FileAccess.Open(DebugScenePath, FileAccess.ModeFlags.Write);
        f.StoreLine(path);
        f.Close();
    }
    
}

#endif    