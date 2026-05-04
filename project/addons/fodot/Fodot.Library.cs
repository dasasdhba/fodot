using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fodot.Generator;
using Godot;
using Godot.Collections;
using FileAccess = Godot.FileAccess;

namespace GodotCSharp;

public partial class FodotMain
{
#if TOOLS
    
    private Array<string> _cachedUnlib = [];
    private Godot.Collections.Dictionary<string, Resource> _cachedLib =[];
    private Godot.Collections.Dictionary<string, string> _cachedMd5 = [];

    private static void LibPrint(string str)
    {
        GD.PrintRich($"[color=DARK_GRAY][Library] {str}[/color]");
    }

    private void LoadLibrary(string path)
    {
        var dir = DirAccess.Open(path);
        foreach (var f in dir.GetFiles()
            .Select(u => path + "/" + u)
            .Where(u => u.GetExtension() == "tres" 
                && !_cachedLib.ContainsKey(u) && !_cachedUnlib.Contains(u) ))
        {
            var res = GD.Load(f);
            if (res.HasMethod("get_fs_content"))
            {
                _cachedLib.Add(f, res);
            }
            else
            {
                _cachedUnlib.Add(f);
            }
        }

        foreach (var d in dir.GetDirectories())
        {
            LoadLibrary(path + "/" + d);
        }
    }

    private void UpdateLibrary()
    {
        var updated = false;
    
        foreach (var k in _cachedLib.Keys)
        {
            if (!FileAccess.FileExists(k))
            {
                _cachedLib.Remove(k);
                _cachedMd5.Remove(k);
                updated = true;
                continue;
            }
            
            var md = FileAccess.GetMd5(k);
            if (_cachedMd5.GetValueOrDefault(k, "") != md)
            {
                var res = _cachedLib[k];
                var globalPath = ProjectSettings.GlobalizePath(k);
                var globalDir = Path.GetDirectoryName(globalPath);
                var parent = Parser.findParentFsproj(globalDir);
                res.SetMeta("_fs_yaml_parent", parent);
                _cachedMd5[k] = md;
                updated = true;
            }
        }
        
        if (!updated) return;
        
        var total = new System.Collections.Generic.Dictionary<string, List<Resource>>();
        foreach (var k in _cachedLib.Keys)
        {
            var res = _cachedLib[k];
            var parent = _cachedLib[k].GetMeta("_fs_yaml_parent", "null").As<string>();
            if (!total.TryGetValue(parent, out var l))
            {
                total.Add(parent, [res]);
            }
            else
            {
                l.Add(res);
            }
        }

        foreach (var p in total.Keys)
        {
            var files = total[p];
            if (p == "null")
            {
                var content = string.Join(", ", files);
                LibPrint($"Cannot find parent fsproj for {content}.\nLibrary will not be created.");
                continue;
            }
            
            var codes = files
                .Select(r => r.Call("get_fs_content").AsString());
            var text = string.Join("\n\n", codes);
            
            var name = Path.GetFileNameWithoutExtension(p);
            var fullCode = 
                $"namespace {name}.Library\n\n" +
                "open Fodot.Core\n" +
                "open Godot\n\n" +
                text;
                
            var file = Path.GetDirectoryName(p) + "/Library.fs";
            File.WriteAllText(file, fullCode);
            Parser.addCompileItem("Library", p);
            LibPrint($"Generated {files.Count} library module for {name}");
        }
    }
    
    private bool _shouldLoadLib = true;
    private bool _shouldKillThread = false;
    private Semaphore _onUpdateLib = new();
    
    private void NotifyUpdateLibrary() => _onUpdateLib.Post();

    private void ConnectToFilesystem()
    {
        _shouldLoadLib = true;
        NotifyUpdateLibrary();
    }

    private void UpdateLibOnThread()
    {
        while (true)
        {
            if (_shouldKillThread) return;
            
            if (_shouldLoadLib)
            {
                _shouldLoadLib = false;
                LoadLibrary("res://"); 
            }
            
            UpdateLibrary();
            _onUpdateLib.Wait();
        }
    }
    
    private GodotThread _libThread;
    private const string CacheCfg = "res://.godot/fodot_lib_cache.cfg";

    private void LibInit()
    {
        EditorInterface.Singleton.GetResourceFilesystem().FilesystemChanged += ConnectToFilesystem;
    
        var cfg = new ConfigFile();
        if (cfg.Load(CacheCfg) == Error.Ok)
        {
            _cachedLib = cfg.GetValue("cache", "lib").AsGodotDictionary<string, Resource>();
            _cachedMd5 = cfg.GetValue("cache", "md5").AsGodotDictionary<string, string>();
            _cachedUnlib = cfg.GetValue("cache", "unlib").AsGodotArray<string>();
        }
        _libThread = new();
        _libThread.Start(Callable.From(UpdateLibOnThread));
    }

    private void LibExit()
    {
        EditorInterface.Singleton.GetResourceFilesystem().FilesystemChanged -= ConnectToFilesystem;
    
        _shouldKillThread = true;
        NotifyUpdateLibrary();
        _libThread.WaitToFinish();
        var cfg = new ConfigFile();
        cfg.SetValue("cache", "lib", _cachedLib);
        cfg.SetValue("cache", "md5", _cachedMd5);
        cfg.SetValue("cache", "unlib", _cachedUnlib);
        cfg.Save(CacheCfg);
    }
    
    private double _libTimer = 0d;

    private void ProcessLib(double delta)
    {
        if (_shouldKillThread) return;
    
        var schedule = LibraryScheduleTime;
        _libTimer += delta;
        if (_libTimer >= schedule)
        {
            _libTimer -= schedule;
            NotifyUpdateLibrary();
        }
    }

#endif        
}