namespace Fodot.Core

open System.Threading
open Fodot.Core.Engine
open Godot

type AsyncNode =
    {
        Node : Node
        Physics : bool
        Ct : CancellationToken
    }
    
    static member New (node : Node) physics ct =
        {
            Node = node
            Physics = physics
            Ct = ct
        }
    static member NewIdle (node : Node) ct =
        AsyncNode.New node false ct
    static member NewPhysics (node : Node) ct =
        AsyncNode.New node true ct
        
module AsyncNode =    
    
    let until (predict : ProcessFunc<bool>) (anode : AsyncNode) =
        let event = Event<unit>()
        let proc =
            ProcessConfig.New anode.Physics (Delta (fun delta ->
                if anode.Ct.IsCancellationRequested |> not then
                    if predict.Invoke delta then
                        event.Trigger()
            ))
        let id = anode.Node |> addProcessBy proc
        
        task {
            try
                do! GDTask.awaitEvent event.Publish anode.Ct
            finally
                anode.Node |> removeProcess id |> ignore
        }
        
    let toProcessThread (anode : AsyncNode) =
        anode |> until (Unit (fun _ -> true))

    let private delayWithSome (proc : ProcessUnit option) (time : float) (anode : AsyncNode) =
        let predictor =
            let mutable timer = 0.0
            Delta (fun delta ->
                if proc.IsSome then proc.Value.Invoke delta
                timer <- timer + delta
                timer >= time
            )
        anode |> until predictor
    
    let delayWith (proc : ProcessUnit) (time : float) (anode : AsyncNode) =
        anode |> delayWithSome (Some proc) time
    
    let delay (time : float) (anode : AsyncNode) =
        anode |> delayWithSome None time
    
    let delayFrameWithSome (proc : ProcessUnit option) (frame : uint) (anode : AsyncNode) =
        let predictor =
            let mutable counter = 0u
            Delta (fun delta ->
                if proc.IsSome then proc.Value.Invoke delta
                counter <- counter + 1u
                counter >= frame
            )
        anode |> until predictor
    
    let delayFrameWith (proc : ProcessUnit) (frame : uint) (anode : AsyncNode) =
        anode |> delayFrameWithSome (Some proc) frame
    
    let delayFrame (frame : uint) (anode : AsyncNode) =
        anode |> delayFrameWithSome None frame
    
type AsyncNode with
    member this.Until (predict : ProcessFunc<bool>) =
        this |> AsyncNode.until predict
    member this.ToProcessThread () =
        this |> AsyncNode.toProcessThread
    member this.Delay (time : float) =
        this |> AsyncNode.delay time
    member this.DelayWith (proc : ProcessUnit) (time : float) =
        this |> AsyncNode.delayWith proc time
    member this.DelayFrame (frame : uint) =
        this |> AsyncNode.delayFrame frame
    member this.DelayFrameWith (proc : ProcessUnit) (frame : uint) =
        this |> AsyncNode.delayFrameWith proc frame