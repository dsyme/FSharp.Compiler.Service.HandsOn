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

      let xmlReader = System.Xml.XmlReader.Create (uri)
      let project = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.LoadProject(xmlReader,"4.0")
      project.FullPath <- uri
      //project.SetProperty ("Configuration", "Debug");
      //project.SetProperty ("Platform", "");

      let project = project.CreateProjectInstance()
      Environment.CurrentDirectory <- Path.GetDirectoryName (uri);

      //for t in project.Targets do 
      //       printfn "target %A --> %A" t.Key t.Value
 
      let b = project.Build( [| "ResolveReferences" |], null )
     // let b = project.Build( [| "ResolveAssemblyReferences" |], null )
      let loadtime = DateTime.Now
      let mkAbsolute dir v = if Path.IsPathRooted v then v else Path.Combine(dir,v) 
      let fileItems  = project.GetItems("Compile")
      let resourceItems  = project.GetItems("Resource")
      let dir = Path.GetDirectoryName project.FullPath

      let getprop s = 
          let v = project.GetPropertyValue s 
          if String.IsNullOrWhiteSpace v then None else Some v

      let split (s: string option) (cs: char[]) =
          match s with 
          | None -> [| |]
          | Some s ->
          if String.IsNullOrWhiteSpace s then [||]
          else s.Split(cs, StringSplitOptions.RemoveEmptyEntries)

      let getbool (s: string option) =
          match s with 
          | None -> false
          | Some s ->
          match (Boolean.TryParse s) with
          | (true, result) -> result
          | (false, _) -> false

      let optimize        = getprop "Optimize" |> getbool
      let assemblyNameOpt = getprop "AssemblyName" 
      let tailcalls       = getprop "Tailcalls" |> getbool
      let outputPathOpt   = getprop "OutputPath" 
      let docFileOpt      = getprop "DocumentationFile" 
      let outputTypeOpt   = getprop "OutputType" 
      let debugTypeOpt    = getprop "DebugType" 
      let baseAddressOpt  = getprop "BaseAddress" 
      let sigFileOpt      = getprop "GenerateSignatureFile" 
      let keyFileOpt      = getprop "KeyFile" 
      let pdbFileOpt      = getprop "PdbFile" 
      let platformOpt     = getprop "Platform" 
      let targetTypeOpt     = getprop "TargetType" 
      let versionFileOpt     = getprop "VersionFile" 
      let targetProfileOpt     = getprop "TargetProfile" 
      let warnLevelOpt     = getprop "Warn" 
      let subsystemVersionOpt     = getprop "SubsystemVersion" 
      let win32ResOpt     = getprop "Win32ResourceFile" 
      let heOpt     = getprop "HighEntropyVA" |> getbool
      let win32ManifestOpt     = getprop "Win32ManifestFile" 
      let debugSymbols    = getprop "DebugSymbols" |> getbool
      let prefer32bit    = getprop "Prefer32Bit" |> getbool
      let warnAsError    = getprop "TreatWarningsAsErrors" |> getbool
      let defines = split (getprop "DefineConstants") [|';';',';' '|]
      let nowarn = split (getprop "NoWarn") [|';';',';' '|]
      let warningsAsError = split (getprop "WarningsAsErrors") [|';';',';' '|]
      let libPaths = split (getprop "ReferencePath") [|';';','|]
      let otherFlags = split (getprop "OtherFlags") [|' '|]
      let isLib = (outputTypeOpt = Some "Library")
      let outputFileOpt = 
          match outputPathOpt, assemblyNameOpt with
          | Some outputPath, Some assemblyName -> 
             let v = Path.Combine(outputPath,assemblyName) + (if isLib then ".dll" else ".exe")
             Some (mkAbsolute dir v)
          | _ -> None

      //for t in project.Items do 
      //       printfn "item %A" (t.EvaluatedInclude)
 
      let docFileOpt = match docFileOpt with None -> None | Some docFile -> Some (mkAbsolute dir docFile)
      let files = [| for f in fileItems do yield mkAbsolute dir f.EvaluatedInclude |]
      let resources = [| for f in resourceItems do yield "--resource:" + mkAbsolute dir f.EvaluatedInclude |]
      let fxVer = project.GetPropertyValue("TargetFrameworkVersion") 

      let references = 
         [| for i in project.GetItems("ReferencePath") do
               yield "-r:" + mkAbsolute dir i.EvaluatedInclude |]
      let libPaths = 
         [| for i in libPaths do
               yield "--lib:" + mkAbsolute dir i |]

      let options = 
       [|
        yield "--simpleresolution"
        yield "--noframework"
        match outputFileOpt with 
        | None -> ()
        | Some outputFile -> yield "--out:" + outputFile
        match docFileOpt with 
        | None -> ()
        | Some docFile -> yield "--doc:" + docFile
        match baseAddressOpt with 
        | None -> ()
        | Some baseAddress -> yield "--baseaddress:" + baseAddress
        match keyFileOpt with 
        | None -> ()
        | Some keyFile -> yield "--keyfile:" + keyFile
        match sigFileOpt with 
        | None -> ()
        | Some sigFile -> yield "--sig:" + sigFile
        match pdbFileOpt with 
        | None -> ()
        | Some pdbFile -> yield "--pdb:" + pdbFile
        match versionFileOpt with 
        | None -> ()
        | Some versionFile -> yield "--versionfile:" + versionFile
        match warnLevelOpt with 
        | None -> ()
        | Some warnLevel -> yield "--warn:" + warnLevel
        match subsystemVersionOpt with 
        | None -> ()
        | Some s -> yield "--subsystemversion:" + s
        if heOpt then yield "--highentropyva+"
        match win32ResOpt with 
        | None -> ()
        | Some win32Res -> yield "--win32res:" + win32Res
        match win32ManifestOpt with 
        | None -> ()
        | Some win32Manifest -> yield "--win32manifest:" + win32Manifest
        match targetProfileOpt with 
        | None -> ()
        | Some targetProfile -> yield "--targetprofile:" + targetProfile
        yield "--fullpaths" 
        yield "--flaterrors" 
        if warnAsError then yield "--warnaserror" 
        yield if isLib then "--target:library" else "--target:exe"
        for symbol in defines do 
            if not (String.IsNullOrWhiteSpace symbol) then 
                yield "--define:" + symbol
        for nw in nowarn do 
            if not (String.IsNullOrWhiteSpace nw) then 
                yield "--nowarn:" + nw
        for nw in warningsAsError do 
            if not (String.IsNullOrWhiteSpace nw) then 
                yield "--warnaserror:" + nw
        yield if debugSymbols then "debug+" else  "--debug-"
        yield if optimize then "--optimize+" else "--optimize-"
        yield if tailcalls then "--tailcalls+" else "--tailcalls-"
        match debugTypeOpt with
          | None -> ()
          | Some debugType ->
            match debugType.ToUpperInvariant() with
            | "NONE"     -> ()
            | "PDBONLY"  -> yield "--debug:pdbonly"
            | "FULL"     -> yield "--debug:full"
            | _         -> ()

        match platformOpt |> Option.map (fun o -> o.ToUpperInvariant()),prefer32bit, targetTypeOpt |> Option.map (fun o -> o.ToUpperInvariant()) with
            | Some "ANYCPU", true, Some "EXE"
            | Some "ANYCPU", true, Some "WINEXE" -> yield "anycpu32bitpreferred"
            | Some "ANYCPU",  _, _  -> yield "anycpu"
            | Some "X86"   ,  _, _  -> yield "x86"
            | Some "X64"   ,  _, _  -> yield "x64"
            | Some "ITANIUM", _, _  -> yield "Itanium"
            | _         -> ()
        match targetTypeOpt |> Option.map (fun o -> o.ToUpperInvariant()) with
            | Some "LIBRARY" -> yield "--target:library"
            | Some "EXE" -> yield "--target:exe"
            | Some "WINEXE" -> yield "--target:winexe" 
            | Some "MODULE" -> yield "--target:module"
            | _ -> ()

        yield! otherFlags
        yield! resources
        yield! libPaths
        yield! references
        yield! files
        |]
      options

    member p.Options = options

[<STAThread; EntryPoint>]
let main(args) = 

    let v = ProjectResolver(args.[0]) 

    for x in v.Options do printfn "%s" x

    // Finalization of the default BuildManager on Mono causes a thread to start when 'BuildNodeManager' is accessed
    // n the finalizer.  The thread start doesn't work when exiting, and even worse he thread is not marked as a 
    // background computation thread, so a console application doesn't exit correctly.
    System.GC.SuppressFinalize(Microsoft.Build.Execution.BuildManager.DefaultBuildManager)

 //   printfn "done"

(*
    System.GC.Collect()
    try 
          System.GC.WaitForPendingFinalizers()
    with _ -> () 
*)


    0
