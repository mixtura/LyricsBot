namespace LyricsBot

open System
open System.Collections.Generic
open Microsoft.Azure.WebJobs.Host
open Newtonsoft.Json
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums
open System

module Utils = 
  type GetLyricsRequest = {
    Artist: string;
    Track: string;
  }

  type SearchLyricsRequest = {
    SearchQuery: string;
  }

  let rec firstSome fs = 
    match fs with
    | x::xs -> Option.orElseWith (fun _ -> firstSome xs) (x())
    | [] -> None

  let tryToOption result = 
    match result with 
    | (true, uri) -> Some uri
    | (false, _) -> None

  let creatUri str = Uri.TryCreate(str, UriKind.Absolute) |> tryToOption

  let extractQueryValueFromUri key (uri : Uri) = 
    uri.Query.Split [|'&'; '='|] |>
    Array.chunkBySize 2 |>
    Array.tryFind (fun pair -> pair.[0] = key) |>
    Option.bind (fun pair -> Some pair.[1])
  
  let serializeObj obj = JsonConvert.SerializeObject obj  
  let deserializeObj<'a> str = JsonConvert.DeserializeObject<'a> str

module SearchLyrics = 
  open Utils

module GetLyrics = 
  open Utils
  open LyricsWikiaGrabber

  let run (getLyricsReqData: string, log: TraceWriter) = 
    log.Info("Get lyrics started.")
    let (getLyricsReq, chatId) = deserializeObj<Tuple<GetLyricsRequest, int>> getLyricsReqData
    let lyrics = findLyrics getLyricsReq.Artist getLyricsReq.Track
    ()

module AnswerToUser =
  open Utils

module ProcessBotUpdate =  
  open Utils  

  let run(update: Update, searchLyricsRequests: ICollection<string>, getLyricsRequests: ICollection<string>, log: TraceWriter) = 
    log.Info "Process bot update started."

    let parseGoogleMusicLink str = 
      creatUri str 
      |> Option.filter (fun uri -> uri.Host.Equals "play.google.com") 
      |> Option.bind (extractQueryValueFromUri "t") 
      |> Option.bind (fun query -> query.Replace('_', ' ').Split('-') |> Some) 
      |> function
        | Some [|artist; name;|] -> Some { Artist = artist; Track = name  }
        | Some _ | None -> None
    
    let parseAppleMusicLink str = Some { Artist = ""; Track = ""}

    let parseAsGetRequest str = 
      [parseGoogleMusicLink; parseAppleMusicLink] 
      |> List.map (fun f -> fun _ -> f str)
      |> firstSome

    let parseAsSearchRequest str =
      Some { SearchQuery = str }

    let (|MessageUpdate|_|) (u: Update) = 
      if u.Type = UpdateType.Message 
      then Some u
      else None

    match update with
      | MessageUpdate(update) ->
      
        let createQueueMessage request = (update.Message.Chat.Id, request) |> serializeObj |> Some
        let bindArg arg func = fun _ -> func arg
        
        let getRequest =
          parseAsGetRequest 
          >> Option.bind createQueueMessage 
          >> Option.map getLyricsRequests.Add

        let searchRequest =
          parseAsSearchRequest 
          >> Option.bind createQueueMessage
          >> Option.map searchLyricsRequests.Add
          
        [getRequest; searchRequest]
          |> List.map (bindArg update.Message.Text)
          |> firstSome
          |> function 
            | Some _ -> log.Info "Process update succeesed." 
            | None -> log.Error "Process update fail"

      | _ -> log.Error "Process update failed"
    ()