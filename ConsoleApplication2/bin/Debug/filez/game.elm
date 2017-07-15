module Game exposing (..)

import Html exposing (program, ul, li, text, div, h2, input, button, Html, Attribute)
import Html.Events exposing (..)
import String exposing (length)
import Html.Attributes exposing (type_, placeholder, style)
import List exposing (map)
import Http exposing (get)
import Json.Decode as Decode


main =
  Html.program
    { init = init
    , view = view
    , update = update
    , subscriptions = subscriptions
    }


-- MODEL

type alias Player = 
  {cards: List String
  ,points: Int
  ,name:String
  ,status:Int
  }
player : Player
player = 
  {cards=[]
  ,points=0
  ,name=""
  ,status=0
  }

type alias Model =
  {pl : Player}
model : Model
model = 
  {pl=player}
  
init : (Model, Cmd Msg)
init =
  (Model player,
  Cmd.none)


-- UPDATE

type Msg
  = Display
  | Name String
  | JoinGame
  | JoinGameResponse (Result Http.Error String)
  | StopGame
  | StopGameResponse (Result Http.Error String)
  | PickCard
  | PickCardResponse (Result Http.Error (List String))
  | GameState
  | GameStateResponse (Result Http.Error String)


update : Msg -> Model -> (Model, Cmd Msg)
update msg model =
  case msg of
    Display ->
      (model, Cmd.none)
    PickCard ->
      (model, pickCard model.pl.name)
    PickCardResponse (Ok cards) ->
      (Model (Player cards 0 model.pl.name 1), Cmd.none)
    PickCardResponse (Err _) ->
      (Model (Player [] 0 "Error" 1), Cmd.none)    
    GameState ->
      (model, gameState)   
    GameStateResponse (Ok _) ->
      (model, Cmd.none)   
    GameStateResponse (Err _) ->
      (model, Cmd.none)
    JoinGame ->
      (model, joinGame model.pl.name)
    JoinGameResponse (Ok name) ->
      (Model (Player [] 0 name 1), Cmd.none) 
    JoinGameResponse (Err _) ->
      (Model (Player [] 0 "Error" 1), Cmd.none)
    StopGame ->
      (model, stopGame)  
    StopGameResponse (Ok _) ->
      (model, Cmd.none)
    StopGameResponse (Err _) ->
      (model, Cmd.none)
    Name name ->
      (Model (Player model.pl.cards model.pl.points name model.pl.status),
      Cmd.none)
      

-- SUBSCRIPTIONS


subscriptions : Model -> Sub Msg
subscriptions model =
  Sub.none
  
  
-- VIEW

renderCards lst =
    ul []
        (map (\l -> li [] [ text l ]) lst)
        
userBordsStyle : Attribute msg
userBordsStyle =
  style
    [ ("float", "left")
      ,("padding", "0 50px")
    ]

view : Model -> Html Msg
view model =
  div [ ] [
    div [ userBordsStyle ]
      [ h2 [] 
           [ text (if model.pl.status == 1 
                   then model.pl.name 
                   else "")
           ]
      , if model.pl.status == 1
          then div [] [ text "" ]
          else input [ type_ "text", placeholder "Votre nom", onInput Name] []
      , div [] 
            [ text ("Cards:"), 
              if model.pl.status == 1 
              then renderCards model.pl.cards 
              else text "" ]
      , div [] 
            [ text ("Points: "), 
              text ( if model.pl.status == 1 
                     then toString (model.pl.points) 
                     else "")]
      , button [ onClick (if model.pl.status == 1 
                          then PickCard 
                          else JoinGame ) ] 
               [ text (if model.pl.status == 1 
                       then "Pick Card" 
                       else "Join Game") ]
      ]
    ]
 
 
-- HTTP

joinGame : String -> Cmd Msg
joinGame player =
  let
    url =
      "http://localhost:8080/newgame/" ++ player

    request =
      Http.get url decodeJoinGame
  in
    Http.send JoinGameResponse request

decodeJoinGame : Decode.Decoder String
decodeJoinGame =
  Decode.field "name" Decode.string

--

pickCard : String -> Cmd Msg
pickCard player =
  let
    url =
      "http://localhost:8080/pickCard/" ++ player

    request =
      Http.get url decodePickCard
  in
    Http.send PickCardResponse request

decodePickCard : Decode.Decoder (List String)
decodePickCard =
  Decode.at [ "cards" ] (Decode.list Decode.string)

--

stopGame : Cmd Msg
stopGame =
  let
    url =
      "http://localhost:8080/stopGame/" ++ model.pl.name

    request =
      Http.get url decodeStopGame
  in
    Http.send StopGameResponse request

decodeStopGame : Decode.Decoder String
decodeStopGame =
  Decode.at ["data", "game"] Decode.string

--

gameState : Cmd Msg
gameState =
  let
    url =
      "http://localhost:8080/gameState/" ++ model.pl.name

    request =
      Http.get url decodeGameState
  in
    Http.send GameStateResponse request

decodeGameState : Decode.Decoder String
decodeGameState =
  Decode.at ["data", "state"] Decode.string