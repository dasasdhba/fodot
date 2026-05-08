# Fodot

I'm trying some werid patterns to make `F#` work with Godot. As for now, we can actually use most of C# bindings, except that gd virtual function will not work at all. Some common choices are using `C#` as a bridge and one can make source generator to do that. In this project, I'm testing some other approaches. The main idea is to make use of `.NET` reflection to implement a custom scripting system, e.g., by assignning `"test_script"` as both a metadata of a `GodotObject` and an attribute data of a `F#` class, we can actually attach this `F#` class as a script to the `GodotObject`.

This approach does work, but it lacks of testings. On the other hand, one of main issues is that we cannot use any editor feature of godot in this approach, so I implement a simple source generator, which reads `yaml` config file and convert it to both `gdscript` with export properites and `F#` bindings.

However, referencing some other Godot addons and packages can still be a problem. For `gdscript` it might be ok, as things will not be better if you are using `C#`. For `C#` packages, the main issue is caused by cycling reference. As you can see, `F#` library needs to be referenced in the Godot `C#` entries, so it cannot reference anything from Godot `C#` project. This problem can be solved if Godot finally supports multiple `C#` projects.
