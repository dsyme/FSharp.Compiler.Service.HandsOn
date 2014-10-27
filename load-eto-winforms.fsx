
#I @"packages/Eto.Forms.1.3.0/lib/net40"
#r @"packages/Eto.Forms.1.3.0/lib/net40/Eto.dll"

#I @"packages/Eto.Platform.Windows.1.3.0/lib/net40"
#r @"packages/Eto.Platform.Windows.1.3.0/lib/net40/Eto.Platform.Windows.dll"

open Eto.Forms

let app = 
    new Application (Eto.Generator.GetGenerator(Eto.Generators.WinAssembly) )
