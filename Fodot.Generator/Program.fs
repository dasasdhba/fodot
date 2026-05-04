open Fodot.Generator.Parser

[<EntryPoint>]
let main args =
    if args.Length < 1 then
        printfn "Usage: Fodot.Generator <inputDir>"
        1
    else
        let inputDir = args[0]
        
        createFsBinding inputDir
        0