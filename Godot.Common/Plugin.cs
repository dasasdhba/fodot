using Godot.Collections;

namespace Godot.Common;

#if TOOLS

public static class Plugin
{

    public static void AddProjectSetting(string key, Variant @default, Variant.Type type, PropertyHint hint = PropertyHint.None, string hintStr = "")
    {
        if (!ProjectSettings.HasSetting(key))
        {
            ProjectSettings.SetSetting(key, @default);
        }
        
        ProjectSettings.SetInitialValue(key, @default);
        Dictionary info = new()
        {
            { "name", key },
            { "type", (int)type },
            { "hint", (int)hint },
            { "hint_string", hintStr }
        };
        ProjectSettings.AddPropertyInfo(info);
    }

    public static T GetProjectSetting<[MustBeVariant] T>(string key, T @default = default)
    {
        if (!ProjectSettings.HasSetting(key))
        {
            return @default;
        }
        
        return ProjectSettings.GetSetting(key).As<T>();
    }
    
}

#endif