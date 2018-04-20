module LyricsBot.Functions

open System
open System.Configuration;
open Microsoft.Azure.WebJobs.Host
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Model

module SearchLyrics = 
  ()

module Telegram =
  open Telegram.Bot

  let telegramClient =
    let telegramBotToken = ConfigurationManager.AppSettings.["TelegramBotToken"]
    new TelegramBotClient(telegramBotToken)

module GetLyrics = 
  open Grabbers
  open Telegram

  [<FunctionName("GetLyrics")>]
  let run ([<QueueTrigger("get-lyrics-requests")>] getLyricsReqData, log: TraceWriter) = 
    log.Info "Get lyrics started."
    
    let (chatId, song) = getLyricsReqData
        
    async {
      let! lyrics = getLyrics song
      
      return! lyrics |> function
      | Some l -> telegramClient.SendTextMessageAsync(chatId, l) |> Async.AwaitTask 
      | None -> log.Error "Get lyrics failed."; new Exception("Something gone wrong.") |> raise
    
    } |> ignore

    log.Info "Get lyrics successed."

module TelegramBotHook =  
  open Telegram.Bot.Types
  open Core
  
  [<FunctionName("TelegramBotHook")>]
  let run 
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "post")>] update: Update, 
     [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Tuple<Int64, string>>, 
     [<Queue("get-lyrics-requests")>] getLyricsRequests: ICollector<Tuple<Int64, Song>>, 
     log: TraceWriter) = 
    
    log.Info "Process update started."

    processUpdate update |> function
      | Some r -> 
        match r with
        | GetLyrics r -> (update.Message.Chat.Id, r) |> getLyricsRequests.Add
        | SearchLyrics r -> (update.Message.Chat.Id, r) |> searchLyricsRequests.Add
        log.Info "Process update successed."
      | _ -> log.Error "Process update failed."

module Test =
  open Grabbers

  [<FunctionName("Test")>]
  let run
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "post")>] req: string,  
     [<Queue("get-lyrics-requests")>] getLyricsRequests: ICollector<Tuple<Song, Int64>>) = 
  
    let song = req.Split('-') |> function
      | [|artist; track|] -> 
        let song = {Artist = artist; Track = track}
        (song, Int64.MaxValue) |> getLyricsRequests.Add
        Some song
      | _ -> None

    song |> Option.bind (getLyrics >> Async.RunSynchronously) |> function
    | Some lyrics -> lyrics
    | None -> raise (new Exception "ERROR")