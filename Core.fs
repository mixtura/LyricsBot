module LyricsBot.Core

open Utils
open Model
open Telegram.Bot.Types  
open Telegram.Bot.Types.Enums

let processUpdate (update: Update) =
  let extractLink (str:string) = 
    str.Split ' ' 
    |> Array.tryFind (fun x -> x.StartsWith "http://" || x.StartsWith "https://")

  let parseGoogleMusicLink str = 
    Option.bind creatUri str 
    |> Option.filter (fun uri -> uri.Host.Equals "play.google.com") 
    |> Option.bind (extractQueryValueFromUri "t") 
    |> Option.bind (fun query -> query.Replace('_', ' ').Split('-') |> Some) 
    |> function
      | Some [|name; artist;|] -> Some { Artist = artist.Trim(); Track = name.Trim()  }
      | Some _ | None -> None
  
  let parseAppleMusicLink str = None

  let parseGetRequest message = 
    [parseGoogleMusicLink; parseAppleMusicLink] 
    |> List.map (fun f -> fun _ -> extractLink message |> f)
    |> firstSome
    |> Option.map GetLyrics

  let parseSearchRequest message = SearchLyrics message |> Some

  let (|MessageUpdate|_|) (u: Update) = 
    if u.Type = UpdateType.Message 
    then Some u.Message
    else None

  match update with
    | MessageUpdate(message) ->
      [parseGetRequest; parseSearchRequest]
        |> List.map (fun x -> fun _ -> x message.Text)
        |> firstSome
    | _ -> None