using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fodot.Generator;
using Godot.Collections;

namespace Godot.FodotPlugin;

#if TOOLS

public partial class FodotMain
{

    private Array<string> _cachedUnlib = [];
    private Collections.Dictionary<string, Resource> _cachedLib = [];
    private Collections.Dictionary<string, string> _cachedLibMd5 = [];
    private Collections.Dictionary<string, string> _cachedYaml = [];
    private Collections.Dictionary<string, string> _cachedParents = [];

    private static void LibPrint(string str)
    {
        GD.PrintRich($"[color=DARK_GRAY][Library] {str}[/color]");
    }

    private void LoadLibrary(string path)
    {
        var dir = DirAccess.Open(path);
        foreach (var f in dir.GetFiles().Select(u => path + "/" + u))
        {
            if (f.EndsWith(".gd.yaml"))
            {
                _cachedYaml.TryAdd(f, "");
            }
            else if (f.GetExtension() == "tres" && !_cachedLib.ContainsKey(f) && !_cachedUnlib.Contains(f))
            {
                var res = GD.Load(f);
                if (res.HasMethod("get_fs_content"))
                {
                    _cachedLib.Add(f, res);
                }
                else if (res.GetClass() != "Resource")
                {
                    _cachedUnlib.Add(f);
                }
            }
        }

        foreach (var d in dir.GetDirectories())
        {
            LoadLibrary(path + "/" + d);
        }
    }
    
    private HashSet<string> GetPendingParents<T>(IDictionary<string, T> res, IDictionary<string, string> md5)
    {
        HashSet<string> result = [];
        
        foreach (var k in res.Keys)
        {
            var parent = _cachedParents.GetValueOrDefault(k, "null");
            
            if (!FileAccess.FileExists(k))
            {
                res.Remove(k);
                md5.Remove(k);
                _cachedParents.Remove(k);
                result.Add(parent);
                continue;
            }
            
            var md = FileAccess.GetMd5(k);
            if (!md5.TryGetValue(k, out string value) || md != value || !FileAccess.FileExists(parent))
            {
                md5[k] = md;
                var path = ProjectSettings.GlobalizePath(k);
                var dir = Path.GetDirectoryName(path);
                var globalParent = Parser.findParentFsproj(dir);
                var newParent = globalParent == "null" ? "null" : 
                    ProjectSettings.LocalizePath(globalParent);
                _cachedParents[k] = newParent;
                result.Add(parent);
                result.Add(newParent);
            }
        }
        
        return result;
    }

    private record struct UpdateData<T>
    {
        public IDictionary<string, T> ResDict { get; init; }
        public IDictionary<string, string> Md5Dict { get; init; }
        public Func<string, string> CodeGenerator { get; init; }
        public string CodeFileName { get; init; }
        public string ConsoleHintAdd { get; init; }
        public string ConsoleHintRemove { get; init; }
    }
    
    private void UpdateWith<T>(UpdateData<T> data)
    {
        var parents = GetPendingParents(data.ResDict, data.Md5Dict);
        
        foreach (var p in parents.Where(FileAccess.FileExists))
        {
            var files = data.ResDict.Keys
                .Where(k => _cachedParents.GetValueOrDefault(k, "null") == p)
                .ToArray();
            
            var path = ProjectSettings.GlobalizePath(p);
            var name = Path.GetFileNameWithoutExtension(path);
            var file = Path.GetDirectoryName(path) + $"/{data.CodeFileName}.fs";
            
            if (files.Length == 0)
            {
                File.Delete(file);
                Parser.removeCompileItem(data.CodeFileName, path);
                LibPrint(string.Format(data.ConsoleHintRemove, name));
            }
            else
            {
                var codes = files.Select(data.CodeGenerator)
                    .Where(c => c != "").ToArray();
                var text = string.Join("\n\n", codes);
                
                var fullCode = 
                    $"namespace {name}.{data.CodeFileName}\n\n" +
                    "open Fodot.Core\n" +
                    "open Godot\n\n" +
                    text;
            
                File.WriteAllText(file, fullCode);
                Parser.addCompileItem(data.CodeFileName, path);
                LibPrint(string.Format(data.ConsoleHintAdd, codes.Length, name));
            }
        }
    }

    private void UpdateYaml()
    {
        var scan = false;
    
        var info = new UpdateData<string>
        {
            ResDict = _cachedYaml,
            Md5Dict = _cachedYaml,
            CodeGenerator = k =>
            {
                try
                {
                    var p = ProjectSettings.GlobalizePath(k);
                    var gd = Parser.createGdString(p);
                    var gdFile = Path.GetFileNameWithoutExtension(p);
                    var dir = Path.GetDirectoryName(p);
                    File.WriteAllText(dir + "/" + gdFile, gd);
                    scan = true;
                    return Parser.createFsString(p);
                }
                catch
                {
                    GD.PushWarning($"[Library] Failed parsing {k}, any format errors?");
                    return "";
                }
            },
            CodeFileName = "Bind",
            ConsoleHintAdd = "Generated {0} binding type for {1}",
            ConsoleHintRemove = "Removed Bind.fs for {0}"
        };
    
        UpdateWith(info);

        if (scan)
        {
            Callable.From(() =>
            {
                EditorInterface.Singleton.GetResourceFilesystem().Scan();
            }).CallDeferred();
        }
    }

    private void UpdateLibrary()
    {
        var info = new UpdateData<Resource>
        {
            ResDict = _cachedLib,
            Md5Dict = _cachedLibMd5,
            CodeGenerator = k => _cachedLib[k].Call("get_fs_content").AsString(),
            CodeFileName = "Library",
            ConsoleHintAdd = "Generated {0} library module for {1}",
            ConsoleHintRemove = "Removed Library.fs for {0}"
        };
    
        UpdateWith(info);
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
            
            UpdateYaml();
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
            _cachedUnlib = cfg.GetValue("cache", "unlib", new Array<string>())
                .AsGodotArray<string>();
            _cachedLib = cfg.GetValue("cache", "lib", new Collections.Dictionary<string, Resource>())
                .AsGodotDictionary<string, Resource>();
            _cachedLibMd5 = cfg.GetValue("cache", "lib_md5", new Collections.Dictionary<string, string>())
                .AsGodotDictionary<string, string>();
            _cachedYaml = cfg.GetValue("cache", "yaml", new Collections.Dictionary<string, string>())
                .AsGodotDictionary<string, string>();
            _cachedParents = cfg.GetValue("cache", "parents", new Collections.Dictionary<string, string>())
                .AsGodotDictionary<string, string>();
        }
        
        _shouldKillThread = false;
        _shouldLoadLib = true;
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
        cfg.SetValue("cache", "unlib", _cachedUnlib);
        cfg.SetValue("cache", "lib", _cachedLib);
        cfg.SetValue("cache", "lib_md5", _cachedLibMd5);
        cfg.SetValue("cache", "yaml", _cachedYaml);
        cfg.SetValue("cache", "parents", _cachedParents);
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