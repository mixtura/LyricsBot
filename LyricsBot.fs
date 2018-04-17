namespace LyricsBot

open System
open System.Collections.Generic
open Microsoft.Azure.WebJobs.Host
open Newtonsoft.Json
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums

module Utils = 
  type GetLyricsRequest = {
    Artist: string;
    Name: string;
  }

  type SearchLyricsRequest = {
    SearchQuery: string;
  }

  let rec firstSome arg funcs = 
    match funcs with
    | x::xs -> Option.orElseWith (fun () -> firstSome arg xs) (x arg)
    | [] -> None

  let serializeObj obj = JsonConvert.SerializeObject obj
  
  let deserializeObj<'a> str = JsonConvert.DeserializeObject<'a> str
  
  let tryToOption result = 
    match result with 
    | (true, uri) -> Some uri
    | (false, _) -> None

module SearchLyrics = 
  open Utils
  open LyricsWikiaGrabber

  let run (searchRequestData: string, log: TraceWriter) = 
    log.Info("Search lyrics started.")
    let searchRequest = deserializeObj<GetLyricsRequest> searchRequestData
    let lyrics = findLyrics searchRequest.Artist searchRequest.Name
    ()

module AnswerToUser =
  open Utils

module ProcessBotUpdate =  
  open Utils  

  let run(update: Update, searchLyricsRequests: ICollection<string>, getLyricsRequests: ICollection<string>, log: TraceWriter) = 
    log.Info "Process bot update started."

    let creatUri str = Uri.TryCreate(str, UriKind.Absolute) |> tryToOption
  
    let extractQueryValueFromUri key (uri : Uri) = 
      uri.Query.Split [|'&'; '='|] |>
      Array.chunkBySize 2 |>
      Array.tryFind (fun pair -> pair.[0] = key) |>
      Option.bind (fun pair -> Some pair.[1])
    
    let parseGoogleMusicLink str = 
      creatUri str 
      |> Option.filter (fun uri -> uri.Host.Equals "play.google.com") 
      |> Option.bind (extractQueryValueFromUri "t") 
      |> Option.bind (fun query -> query.Replace('_', ' ').Split('-') |> Some) 
      |> function
        | Some [|artist; name;|] -> Some { Artist = artist; Name = name  }
        | Some _ | None -> None
    
    let parseApplyMusicLink str = Some { Artist = ""; Name = ""}

    let parseAsGetRequest str = 
      firstSome str [parseGoogleMusicLink; parseApplyMusicLink]

    let parseAsSearchRequest str =
      Some { SearchQuery = str }

    let (|MessageUpdate|_|) (u: Update) = 
      if u.Type = UpdateType.Message 
      then Some u
      else None

    match update with
      | MessageUpdate(update) ->
      
        let createQueueMessage request = (update.Message.Chat.Id, request) |> serializeObj |> Some
          
        let getRequest =
          parseAsGetRequest 
          >> Option.bind createQueueMessage 
          >> Option.map getLyricsRequests.Add

        let searchRequest =
          parseAsSearchRequest 
          >> Option.bind createQueueMessage
          >> Option.map searchLyricsRequests.Add

        firstSome update.Message.Text [getRequest; searchRequest] |> function 
          | Some _ -> log.Info "Process update succeesed." 
          | None -> log.Error "Process update fail"
      | _ -> log.Error "Process update failed"
    ()