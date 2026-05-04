open Fodot.Generator.Parser

[<EntryPoint>]
let main args =
    if args.Length < 2 then
        printfn "Usage: Fodot.Generator <inputDir> <outputFile>"
        1
    else
        let inputDir = args[0]
        let outputFile = args[1]
        
        createFsBinding inputDir outputFile
        0