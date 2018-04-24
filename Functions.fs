module LyricsBot.Functions

open System
open Microsoft.Extensions.Configuration;
open Microsoft.Azure.WebJobs.Host
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Model
open Telegram.Bot.Types

module SearchLyrics = 
  ()

module Telegram =
  open Telegram.Bot

  let telegramClient token =
    new TelegramBotClient(token)

module Common = 
  let config basePath = 
    (new ConfigurationBuilder())
      .SetBasePath(basePath)
      .AddJsonFile("local.settings.json", true, true)
      .AddEnvironmentVariables()
      .Build()

module GetLyrics = 
  open Grabbers
  open Telegram
  open Common

  [<FunctionName("GetLyrics")>]
  let run ([<QueueTrigger("get-lyrics-requests")>] getLyricsReqData : Tuple<Int64, Song>, log: TraceWriter, context: ExecutionContext) = 
    log.Info "Get lyrics started."
    
    let config = config context.FunctionAppDirectory
    let telegramClient = telegramClient config.["telegramBotToken"] 
    let (chatId, song) = getLyricsReqData
    let songDescription = sprintf "song '%s' by artist '%s'" song.Track song.Artist
    let sendMessage body = 
      telegramClient.SendTextMessageAsync(new ChatId(chatId), body) 
      |> Async.AwaitTask 
      |> Async.RunSynchronously 
      |> ignore

    song |> getLyrics |> Async.RunSynchronously |> function
      | Some l -> sendMessage l
      | None -> 
        songDescription
          |> sprintf "Get lyrics for %s failed."
          |> log.Error
        songDescription
          |> sprintf "Lyrics for %s not found."  
          |> sendMessage

    (*
    async {
      let! lyrics = getLyrics song
      
      return! lyrics |> function
      | Some l -> telegramClient.SendTextMessageAsync(new ChatId(chatId), l) |> Async.AwaitTask 
      | None -> log.Error "Get lyrics failed."; new Exception("Something gone wrong.") |> raise
    
    } |> ignore
    *)

    log.Info "Get lyrics succeeded."

module TelegramBotHook =
  open Core

  [<FunctionName("TelegramBotHook")>]
  let run 
    ([<HttpTrigger(AuthorizationLevel.Function, "post")>] update: Update, 
     [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Tuple<Int64, string>>, 
     [<Queue("get-lyrics-requests")>] getLyricsRequests: ICollector<Tuple<Int64, Song>>, 
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
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "post")>] req: string,  
     [<Queue("get-lyrics-requests")>] getLyricsRequests: ICollector<Tuple<Int64, Song>>) = 
  
    let song = req.Split('-') |> function
      | [|artist; track|] -> 
        let song = {Artist = artist; Track = track}
        (Int64.MaxValue, song) |> getLyricsRequests.Add
        Some song
      | _ -> None

    song |> Option.bind (getLyrics >> Async.RunSynchronously) |> function
    | Some lyrics -> lyrics
    | None -> raise (new Exception "ERROR")