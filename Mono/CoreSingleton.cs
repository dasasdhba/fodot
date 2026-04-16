using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Microsoft.FSharp.Core;

namespace Mono;

public partial class CoreSingleton : Node
{
    public static CoreSingleton Instance { get; private set; }

    public CoreSingleton() : base()
    {
        Instance = this;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        GetTree().NodeAdded += node =>
        {
            Lib.Core.Register.registerToOwner(node);
            Lib.Core.Register.registerScript(node);
        };
    }
    
    private partial class FScriptData : Resource
    {
        public Dictionary<string, object> Data = [];
    }

    private const string DataMeta = "_FSData";
    private static void UpdateScriptData(Node node, string script, object obj)
    {
        if (!node.HasMeta(DataMeta)) node.SetMeta(DataMeta, new FScriptData());
        var data = node.GetMeta(DataMeta).As<FScriptData>();
        data.Data[script] = obj;
    }
    
    public static object GetScriptData(Node node, string script)
    {
        if (!node.HasMeta(DataMeta)) return null;
        var data = node.GetMeta(DataMeta).As<FScriptData>();
        return data.Data.GetValueOrDefault(script, null);
    }
}