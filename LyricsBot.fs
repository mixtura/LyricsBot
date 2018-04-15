namespace LyricsBot

open System
open System.Collections.Generic
open Microsoft.Azure.WebJobs.Host
open Newtonsoft.Json
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums

module Utils = 
  open FSharp.Data

  type AppSettings = JsonProvider<"appSettings.json">
  type SearchRequest = {
    ChatId : Int64;
    SearchQuery: string;
  }
  
  let appSettings = AppSettings.GetSample()

  let serializeObj obj = JsonConvert.SerializeObject obj
  
  let deserializeObj<'a> str = JsonConvert.DeserializeObject<'a> str
  
  let tryToOption result = 
    match result with 
    | (true, uri) -> Some uri
    | (false, _) -> None

module SearchLyrics = 
  open Utils

  let run (searchRequestData: string, log: TraceWriter) = 
    let searchRequest = deserializeObj<SearchRequest> searchRequestData
    let apiKey = appSettings.ApiKeys.MusixMatch.ToString()
    let searchResult =
      MusixMatchApi.getLyrics apiKey (searchRequest.SearchQuery, searchRequest.SearchQuery) |> 
      MusixMatchApi.MusixMatchLyricsProvider.Load
    ()

module AnswerToUser =
  open Utils

module ProcessBotUpdate =  
  open Utils  
  
  let creatUri str = Uri.TryCreate(str, UriKind.Absolute) |> tryToOption
  
  // TODO: Consider using fParse
  let extractQueryValueFromUri key (uri : Uri) = 
    uri.Query.Split [|'&'; '='|] |>
    Array.chunkBySize 2 |>
    Array.tryFind (fun pair -> pair.[0] = key) |>
    Option.bind (fun pair -> Some pair.[1])
  
  let parseGoogleMusicLink str = 
    creatUri str |> 
    Option.filter (fun uri -> uri.Host.Equals "play.google.com") |>
    Option.bind (extractQueryValueFromUri "t") |>
    Option.bind (fun query -> query.Replace ('_', ' ') |> Some)
  
  let parseSearchRequest str = 
    str |> 
    parseGoogleMusicLink |> 
    function
      | Some s -> s
      | None -> str

  let (|MessageSearchRequest|_|) (u: Update) = 
    if u.Type = UpdateType.Message 
    then Some {
      ChatId = u.Message.Chat.Id; 
      SearchQuery = (parseSearchRequest u.Message.Text) }
    else None

  let storeSearchRequest (searchRequests: ICollection<string>) searchRequest =  
      serializeObj searchRequest |> 
      searchRequests.Add

  let run(update: Update, searchRequests: ICollection<string>, log: TraceWriter) = 
    log.Info "Process update"
    match update with
      | MessageSearchRequest(request) -> 
          request |> 
          storeSearchRequest searchRequests
          log.Info "Process update success"
      | _ -> log.Info "Process update failed"