
#I @"packages/Eto.Forms.1.3.0/lib/net40"
#r @"packages/Eto.Forms.1.3.0/lib/net40/Eto.dll"

#r @"packages/Eto.Platform.Gtk.1.3.0/lib/net40/Eto.Platform.Gtk.dll"
#r "../gtk-sharp-2.0/gtk-sharp.dll"
#r "../gtk-sharp-2.0/gdk-sharp.dll"
#r "../gtk-sharp-2.0/atk-sharp.dll"
#r "../gtk-sharp-2.0/glib-sharp.dll"
#r "../gtk-sharp-2.0/glib-sharp.dll"
let dummy = new Eto.Platform.GtkSharp.Forms.CursorHandler()

module GtkEventLoop =     
    open System

    // Workaround bug http://stackoverflow.com/questions/13885454/mono-on-osx-couldnt-find-gtksharpglue-2-dll
    //
    // There is no harm if this code is run more than once.
    if Environment.OSVersion.Platform = System.PlatformID.MacOSX then 
        let prevDynLoadPath = Environment.GetEnvironmentVariable("DYLD_FALLBACK_LIBRARY_PATH")
        let newDynLoadPath =  "/Library/Frameworks/Mono.framework/Versions/Current/lib" + (match prevDynLoadPath with null -> "" | s -> ":" + s) + ":/usr/lib"
        System.Environment.SetEnvironmentVariable("DYLD_FALLBACK_LIBRARY_PATH", newDynLoadPath)

    // Initialize Gtk. There is no harm if this code is run more than once.
    Gtk.Application.Init()

    fsi.EventLoop <- 
     { new Microsoft.FSharp.Compiler.Interactive.IEventLoop with
       member x.Run() = Gtk.Application.Run() |> ignore; false
       member x.Invoke f = 
         let res = ref None
         let evt = new System.Threading.AutoResetEvent(false)
         Gtk.Application.Invoke(new System.EventHandler(fun _ _ ->
           res := Some(f())
           evt.Set() |> ignore ))
         evt.WaitOne() |> ignore
         res.Value.Value 
       member x.ScheduleRestart() = () }

open Eto.Forms
open System

let app = 
    new Application (Eto.Generator.GetGenerator(Eto.Generators.GtkAssembly) )



