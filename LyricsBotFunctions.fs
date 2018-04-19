namespace LyricsBot

open System
open System.Collections.Generic
open Microsoft.Azure.WebJobs.Host
open Model
open Utils

module SearchLyrics = 
  ()

module GetLyrics = 
  open LyricsWikiaGrabber

  let run (getLyricsReqData: string, log: TraceWriter) = 
    log.Info("Get lyrics started.")
    let (song, chatId) = deserializeObj<Tuple<Song, int>> getLyricsReqData
    let lyrics = getLyrics song
    ()

module AnswerToUser =
  ()

module ProcessBotUpdate =  
  open Telegram.Bot.Types
  open Telegram.Bot.Types.Enums

  let run (update: Update, 
           searchLyricsRequests: ICollection<string>, 
           getLyricsRequests: ICollection<string>, 
           log: TraceWriter) = 
    
    log.Info "Process update started."

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

    let parseSearchRequest str =
      SearchLyrics str
      |> Some

    let (|MessageUpdate|_|) (u: Update) = 
      if u.Type = UpdateType.Message 
      then Some u
      else None

    match update with
      | MessageUpdate(update) ->
      
        let createQueueMessage request = (update.Message.Chat.Id, request) |> serializeObj

        [parseGetRequest; parseSearchRequest]
          |> List.map (fun x -> fun _ -> x update.Message.Text)
          |> firstSome
          |> function 
            | Some request -> 
              match request with
                | GetLyrics r -> createQueueMessage r |> getLyricsRequests.Add
                | SearchLyrics r -> createQueueMessage r |> searchLyricsRequests.Add
              log.Info "Process request successed."
            | None -> log.Error "Process update fail."

      | _ -> log.Error "Process update failed."
    ()