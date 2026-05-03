namespace Fodot.Module

open Fodot.Core
open Godot

module Tween =
    
    let getLoopsLeft (tween : Tween) =
        tween.GetLoopsLeft()
    
    let getTotalElapsedTime (tween : Tween) =
        tween.GetTotalElapsedTime()
    
    let isRunning (tween : Tween) =
        tween.IsRunning()
    
    let isValid (tween : Tween) =
        tween.IsValid()
    
    let kill (tween : Tween) =
        tween.Kill()
    
    let parallax (tween : Tween) =
        tween.Parallel ()
    
    let pause (tween : Tween) =
        tween.Pause ()
    
    let play (tween : Tween) =
        tween.Play ()
    
    let setEase ease (tween : Tween) =
        tween.SetEase ease
    
    let setIgnoreTimeScale (tween : Tween) =
        tween.SetIgnoreTimeScale ()
    
    let setParallax (tween : Tween) =
        tween.SetParallel ()
    
    let setProcessMode (mode : Tween.TweenProcessMode) (tween : Tween) =
        tween.SetProcessMode mode
    
    let setSpeedScale speed (tween : Tween) =
        tween.SetSpeedScale speed
    
    let setTrans trans (tween : Tween) =
        tween.SetTrans trans
    
    let stop (tween : Tween) =
        tween.Stop ()
        
    let callback (action : unit -> unit) (tween : Tween) =
        tween.TweenCallback (Callable.From(action))
        
    let interval (time : float) (tween : Tween) =
        tween.TweenInterval time
        
    let method<'a> (method : 'a -> unit) (origin :'a) (target : 'a) (duration : float) (tween : Tween) =
        tween.TweenMethod (method, origin, target, duration)
        
    let property<'a> (obj : GodotObject) (prop : string) (final : 'a) (duration : float) (tween : Tween) =
        tween.TweenProperty (obj, prop, final |> Variant.from, duration)
    
    let subtween sub (tween : Tween) =
        tween.TweenSubtween sub
    
    let createWith (node : Node) =
        node.CreateTween ()
        
    let createPhysicsWith (node : Node) =
        let tween = createWith node
        tween.SetProcessMode Tween.TweenProcessMode.Physics
        
module CallbackTweener =
    
    let setDelay (delay : float) (tween : CallbackTweener) =
        tween.SetDelay delay
        
module MethodTweener =
    
    let setDelay (delay : float) (tween : MethodTweener) =
        tween.SetDelay delay
        
    let setTrans trans (tween : MethodTweener) =
        tween.SetTrans trans
        
    let setEase ease (tween : MethodTweener) =
        tween.SetEase ease
        
module SubtweenTweener =
    
    let setDelay (delay : float) (tween : SubtweenTweener) =
        tween.SetDelay delay
        
module PropertyTweener =
    
    let asRelative (tween : PropertyTweener) =
        tween.AsRelative ()
        
    let from<'a> (value : 'a) (tween : PropertyTweener) =
        tween.From (value |> Variant.from)
        
    let fromCurrent (tween : PropertyTweener) =
        tween.FromCurrent ()
        
    let setDelay (delay : float) (tween : PropertyTweener) =
        tween.SetDelay delay
        
    let setInterpolator (method : float -> float) (tween : PropertyTweener) =
        tween.SetCustomInterpolator (Callable.From(method))
        
    let setTrans trans (tween : PropertyTweener) =
        tween.SetTrans trans
        
    let setEase ease (tween : PropertyTweener) =
        tween.SetEase ease