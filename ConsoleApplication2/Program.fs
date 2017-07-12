// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System
open System.Threading
open Suave
open System.IO
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors


[<EntryPoint>]
let main argv =
  // request treatement
  let browse =
    request (fun r ->
      match r.queryParam "action" with
      | Choice1Of2 action -> OK (sprintf "Action: %s" action)
      | Choice2Of2 msg -> Files.browseFileHome "index.html")
  let app : WebPart =
    choose [
//      GET >=> path "/" >=> Files.browseFileHome "index.html"
      GET >=> path "/" >=> browse
      GET >=> Files.browseHome
      RequestErrors.NOT_FOUND "Page not found." 
    ]
  let config =
    { defaultConfig with homeFolder = Some (Path.GetFullPath "../../../") }
  
  startWebServer config app

  0 // return an integer exit code

