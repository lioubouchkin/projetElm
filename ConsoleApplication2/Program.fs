// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System
open System.Threading
open Suave
open System.IO
open Suave.Filters
open Suave.Operators
open Suave.Successful


[<EntryPoint>]
let main argv =
  let app : WebPart =
    choose [
      GET >=> path "/" >=> Files.browseFileHome "index.html"
      GET >=> Files.browseHome
      RequestErrors.NOT_FOUND "Page not found." 
    ]
  let config =
    { defaultConfig with homeFolder = Some (Path.GetFullPath "../../../") }
  
  startWebServer config app
//  let cts = new CancellationTokenSource()
//  let conf = { defaultConfig with cancellationToken = cts.Token }
//  let listening, server = startWebServerAsync conf (Successful.OK "Hello World")
//  let listening, server = startWebServerAsync conf app
    
//  Async.Start(server, cts.Token)
//  printfn "Make requests now"
//  Console.ReadKey true |> ignore
    
//  cts.Cancel()

  0 // return an integer exit code

