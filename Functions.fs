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
  open Grabbers.AZLyrics

  [<FunctionName("SearchLyrics")>]
  let run
    ([<QueueTrigger("search-lyrics-requests")>] searchLyricsReqData, 
     log: TraceWriter, 
     context: ExecutionContext) =
     
    log.Info "Search lyrics started."

    let (chatId, query) = searchLyricsReqData
    let telegramClient = telegramClient context
    let sendTextMessage = sendTextMessage telegramClient chatId
    let lyrics = searchLyrics query

    match lyrics with
      | Some l -> sendTextMessage l; log.Info "Search lyrics succeeded"
      | None -> log.Error "Search lyrics failed.";

module GetLyrics = 
  open Grabbers.WikiaLyrics
  open Telegram

  [<FunctionName("GetLyrics")>]
  let run 
    ([<QueueTrigger("get-lyrics-requests")>] getLyricsReqData, 
     [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>, 
     log: TraceWriter, 
     context: ExecutionContext) = 
    
    log.Info "Get lyrics started."

    let (chatId, song) = getLyricsReqData
    let songDescription = sprintf "%s - %s" song.Track song.Artist
    let telegramClient = telegramClient context
    let sendTextMessage = sendTextMessage telegramClient chatId

    song |> getLyrics |> Async.RunSynchronously |> function
      | Some lyrics -> sendTextMessage lyrics; log.Info "Get lyrics succeeded."
      | None -> 
        sprintf 
          "Attempt to get lyrics for song '%s' directly failed." 
          songDescription
        |> log.Error

        (chatId, songDescription) |> searchLyricsRequests.Add

module TelegramBotHook =
  open Core

  [<FunctionName("TelegramBotHook")>]
  let run 
    ([<HttpTrigger(AuthorizationLevel.Function, "post")>] update: Update, 
     [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>, 
     [<Queue("get-lyrics-requests")>] getLyricsRequests: ICollector<Int64 * Song>, 
     log: TraceWriter) = 
    
    log.Info "Telegram bot hook started."

    processUpdate update |> function
      | Some r -> 
        match r with
        | GetLyrics r -> (update.Message.Chat.Id, r) |> getLyricsRequests.Add
        | SearchLyrics r -> (update.Message.Chat.Id, r) |> searchLyricsRequests.Add
        log.Info "Telegram bot hook succeeded."
      | _ -> log.Error "Telegram bot hook failed."

module Test =
  open Grabbers.AZLyrics
  open Grabbers.WikiaLyrics

  [<FunctionName("Test")>]
  let run
    ([<HttpTrigger(AuthorizationLevel.Function, "post")>] req: string,  
     [<Queue("get-lyrics-requests")>] getLyricsRequests: ICollector<Int64 * Song>) = 
  
    let foundLyrics = searchLyrics req |> function
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