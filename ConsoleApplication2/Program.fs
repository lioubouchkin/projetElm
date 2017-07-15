// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System
open System.Text.RegularExpressions
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
type Player = {
    Cards : List<string>
    points : int
    name : string
    status : int
}
// Pack of cards to play
type Pack = {Cards : List<string>}

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

  // remove an item at index <i> from the list <l>
  let rec remove i l =
      match i, l with
      | 0, x::xs ->
        xs
      | i, x::xs -> x::remove (i - 1) xs
      | i, [] -> failwith "index out of range"
  
  // gets the random index value in the range of the given list lentgh
  let randomIndex = fun (lst : List<String>) -> fun (rng : Random) ->
      rng.Next(List.length lst)
 
  // new pack template
  let newPack = File.ReadAllText("filez/pack.txt")

  // 'a -> WebPart
  let JSON v =
      let jsonSerializerSettings = new JsonSerializerSettings()
      jsonSerializerSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver()

      JsonConvert.SerializeObject(v, jsonSerializerSettings)
      |> OK
      >=> Writers.setMimeType "application/json; charset=utf-8"

  // 'obj -> string
  let getJsonString = fun obj ->
      JsonConvert.SerializeObject(obj)
  
  // get card weight
  let getCardValue str =
    Regex.Replace(str, @"[^\d]", String.Empty ) |> int

  // count the weight of cards in the list
  let countPoints cards = 
    cards |> List.map (fun value -> getCardValue value)
          |> List.sum

  let startAnew =
    fun (player : String) ->
        File.WriteAllText("filez/newGame.txt", newPack)
        let Player = { Cards=[];
          points = 0;
          name = player;
          status = 1
        }
        File.WriteAllText("filez/"+player+".txt", JsonConvert.SerializeObject(Player))
        Player |> JSON

  let pPlay =
    fun (player : String) ->
        let Player = JsonConvert.DeserializeObject<Player>(File.ReadAllText("filez/" + player + ".txt"))
        let Pack = JsonConvert.DeserializeObject<Pack>(File.ReadAllText("filez/newGame.txt"))
        let r = randomIndex (Pack.Cards : List<String>) (new System.Random())
        let card = Pack.Cards.Item r
        // pack left after one card is picked
        let Pack = { Cards = (remove r Pack.Cards) }
        let playerCards = List.append Player.Cards [card]
        let Player = {Cards = playerCards;
            points = countPoints playerCards;
            name = Player.name;
            status = 1}
        File.WriteAllText("filez/"+player+".txt", JsonConvert.SerializeObject(Player))
        File.WriteAllText("filez/newGame.txt", JsonConvert.SerializeObject(Pack))
        Player |> JSON

  let pStop =
    fun (game : String, player : String) -> 
        let myState = File.ReadAllText("filez/" + game + ".txt" )
        OK ("Player" + player + "stoped")

  let pWatch =
    fun (game : String, player : String) -> 
        let myState = File.ReadAllText("filez/" + game + ".txt" )
        OK myState

  let app =
      choose [
        GET >=> choose
            [ path "/" >=> Files.browseFileHome "index.html"
             // path "/game.js" >=> Files.browseFileHome "game.js"
              path "/hello" >=> OK "why hey hello"
              path "/goodbye" >=> OK "Good bye GET"
              pathScan "/newgame/%s" startAnew 
              pathScan "/pickCard/%s" pPlay 
              pathScan "/stop/%s/%s" pStop
              pathScan "/gameState/%s/%s" pWatch
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