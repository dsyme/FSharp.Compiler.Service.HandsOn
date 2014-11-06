

(*
Compiler Services: Hands-On Tutorial
==================================

This tutorial demonstrates symbols, projects, interactive compilation/execution and the file system API

*)

//---------------------------------------------------------------------------
// Task 0. Create a checker


#I "packages/FSharp.Compiler.Service.0.0.73/lib/net45/"
#r "FSharp.Compiler.Service.dll"

open System
open System.Collections.Generic
open Microsoft.FSharp.Compiler.SourceCodeServices

let checker = __CREATE_AN_FSHARP_CHECKER__ 

//---------------------------------------------------------------------------
// Task 1. Crack an F# project file and get its options


let exampleProject = __SOURCE_DIRECTORY__ + @"/example/example.fsproj"
let exampleScript = __SOURCE_DIRECTORY__ + "/example/Script.fsx"

// If using Windows, or Mono/OSX/Linux with F# tag 3.1.1.27 or greater, you have the option
// of analyzing entire projects:
//let projectOptions = checker.GetProjectOptionsFromProjectFile(exampleProject) 


let projectOptions = 
    let scriptText = System.IO.File.ReadAllText(exampleScript)
    checker.GetProjectOptionsFromScript(exampleScript, scriptText)
    |> Async.RunSynchronously

__LOOK_AROUND_ON_PROJECT_OPTIONS_AND_CHECK_THEY_LOOK_OK__


//---------------------------------------------------------------------------
// Task 2. Parse and check an entire project

let wholeProjectResults = 
    __USE_THE_CHECKER_TO_PARSE_AND_CHECK_USING_THE_GIVEN_PROJECT_OPTIONS__
    __DONT_FORGET_TO_RUN_THE_CALL_SYNCHRONOUSLY__



//---------------------------------------------------------------------------
// Task 3. Analyze all uses of all the symbols used in the project to collect some project statistics

// Helper to count
module Seq = 
    let frequencyBy f s = 
        s 
        |> Seq.countBy f
        |> Seq.sortBy (snd >> (~-))


let allUsesOfAllSymbols = 
    wholeProjectResults.__GET_ALL_USES_OF_ALL_SYMBOLS_FROM_THE_INTERACTIVE_CHECKER_RESULTS_FOR_THE_PROJECT__
    __DONT_FORGET_TO_RUN_THE_CALL_SYNCHRONOUSLY__


// Task 3a. Frequency by display name

allUsesOfAllSymbols |> Seq.frequencyBy (fun su -> __GET_THE_DISPLAY_NAME_OF_THE_SYMBOL_ASSOCIATED_WITH_THE_SYMBOL_USE__) 

// Task 3b. Frequency by kind

allUsesOfAllSymbols |> Seq.frequencyBy (fun su -> __CHECK_IF_THE_SYMBOL_ASSOCIATED_WITH_THE_SYMBOL_USE_IS_FSharpMemberOrFunctionOrValue) 

// Task 3c. Frequency by kind

allUsesOfAllSymbols |> Seq.frequencyBy (fun su -> __USE_su.Symbol.GetType().Name__TO_CATEGORIZE_THE_SYMBOL__) 

// Task 3d. Frequency by kind

allUsesOfAllSymbols |> Seq.frequencyBy (fun su -> 
       match su.Symbol with 
       | :? FSharpMemberOrFunctionOrValue as mv -> 
           if mv.IsProperty || mv.IsPropertyGetterMethod || mv.IsPropertySetterMethod then 
              "prop"
           elif mv.IsMember then 
              "method"
           elif mv.IsModuleValueOrMember && mv.CurriedParameterGroups.Count = 0 then 
              "value"
           elif mv.IsModuleValueOrMember && mv.IsTypeFunction then  
              "tyfunc"
           elif mv.IsModuleValueOrMember then 
              "function"
           else
              "local"
       | :? FSharpUnionCase as uc -> 
              "unioncase"
       | :? FSharpField as uc -> 
              "field"
       | :? FSharpEntity as e -> 
              __ADD_CASES_TO_CHECK_IF_THE_ENTITY_IS_A_MODULE_OR_CLASS_OR_INTERFACE_OR_NAMESPACE__

       | _ -> "other") 

//---------------------------------------------------------------------------
// Task 4. Look for short variable names in module or member definitions (not locals)


