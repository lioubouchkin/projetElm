// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open System.Threading
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open System.IO
open Suave.Json
open System.Runtime.Serialization
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

// 
type Game = {
    Cards : List<string>
    points : int
    player : string
    status : int
}

[<EntryPoint>]
let main argv = 
  let cts = new CancellationTokenSource()
  let conf = { defaultConfig with cancellationToken = cts.Token ; homeFolder = Some (Path.GetFullPath "filez")}  

  let sleep milliseconds message: WebPart =
      fun (x : HttpContext) ->
        async {
          do! Async.Sleep milliseconds
          return! OK message x
        }

  //File.WriteAllText("temp.txt","toto D:")
  let bluePrint = File.ReadAllText("filez/temp.txt")

  let getItemToList =
    fun ( data : list<String>, i : int ) ->
        data.Item(i).Split(';') |> Array.toList

  let mapData = 
    fun ( data : list<String> ) ->
        let namesPlayers = getItemToList(data,4)
        let statusPlayers = getItemToList(data,5)
        let cardsMain = [getItemToList(data,0),getItemToList(data,1),getItemToList(data,2),getItemToList(data,3)]
        let cardsPlayersTemp = [| for i in 1 .. namesPlayers.Length -> []  |]
        for i = 0 to 0 + namesPlayers.Length - 1 do
         cardsPlayersTemp.SetValue( getItemToList(data,i+6),i)
        let cardsPlayers = cardsPlayersTemp |> Array.toList
        [namesPlayers,statusPlayers,cardsMain,cardsPlayers]

  let getLines = 
    fun ( path : String ) ->
      let lines = 
        File.ReadAllLines(path)
        |> Array.map (fun line ->
        let newLine = line
        newLine )
      lines |> Array.toList  


  let getData =
    fun ( str : String ) ->
      let path = "filez/" + str + ".txt"
      let data path = 
        if File.Exists(path) then mapData(getLines(path))
        else []
      data(path)

      // 'a -> WebPart
  let JSON v =
      let jsonSerializerSettings = new JsonSerializerSettings()
      jsonSerializerSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver()

      JsonConvert.SerializeObject(v, jsonSerializerSettings)
      |> OK
      >=> Writers.setMimeType "application/json; charset=utf-8"

  let startAnew =
        fun (str : String) ->
        let Game = { Cards=[];
          points = 0;
          player = str;
          status = 1
        }
        Game |> JSON
 //       File.WriteAllText("filez/" + str + ".txt", bluePrint)
        //let myNplayerewBoard = File.ReadAllText(str + ".txt")
        //myNewBostatusard
 //       OK (str + " created")

  let pPlay =
        fun (game : String, player : String) -> 
        let myState = File.ReadAllText("filez/" + game + ".txt" )
        myState |> JSON

  let pStop =
        fun (game : String, player : String) -> 
        let myState = File.ReadAllText("filez/" + game + ".txt" )
        OK ("Player" + player + "stoped")

  let pWatch =
        fun (game : String, player : String) -> 
        let myState = File.ReadAllText("filez/" + game + ".txt" )
        OK myState

  let pJoin = 
        fun ( str : String ) ->
        let data = getData str
        let item = 
          if data.Length > 0 then data.Item(0).ToValueTuple().Item4.Item(0).Item(0)
          else "no Item"
        OK item

  let app =
      choose [
        GET >=> choose
            [ path "/" >=> Files.browseFileHome "index.html"
             // path "/game.js" >=> Files.browseFileHome "game.js"
              path "/hello" >=> OK "why hey hello"
              path "/goodbye" >=> OK "Good bye GET"
              pathScan "/newgame/%s" startAnew 
              pathScan "/pick/%s/%s" pPlay 
              pathScan "/stop/%s/%s" pStop
              pathScan "/gameState/%s/%s" pWatch 
              pathScan "/join/%s" pJoin
            ]
        POST >=> choose
            [ path "/hello" >=> OK "Hello POST"
              path "/" >=> Files.browseFileHome "index.html"
              path "/goodbye" >=> OK "Good bye POST" ]
        GET >=> Files.browseHome
        RequestErrors.NOT_FOUND "Page not found."  
        ]


  let listening, server = startWebServerAsync conf (app)
    
  Async.Start(server, cts.Token)
  printfn "Make requests now"
  Console.ReadKey true |> ignore
    
  cts.Cancel()

  0 // return an integer exit code