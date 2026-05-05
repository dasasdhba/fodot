using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fodot.Generator;
using Godot.Collections;

namespace Godot;

#if TOOLS

public partial class FodotMain
{
    
    private Array<string> _cachedUnlib = [];
    private Collections.Dictionary<string, Resource> _cachedLib =[];
    private Collections.Dictionary<string, string> _cachedMd5 = [];

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
                _cachedMd5[k] = md;
                updated = true;
            }
        }
        
        if (!updated) return;
        
        var total = new System.Collections.Generic.Dictionary<string, List<Resource>>();
        var cachedParent = new System.Collections.Generic.Dictionary<string, string>();
        
        foreach (var k in _cachedLib.Keys)
        {
            var res = _cachedLib[k];
            var globalPath = ProjectSettings.GlobalizePath(k);
            var globalDir = Path.GetDirectoryName(globalPath) ?? "null";
            string parent;
            if (cachedParent.TryGetValue(globalDir, out var p))
            {
                parent = p;
            }
            else
            {
                parent = Parser.findParentFsproj(globalDir);
                cachedParent.Add(globalDir, parent);
            }
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
                "open Godot\n" +
                "open Godot.Collections\n\n" +
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
    
}

#endif