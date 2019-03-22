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

let sendTextMessage (client: TelegramBotClient) (chatId: Int64) body = 
  client.SendTextMessageAsync(ChatId(chatId), body) 
  |> Async.AwaitTask 
  |> Async.RunSynchronously 
  |> ignore