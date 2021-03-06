﻿// Learn more about F# at http://fsharp.org
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

// cards: list of cards picked by the player
// points: the total points of the cards
// name: player's name
// status: player's status (0 - no player, 1 - the player started to play, 2 - the player finished the game, 3 - the player lost)
type Player = {
    cards : List<string>
    points : int
    name : string
    status : int
}
// cards: cards number picked by the player
// points: the total points of the cards
// name: player's name
// status: player's status (0 - no player, 1 - the player started to play, 2 - the player finished the game, 3 - the player lost)
type Adversaire = {
    cards : int
    points : int
    name : string
    status : int
}
// Data sent to html client
type GameBoard = {
    player : Player
    adversaire : Adversaire
}

// Data saved at server
type Game = {cards : List<string>;
    players: List<Player>}

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
      | 0, x::xs -> xs
      | i, x::xs -> x::remove (i - 1) xs
      | i, [] -> failwith "index out of range"
  
  // gets the random index value in the range of the given list lentgh
  let randomIndex = fun (lst : List<String>) -> fun (rng : Random) ->
      rng.Next(List.length lst)
 
  // new pack template
  let newGame = File.ReadAllText("filez/gameTemplate.txt")

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
  
  // function to add an element to the head of the list
  let addToList = fun (lst:List<'T>) (element:'T) ->
      element::lst

  // get card weight
  let getCardValue str =
    Regex.Replace(str, @"[^\d]", String.Empty ) |> int

  // count the weight of cards in the list
  let countPoints cards = 
    cards |> List.map (fun value -> getCardValue value)
          |> List.sum

  let findIndexByName =
    fun (name:String) -> fun (list:List<Player>) ->
      List.findIndex (fun (elem:Player) -> elem.name = name) list
  
  // function treats the action of joining/starting the game
  let startAnew =
    fun (playerName : String) ->
        if not(File.Exists "filez/newGame.txt")
        then File.WriteAllText("filez/newGame.txt", newGame)
        let newGame = JsonConvert.DeserializeObject<Game>(File.ReadAllText("filez/newGame.txt"))
        let player:Player = { cards=[];
          points = 0;
          name = playerName;
          status = 1
        }

        let adversaire:Adversaire = 
            if (newGame.players.Length = 0) then { cards = 0;
                  points = 0;
                  name = "";
                  status = 0
                }
            else { cards = newGame.players.Item(0).cards.Length;
                  points = 0;
                  name = newGame.players.Item(0).name;
                  status = newGame.players.Item(0).status
                }
        let players = addToList newGame.players player
        let newGame:Game = {
            cards = newGame.cards;
            players = players
        }
        File.WriteAllText("filez/newGame.txt", JsonConvert.SerializeObject(newGame))
        let gameBoard : GameBoard = { player = player;
            adversaire = adversaire       
        }
        gameBoard |> JSON

  // function treats the action of picking a card
  let pPlay =
    fun (playerName : String) ->
        // the game state before picking a card 
        let game = JsonConvert.DeserializeObject<Game>(File.ReadAllText("filez/newGame.txt"))
        let playerIndex = findIndexByName playerName game.players
        let player =
            // player didn't start the game - the operation is not authoried
            if playerIndex >= 0 then (game.players.Item ( findIndexByName playerName game.players ))
            else { cards=[];
              points = 0;
              name = "";
              status = 0
            }
        
        match player.status with
        | 1 ->  // an active player
            let r = randomIndex (game.cards : List<String>) (new System.Random())
            let card = game.cards.Item r
            let playerCards = List.append player.cards [card]

            // player's updated state after picking the card
            let player:Player = {cards = playerCards;
                points = countPoints playerCards;
                name = player.name;
                status = 1}

            let adversaireIndex = 
                if playerIndex = 1 then 0
                else 1
            
            // the rival player data for the web browser
            let adversaire : Adversaire =
                if (adversaireIndex = 1) && (game.players.Length = 1)
                then { cards = 0;
                     points = 0;
                     name = "";
                     status = 0
                     }
                else { cards = game.players.Item(adversaireIndex).cards.Length;
                     points = 0;
                     name = game.players.Item(adversaireIndex).name;
                     status = game.players.Item(adversaireIndex).status
                     }

            let gameBoard : GameBoard = { player = player;
                adversaire = adversaire       
            }
            
            // write a new game state 
            let game = if (adversaireIndex = 1) && (game.players.Length = 1)
                       then { cards = (remove r game.cards); 
                            players = [player] }
                       else { cards = (remove r game.cards); 
                            players = [player;
                                      { cards = game.players.Item(adversaireIndex).cards;
                                              points = game.players.Item(adversaireIndex).points;
                                              name = game.players.Item(adversaireIndex).name;
                                              status = game.players.Item(adversaireIndex).status
                                       }]
                            }
            File.WriteAllText("filez/newGame.txt", JsonConvert.SerializeObject(game))

            gameBoard |> JSON       
        | _ -> // TODO write behavior for other statuses
            player |> JSON


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