allUsesOfAllSymbols 
    |> Seq.filter (fun su -> 
         __FIND_ALL_FSharpMemberOrFunctionOrValue_SYMBOLS_WITH_DISPLAY_NAME_LENGTH_LESS_THAN_THREE_AND_PASSING_IsModuleValueOrMember__)
    |> Seq.frequencyBy (fun su -> su.Symbol.DisplayName)


//---------------------------------------------------------------------------
// Task 5. Generate an efficient "power" function using the dynamic compiler


open Microsoft.FSharp.Compiler.Interactive.Shell
open System
open System.IO
open System.Text

// A helper class to wrap an F# Interactive Session
type Evaluator() = 
    // Intialize output and input streams
    let sbOut = new StringBuilder()
    let sbErr = new StringBuilder()
    let inStream = new StringReader("")
    let outStream = new StringWriter(sbOut)
    let errStream = new StringWriter(sbErr)

    // Build command line arguments & start FSI session
    let argv = [| "C:\\fsi.exe" |]
    let allArgs = Array.append argv [|"--noninteractive"; "--optimize+"|]

    let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
    let fsiSession = FsiEvaluationSession.Create(fsiConfig, allArgs, inStream, outStream, errStream)  

    member __.EvalExpression text =
      match fsiSession.EvalExpression(text) with
      | Some value -> value.ReflectionValue
      | None -> failwith "Got no result!"

let evaluator = Evaluator()
let pow0obj =
    evaluator.EvalExpression
        """
let rec pow n x = if n = 0 then 1.0 else x * pow (n-1) x 
pow        
        """

// EvalExpression returns an 'obj'. Convert the object to the expected type
let pow0 = pow0obj |> unbox<int -> double -> double>

__TEST_POW0___

let pow1obj =
    evaluator.EvalExpression
        """
let rec pow n x = 
    let mutable v = 1.0 
__DO_AN_IMPLEMENTATION_OF_POW_USING_A_MUTABLE__

pow
"""

let pow1 = pow1obj |> unbox<int -> double -> double>

__TEST_POW1___


let pow2 n =
    evaluator.EvalExpression
       ("""
 __DO_AN_IMPLEMENTATION_OF_POW_USING_GENERATED_CODE__
 let rec pow (x:double) = 
      __MAKE_THIS_BE x * .... * x 
 pow
 
 """)

     |> unbox<double -> double>


__TEST_POW2___

#time "on"


// Generate a specialized 'pow2' for size 10 and 100
let pow2_10 = pow2 10 

__GENERATE_SPECIALIZED_POW2_FOR_SIZE_100_AND_CALL_IT_'pow2_100'__

// A benchmarking function that uses 'f' many times
let powt f = 
    let mutable res = 0.0
    for i in 0 .. 10000000 do 
        res <- f 10.0 
    res

__BENCHMARK_POW0_POW1_POW2_USING_POWT___

//powt (pow1 100)
//powt pow2_100


//---------------------------------------------------------------------------
// Task 6. Create a file system which draws its input from a form


#r "FSharp.Compiler.Service.dll"
open System
open System.IO
open System.Collections.Generic
open System.Text
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library

type MyFileSystem() = 
    /// The default file system
    let dflt = Shim.FileSystem
    
    /// The store of files in the virtualized file system
    let files = Dictionary<string,(DateTime * string)>()

    /// Sets the file text in the file system
    member __.SetFile(file, text:string) = 
         files.[file] <- (DateTime.Now, text)

    interface IFileSystem with
        // Implement the service to open files for reading and writing
        member __.FileStreamReadShim(fileName) = 
            if files.ContainsKey(fileName) then
                let (fileWriteTime, fileText) = files.[fileName]
                new MemoryStream(Encoding.UTF8.GetBytes(fileText)) :> Stream
            else 
                dflt.FileStreamReadShim(fileName)

        member __.ReadAllBytesShim(fileName) = 
            __IMPLEMENT_THIS_PART_OF_THE_FILE_SYSTEM_API_USING_'Encoding.UTF8.GetBytes(fileText)'__
            __CHECK_'files.ContainsKey'__FIRST__LIKE_THE_OTHER_CASES__
            __USE_'dftl.ReadAllBytesShim(fileName)'_LIKE_THE_OTHER_CASES__

        member __.GetLastWriteTimeShim(fileName) = 
            if files.ContainsKey(fileName) then
                let (fileWriteTime, fileText) = files.[fileName]
                fileWriteTime
            else 
                dflt.GetLastWriteTimeShim(fileName)

        member __.SafeExists(fileName) = 
            files.ContainsKey(fileName) || dflt.SafeExists(fileName)

        member __.FileStreamCreateShim(fileName) = dflt.FileStreamCreateShim(fileName)
        member __.FileStreamWriteExistingShim(fileName) = dflt.FileStreamWriteExistingShim(fileName)
        member __.GetTempPathShim() = dflt.GetTempPathShim()
        member __.GetFullPathShim(fileName) = dflt.GetFullPathShim(fileName)
        member __.IsInvalidPathShim(fileName) = dflt.IsInvalidPathShim(fileName)
        member __.IsPathRootedShim(fileName) = dflt.IsPathRootedShim(fileName)
        member __.FileDelete(fileName) = dflt.FileDelete(fileName)
        member __.AssemblyLoadFrom(fileName) = dflt.AssemblyLoadFrom fileName
        member __.AssemblyLoad(assemblyName) = dflt.AssemblyLoad assemblyName 

