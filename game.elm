-- module Game exposing (..)

import Html exposing (Html, button, div, text)
import Html.Events exposing (onClick)
import Http
import Json.Decode as Decode


main =
  Html.program
    {  init = init "cats"
    , view = view
    , update = update
    , subscriptions = subscriptions
    }


-- MODEL

type alias Model =
  {
    playerName : String,
    playerCards : Int,
    playerPoints : Int,
    heapSize : Int,
    endGame : Bool
  }

model : Model
model =
  Model "Joueur 1" 0 0 52 False

init : String -> (Model, Cmd Msg)
init topic =
  ( model
  , newGame model.playerName
  )


-- UPDATE

type Msg 
  = NewGame
  | NewGameResponse (Result Http.Error String)
  | StopGame 
  | StopGameResponse (Result Http.Error String)
  | PickCard
  | PickCardResponse (Result Http.Error String)
  | GameState
  | GameStateResponse (Result Http.Error String)

update : Msg -> Model -> (Model, Cmd Msg)
update msg model =
  case msg of
    NewGame ->
      (model, newGame (model.playerName))
    
    NewGameResponse (Ok _) ->
      (model, Cmd.none)
    
    NewGameResponse (Err _) ->
      (model, Cmd.none)
    
    StopGame ->
      (model, stopGame (model.playerName))
    
    StopGameResponse (Ok _) ->
      (model, Cmd.none)
    
    StopGameResponse (Err _) ->
      (model, Cmd.none)
    
    PickCard ->
      (model, pickCard (model.playerName))
    
    PickCardResponse (Ok _) ->
      (model, Cmd.none)
    
    PickCardResponse (Err _) ->
      (model, Cmd.none)
    
    GameState ->
      (model, gameState (model.playerName))
    
    GameStateResponse (Ok _) ->
      (model, Cmd.none)
    
    GameStateResponse (Err _) ->
      (model, Cmd.none)


-- VIEW

view : Model -> Html Msg
view model =
  div []
    [ div [] [ text (toString model.playerName) ]
    , button [ onClick PickCard ] [ text "Pick a card" ]
    , div [] [ text (toString model.playerPoints) ]
    , div [] [ text (toString model.heapSize) ]
    , div [] [ text (toString model.endGame) ]
    ]


-- HTTP

newGame : String -> Cmd Msg
newGame playerName =
  let
    url =
      "https://localhost:8080/newGame/" ++ playerName

    request =
      Http.get url decodeNewGame
  in
    Http.send NewGameResponse request

decodeNewGame : Decode.Decoder String
decodeNewGame =
  Decode.at ["data", "game"] Decode.string

--

stopGame : String -> Cmd Msg
stopGame playerName =
  let
    url =
      "https://localhost:8080/stopGame/" ++ playerName

    request =
      Http.get url decodeStopGame
  in
    Http.send StopGameResponse request

decodeStopGame : Decode.Decoder String
decodeStopGame =
  Decode.at ["data", "game"] Decode.string

--

pickCard : String -> Cmd Msg
pickCard playerName =
  let
    url =
      "https://localhost:8080/pickCard/" ++ playerName

    request =
      Http.get url decodePickCard
  in
    Http.send PickCardResponse request

decodePickCard : Decode.Decoder String
decodePickCard =
  Decode.at ["data", "card"] Decode.string

--

gameState : String -> Cmd Msg
gameState playerName =
  let
    url =
      "https://localhost:8080/gameState/" ++ playerName

    request =
      Http.get url decodePickCard
  in
    Http.send GameStateResponse request

decodeGameState : Decode.Decoder String
decodeGameState =
  Decode.at ["data", "state"] Decode.string


-- SUBSCRIPTIONS

subscriptions : Model -> Sub Msg
subscriptions model =
  Sub.none