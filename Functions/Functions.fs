module LyricsBot.Functions

open System
open Microsoft.Azure.WebJobs.Host
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Model
open Telegram.Bot.Types
open LyricsBot.Telegram

module SearchLyrics = 
  open Grabbers.AZLyrics

  [<FunctionName("SearchLyrics")>]
  let run
    ([<QueueTrigger("search-lyrics-requests")>] searchLyricsReqData, 
     log: TraceWriter, 
     context: ExecutionContext) =

    log.Info "Search lyrics started."

    let (chatId, query) = searchLyricsReqData
    let telegramClient = telegramClient context
    let sendLyrics = sendTextMessage telegramClient chatId
    let lyrics = searchLyrics query

    match lyrics with
      | Some l -> sendLyrics l; log.Info "Search lyrics succeeded"
      | None -> log.Error "Search lyrics failed.";

module ProcessGMLinkRequest =
  open Grabbers.GoggleMusic
  
  [<FunctionName("ProcessGMLinkRequest")>]
  let run 
    ([<QueueTrigger("gm-link-requests")>] searchLyricsReqData, 
     [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>,
     log: TraceWriter, 
     context: ExecutionContext) =

    log.Info "ProcessGMLinkRequest started."

    let (chatId, url) = searchLyricsReqData
    let telegramClient = telegramClient context
    
    let songNameAsString {Artist = artist; Track = track} = 
      sprintf "%s - %s" artist track
    
    let sendLyrics song lyrics = 
      sprintf "%s \n %s" (songNameAsString song) lyrics
      |> sendTextMessage telegramClient chatId

    getLyrics url |> function
    | Some s -> s |> function
      | Lyrics (songName, lyrics) -> 
        sendLyrics songName lyrics 
        log.Info "Lyrics found on GM."

      | SongName s -> 
        (chatId, songNameAsString s) |> searchLyricsRequests.Add
        log.Info "Lyrics not found on GM. Search request added."
    | None -> log.Error "Can't process GM link request."

    log.Info "ProcessGMLinkRequest completed."
    
module TelegramBotHook =
  open Core

  [<FunctionName("TelegramBotHook")>]
  let run 
    ([<HttpTrigger(AuthorizationLevel.Function, "post")>] update: Update, 
     [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>,      
     [<Queue("gm-link-requests")>] gmLinkRequests: ICollector<Int64 * Uri>, 
     log: TraceWriter) = 
    
    log.Info "Telegram bot hook started."

    let processRequest chatId req = 
      match req with
      | SearchLyricsQuery query -> searchLyricsRequests.Add (chatId, query)
      | GMLink link -> gmLinkRequests.Add (chatId, link)
      | ItunesLink link -> log.Error "itunes link can't be parsed yet."; ()

    match update with
      | MessageUpdate(message) -> parseMessage message.Text |> function
        | Some req -> processRequest message.Chat.Id req
        | None -> log.Error "Telegram bot failed to parse message." 
      | _ -> log.Error "Not supported update type."