let myFileSystem = MyFileSystem()
Shim.FileSystem <- myFileSystem

let fileName1 = @"c:\mycode\test1.fs" // note, the path doesn't exist
let fileName2 = @"c:\mycode\test2.fs" // note, the path doesn't exist

myFileSystem.SetFile(fileName1, """module N
let x = 1""")
myFileSystem.SetFile(fileName2, """module M
let x = N.x + 1""")
FileSystem.ReadAllBytesShim fileName1
FileSystem.ReadAllBytesShim fileName2

//---------------------------------------------------------------------------
// Task 6b. Check with respect to the file system

// Create a new set of project options with a different set of file names
let projectOptions2 = 
    { projectOptions with 
        OtherOptions = [|   yield! projectOptions.OtherOptions |> Array.filter(fun s -> not (s.EndsWith ".fs"))
                            yield fileName1
                            yield fileName2 |] }

let wholeProjectResults2 = 
    checker.ParseAndCheckProject(projectOptions2) 
    |> Async.RunSynchronously

// Check if the project results contains errors
wholeProjectResults2.Errors

//---------------------------------------------------------------------------
// Task 7. Create an IDE


#load "load-eto-winforms.fsx"  // <------ USE THIS ON WINDOWS
//#load "load-eto-gtk.fsx"         // <------ USE THIS ON MAC

open System
open Eto.Forms
open Eto.Drawing


let createEditor(fileName, fileText) =

    let form = new Form(Title = fileName, ClientSize = new Size(400, 350))

    let textArea = new TextArea( (* Dock=DockStyle.Fill, Multiline=true *))
    //tb1.TextChanged.Add(fun _ -> (* printfn "setting..."; myFileSystem.SetFile(fileName, tb1.Text) *)()  )
    textArea.Text <- fileText
    form.Content <- new Scrollable(Content = textArea)

    form.Show()
    textArea

let textArea1 = createEditor(fileName1, "module FileOne\n\nlet x = 1")
let textArea2 = createEditor(fileName2, "module FileTwo\n\nlet x = 1") 


async { for i in 0 .. 100 do 
          try 
            do! Async.Sleep 1000
            do myFileSystem.SetFile(fileName1, textArea1.Text) 
            do myFileSystem.SetFile(fileName2, textArea2.Text) 

            printfn "checking..."

            let! wholeProjectResults = __PARSE_AND_CHECK_THE_WHOLE_PROJECT_HERE__ // checker.ParseAndCheckProject(projectOptions2) 
            printfn "checked..."

            __ADD_AN_ANALYSIS_WHICH_REPORTS_THE_USE_OF_MUTABLE_VALUES_IN_THE_PROJECT_AND_PRINTS_THE_RESULTS__

            if wholeProjectResults.Errors.Length = 0 then 
               printfn "all ok!" 

            for e in wholeProjectResults.Errors do 
               printfn "error/warning: %s(%d%d): %s" e.FileName e.StartLineAlternate e.StartColumn e.Message 

          with e -> 
              printfn "whoiops...: %A" e.Message }
   |> Async.StartImmediate


// Use this to stop, or else just reset the session :)
// Async.CancelDefaultToken()

