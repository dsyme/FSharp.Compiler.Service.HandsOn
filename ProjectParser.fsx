// --------------------------------------------------------------------------------------
// (c) Robin Neatherway
// --------------------------------------------------------------------------------------
#if INTERACTIVE
#r "Microsoft.Build"
#r "Microsoft.Build.Engine"
#r "Microsoft.Build.Framework"
#r "Microsoft.Build.Tasks.v4.0"
#r "Microsoft.Build.Utilities.v4.0"
#endif

// Disable warnings for obsolete MSBuild.
// Mono doesn't support the latest API.
#nowarn "0044"

open System
open System.IO
open Microsoft.Build.BuildEngine
open Microsoft.Build.Framework
open Microsoft.Build.Tasks
open Microsoft.Build.Utilities

type ProjectResolver(uri: string) = 
    let project = new Project()
    do project.Load(uri)
    let loadtime = DateTime.Now
    let mkAbsolute dir v = if Path.IsPathRooted v then v else Path.Combine(dir,v) 

    member __.FileName = project.FullFileName

    member __.LoadTime = loadtime

    member __.Directory =  
      Path.GetDirectoryName project.FullFileName

    member p.Files =
      let fs  = project.GetEvaluatedItemsByName("Compile")
      let dir = p.Directory
      [| for f in fs do yield mkAbsolute p.Directory f.FinalItemSpec |]

    member p.FrameworkVersion = project.GetEvaluatedProperty("TargetFrameworkVersion") 

    // We really want the output of ResolveAssemblyReferences. However, this
    // needs as input ChildProjectReferences, which is populated by
    // ResolveProjectReferences. For some reason ResolveAssemblyReferences
    // does not depend on ResolveProjectReferences, so if we don't run it first
    // then we won't get the dll files for imported projects in this list.
    // We can therefore build ResolveReferences, which depends on both of them,
    // or [|"ResolveProjectReferences";"ResolveAssemblyReferences"|]. These seem
    // to be equivalent. See Microsoft.Common.targets if you want more info.
    member p.References =
      let b = project.Build([|"ResolveReferences"|])
      [| for i in project.GetEvaluatedItemsByName("ReferencePath") do
           yield "-r:" + mkAbsolute p.Directory i.FinalItemSpec |]

    member p.Options =
      let getprop s = project.GetEvaluatedProperty s
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
             Some (mkAbsolute p.Directory v)

      let docfile = if String.IsNullOrWhiteSpace docfile then None else Some (mkAbsolute p.Directory docfile)
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
        yield! p.References
        yield! p.Files
       |]
