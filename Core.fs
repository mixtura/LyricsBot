module LyricsBot.Core

open Utils
open Model
open Telegram.Bot.Types  
open Telegram.Bot.Types.Enums

let processUpdate (update: Update) =
  let parseGoogleMusicLink str = 
    creatUri str 
    |> Option.filter (fun uri -> uri.Host.Equals "play.google.com") 
    |> Option.bind (extractQueryValueFromUri "t") 
    |> Option.bind (fun query -> query.Replace('_', ' ').Split('-') |> Some) 
    |> function
      | Some [|artist; name;|] -> Some { Artist = artist; Track = name  }
      | Some _ | None -> None
  
  let parseAppleMusicLink str = Some { Artist = ""; Track = ""}

  let parseGetRequest str = 
    [parseGoogleMusicLink; parseAppleMusicLink] 
    |> List.map (fun f -> fun _ -> f str)
    |> firstSome
    |> Option.map GetLyrics

  let parseSearchRequest str = SearchLyrics str |> Some

  let (|MessageUpdate|_|) (u: Update) = 
    if u.Type = UpdateType.Message 
    then Some u
    else None

  match update with
    | MessageUpdate(update) ->
      [parseGetRequest; parseSearchRequest]
        |> List.map (fun x -> fun _ -> x update.Message.Text)
        |> firstSome
    | _ -> None