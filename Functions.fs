module LyricsBot.Functions

open System
open Microsoft.Extensions.Configuration;
open Microsoft.Azure.WebJobs.Host
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Model
open Telegram.Bot.Types

module Telegram =
  open Telegram.Bot

  let telegramClient (context: ExecutionContext) =
    let config = 
      (new ConfigurationBuilder())
        .SetBasePath(context.FunctionAppDirectory)
        .AddJsonFile("local.settings.json", true, true)
        .AddEnvironmentVariables()
        .Build()
    
    let token = config.["telegramBotToken"]

    new TelegramBotClient(token)

  let sendTextMessage (client: TelegramBotClient) (chatId: Int64) body = 
    client.SendTextMessageAsync(new ChatId(chatId), body) 
    |> Async.AwaitTask 
    |> Async.RunSynchronously 
    |> ignore

module SearchLyrics = 
  open Telegram
  open Grabbers

  [<FunctionName("SearchLyrics")>]
  let run
    ([<QueueTrigger("search-lyrics-requests")>] searchLyricsReqData, 
     log: TraceWriter, 
     context: ExecutionContext) =
     
    log.Info "Search lyrics started."

    let (chatId, query) = searchLyricsReqData
    let telegramClient = telegramClient context
    let sendTextMessage = sendTextMessage telegramClient chatId
    let lyrics = searchLyrics query |> Async.RunSynchronously

    match lyrics with
      | Some l -> sendTextMessage l; log.Info "Search lyrics succeeded"
      | None -> log.Error "Search lyrics failed."; 

    log.Info "Search lyrics completed."

module GetLyrics = 
  open Grabbers
  open Telegram

  [<FunctionName("GetLyrics")>]
  let run 
    ([<QueueTrigger("get-lyrics-requests")>] getLyricsReqData, 
     log: TraceWriter, 
     context: ExecutionContext) = 
    
    log.Info "Get lyrics started."

    let (chatId, song) = getLyricsReqData
    let songDescription = sprintf "song '%s' by artist '%s'" song.Track song.Artist
    let telegramClient = telegramClient context
    let sendTextMessage = sendTextMessage telegramClient chatId

    song |> getLyrics |> Async.RunSynchronously |> function
      | Some lyrics -> sendTextMessage lyrics
      | None -> 
        songDescription
          |> sprintf "Get lyrics for %s failed."
          |> log.Error
        songDescription
          |> sprintf "Lyrics for %s not found."  
          |> sendTextMessage

    log.Info "Get lyrics completed."

module TelegramBotHook =
  open Core

  [<FunctionName("TelegramBotHook")>]
  let run 
    ([<HttpTrigger(AuthorizationLevel.Function, "post")>] update: Update, 
     [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>, 
     [<Queue("get-lyrics-requests")>] getLyricsRequests: ICollector<Int64 * Song>, 
     log: TraceWriter) = 
    
    log.Info "Process update started."

    processUpdate update |> function
      | Some r -> 
        match r with
        | GetLyrics r -> (update.Message.Chat.Id, r) |> getLyricsRequests.Add
        | SearchLyrics r -> (update.Message.Chat.Id, r) |> searchLyricsRequests.Add
        log.Info "Process update succeeded."
      | _ -> log.Error "Process update failed."

module Test =
  open Grabbers

  [<FunctionName("Test")>]
  let run
    ([<HttpTrigger(AuthorizationLevel.Function, "post")>] req: string,  
     [<Queue("get-lyrics-requests")>] getLyricsRequests: ICollector<Int64 * Song>) = 
  
    let foundLyrics = searchLyrics req |> Async.RunSynchronously |> function
      | Some l -> l
      | None -> "none"

    let song = req.Split('-') |> function
      | [|artist; track|] -> 
        let song = {Artist = artist; Track = track}
        (Int64.MaxValue, song) |> getLyricsRequests.Add
        Some song
      | _ -> None

    song |> Option.bind (getLyrics >> Async.RunSynchronously) |> function
    | Some lyrics -> lyrics
    | None -> raise (new Exception "ERROR")