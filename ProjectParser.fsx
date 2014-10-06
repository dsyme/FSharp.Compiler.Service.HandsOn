// --------------------------------------------------------------------------------------
// Sample code
// --------------------------------------------------------------------------------------

#if INTERACTIVE
#r "Microsoft.Build"
#r "Microsoft.Build.Engine"
#r "Microsoft.Build.Framework"
#r "Microsoft.Build.Tasks.v4.0"
#r "Microsoft.Build.Utilities.v4.0"
#endif


open System
open System.IO

type ProjectResolver(uri: string) = 
  
    let options = 

//      use engine = new Microsoft.Build.Evaluation.ProjectCollection()
      let project = 

            let xmlReader = System.Xml.XmlReader.Create (uri)
            let project = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection .LoadProject(xmlReader,"4.0")
            project.FullPath <- uri
            //project.SetProperty ("Configuration", "Debug");
            //project.SetProperty ("Platform", "");

            Environment.CurrentDirectory <- Path.GetDirectoryName (uri);
            project //let pi = project.CreateProjectInstance()

            //let b = pi.Build ([| "Build" |], [| |])
            //pi
 
      let loadtime = DateTime.Now
      let mkAbsolute dir v = if Path.IsPathRooted v then v else Path.Combine(dir,v) 
      let fileItems  = project.GetItems("Compile")
      let dir = Path.GetDirectoryName project.FullPath

      let getprop s = project.GetPropertyValue s
      let split (s: string) (cs: char[]) =
          if String.IsNullOrWhiteSpace s then [||]
          else s.Split(cs, StringSplitOptions.RemoveEmptyEntries)
      let getbool (s: string) =
          match (Boolean.TryParse s) with
          | (true, result) -> result
          | (false, _) -> false
      let optimize     = getprop "Optimize" |> getbool
      let aname        = getprop "AssemblyName" 
      let tailcalls    = getprop "Tailcalls" |> getbool
      let outpath      = getprop "OutputPath" 
      let docfile      = getprop "DocumentationFile" 
      let outtype      = getprop "OutputType" 
      let debugsymbols = getprop "DebugSymbols" |> getbool
      let defines = split (getprop "DefineConstants") [|';';',';' '|]
      let nowarn = split (getprop "NoWarn") [|';';',';' '|]
      let otherflags = getprop "OtherFlags" 
      let otherflags = 
          if String.IsNullOrWhiteSpace otherflags then [||]
          else split otherflags [|' '|]
      let isLib = (outtype="Library")
      let outfile = 
          if String.IsNullOrWhiteSpace outpath then None
          else 
             let v = Path.Combine(outpath,aname) + (if isLib then ".dll" else ".exe")
             Some (mkAbsolute dir v)

      let docfile = if String.IsNullOrWhiteSpace docfile then None else Some (mkAbsolute dir docfile)
      let b = project.Build( [| "ResolveReferences" |], null )
      let files = [| for f in fileItems do yield mkAbsolute dir f.EvaluatedInclude |]
      let fxVer = project.GetPropertyValue("TargetFrameworkVersion") 

      let references = 
         [| for i in project.GetItems("ReferencePath") do
               yield "-r:" + mkAbsolute dir i.EvaluatedInclude |]

      let options = 
       [|
        yield "--simpleresolution"
        yield "--noframework"
        match outfile with 
        | None -> ()
        | Some f -> yield "--out:" + f
        match docfile with 
        | None -> ()
        | Some f -> yield "--doc:" + f
        yield "--warn:3" 
        yield "--fullpaths" 
        yield "--flaterrors" 
        yield if isLib then "--target:library" else "--target:exe"
        for symbol in defines do 
            if not (String.IsNullOrWhiteSpace symbol) then 
                yield "--define:" + symbol
        for nw in nowarn do 
            if not (String.IsNullOrWhiteSpace nw) then 
                yield "--nowarn:" + nw
        yield if debugsymbols then  "--debug+" else  "--debug-"
        yield if optimize then "--optimize+" else "--optimize-"
        yield if tailcalls then "--tailcalls+" else "--tailcalls-"
        yield! otherflags
        yield! references
        yield! files
        |]
      options

    member p.Options = options


[<STAThread; EntryPoint>]
let main(args) = 

    let v = ProjectResolver(args.[0]) 

    for x in v.Options do printfn "%s" x

    //let b = Microsoft.Build.Execution.BuildManager()
    //let nm = typeof<Microsoft.Build.Execution.BuildManager>.InvokeMember("BuildNodeManager", System.Reflection.BindingFlags.NonPublic  ||| System.Reflection.BindingFlags.GetProperty ||| System.Reflection.BindingFlags.Instance, null, b, [| |])

//    let nm = b
    //b.Finalize()
    //printfn "done"

    System.GC.Collect()
    try 
          System.GC.WaitForPendingFinalizers()
    with _ -> () 


    0
