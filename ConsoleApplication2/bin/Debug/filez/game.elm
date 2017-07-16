module Game exposing (..)

import Html exposing (program, ul, li, text, div, h2, h3, input, button, Html, Attribute)
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
  
type alias Adversaire = 
  {cards:Int
  ,points:Int
  ,name:String
  ,status:Int
  }
adversaire : Adversaire
adversaire = 
  {cards = 0
  ,points=0
  ,name = ""
  ,status = 0
  }

type alias Model =
  {pl : Player
  ,adv: Adversaire
  }
model : Model
model = 
  {pl=player
  ,adv = adversaire
  }
  
init : (Model, Cmd Msg)
init =
  (model,
  Cmd.none)


-- UPDATE

type Msg
  = Display
  | Name String
  | JoinGame
  | JoinGameResponse (Result Http.Error Model)
  | PickCard
  | PickCardResponse (Result Http.Error Model)
  | GameState
  | GameStateResponse (Result Http.Error String)
  | None


update : Msg -> Model -> (Model, Cmd Msg)
update msg model =
  case msg of
    Display ->
      (model, Cmd.none)
    PickCard ->
      (model, pickCard model.pl.name)
    PickCardResponse (Ok player) ->
      (Model (model.pl) (model.adv)
      ,Cmd.none)
    PickCardResponse (Err _) ->
      (Model (model.pl) (model.adv)
      ,Cmd.none)    
    GameState ->
      (model, gameState)   
    GameStateResponse (Ok _) ->
      (model, Cmd.none)   
    GameStateResponse (Err _) ->
      (model, Cmd.none)
    JoinGame ->
      (model, joinGame model.pl.name)
    JoinGameResponse (Ok model) ->
      (Model (model.pl) (model.adv)
      ,Cmd.none) 
    JoinGameResponse (Err _) ->
      (Model (model.pl) (model.adv)
      ,Cmd.none)
    Name name ->
      (Model
        (Player model.pl.cards model.pl.points name model.pl.status)
          (Adversaire model.adv.cards model.adv.points model.adv.name model.adv.status)
        ,Cmd.none)
    None ->
      (model, Cmd.none)
      

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
           [ text (if model.pl.status /= 0 
                   then model.pl.name 
                   else "")
           ]
      , if model.pl.status /= 0
          then div [] [ text "" ]
          else input [ type_ "text", placeholder "Votre nom", onInput Name] []
      , div [] 
            [ text ("Cards:"), 
              if model.pl.status /= 0 
              then renderCards model.pl.cards 
              else text "" ]
      , div [] 
            [ text ("Points: "), 
              text ( if model.pl.status /= 0 
                     then toString (model.pl.points) 
                     else "")]
      , button [ onClick (if model.pl.status /= 0 
                          then PickCard 
                          else if model.pl.status == 0
                          then JoinGame 
                          else None) ] 
               [ text (if model.pl.status /= 0 
                       then "Pick Card"
                       else if model.pl.status == 0 
                       then "Join Game"
                       else "") ]
      ]
    ,div [userBordsStyle]
      [ h3 [] 
           [ text (if (model.adv.status == 1) || (model.pl.status == 1)
                   then "against" 
                   else "")
           ]
       ]
    ,div [ userBordsStyle ]
      [ h2 [] [ text (if model.adv.status /= 0
                      then model.adv.name 
                      else "waiting for a new player...")
              ]
      , div [] 
            [ text (if model.adv.status /= 0
                      then "Cards: " 
                      else ""),
              text (if model.adv.status /= 0
                      then toString model.adv.cards
                      else "")
            ]
      , div [] 
            [ text (if model.adv.status /= 0
                    then "Points: "
                    else ""), 
              text ( if model.adv.status /= 0
                     then toString model.adv.points
                     else "" )
            ]
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

decodeJoinGame : Decode.Decoder Model
decodeJoinGame =
  Decode.map2
    Model
      (Decode.map4
        Player
          (Decode.at ["player", "cards"] (Decode.list Decode.string))
          (Decode.at ["player", "points"] Decode.int)
          (Decode.at ["player", "name"] Decode.string)
          (Decode.at ["player", "status"] Decode.int)
      )
      (Decode.map4
        Adversaire
          (Decode.at ["adversaire", "cards"] Decode.int)
          (Decode.at ["adversaire", "points"] Decode.int)
          (Decode.at ["adversaire", "name"] Decode.string)
          (Decode.at ["adversaire", "status"] Decode.int)
      )

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

decodePickCard : Decode.Decoder Model
decodePickCard =
  Decode.map2
    Model
      (Decode.map4
        Player
          (Decode.at ["player", "cards"] (Decode.list Decode.string))
          (Decode.at ["player", "points"] Decode.int)
          (Decode.at ["player", "name"] Decode.string)
          (Decode.at ["player", "status"] Decode.int)
      )
      (Decode.map4
        Adversaire
          (Decode.at ["adversaire", "cards"] Decode.int)
          (Decode.at ["adversaire", "points"] Decode.int)
          (Decode.at ["adversaire", "name"] Decode.string)
          (Decode.at ["adversaire", "status"] Decode.int)
      )

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