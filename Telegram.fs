module LyricsBot.Telegram

open Microsoft.Extensions.Configuration;
open Microsoft.Azure.WebJobs
open System
open Telegram.Bot
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums  

let (|MessageUpdate|_|) (u: Update) = 
  if u.Type = UpdateType.Message 
  then Some u.Message
  else None

let createTelegramBotClient (context: ExecutionContext) =
  let config = 
    (ConfigurationBuilder())
      .SetBasePath(context.FunctionAppDirectory)
      .AddJsonFile("local.settings.json", true, true)
      .AddEnvironmentVariables()
      .Build()

  let token = config.["telegramBotToken"]

  TelegramBotClient(token)

let splitMessage (message: string) = 
  let maxLength = 4096
  
  let (|Chunk|_|) (str:string) =    
    if str.Length <= maxLength then None else
    match str.Substring(0, maxLength).LastIndexOf('\n') with
    | splitPoint when splitPoint > 0 
      -> Some (str.Substring(0, splitPoint), str.Substring(splitPoint))
    | _ -> Some (str.Substring(0, maxLength), str.Substring(maxLength))

  let rec splitMessageInner (msg: string) acc =
    match msg with
    | Chunk (chunk, rest) -> splitMessageInner rest (chunk::acc) 
    | _ -> msg::acc

  splitMessageInner message [] |> List.rev

let sendTextMessage (client: TelegramBotClient) (chatId: Int64) body = 
  client.SendTextMessageAsync(ChatId(chatId), body) 
  |> Async.AwaitTask 
  |> Async.RunSynchronously 
  |> ignore

let replyTextMessage (client: TelegramBotClient) (chatId: Int64) (replyToId: int) body = 
  client.SendTextMessageAsync(ChatId(chatId), body, replyToMessageId = replyToId) 
  |> Async.AwaitTask 
  |> Async.RunSynchronously 
  |> ignore

let splitAndSendMessages (client: TelegramBotClient) (chatId: Int64) (replyToId: int) body =
  let replyMessages messages = 
    messages |> List.head |> replyTextMessage client chatId replyToId
    messages |> List.skip 1 |> List.iter(fun msg -> sendTextMessage client chatId msg)

  splitMessage body |> replyMessages