module LyricsBot.Telegram

open Telegram.Bot
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums  
open Microsoft.Extensions.Configuration;
open Microsoft.Azure.WebJobs
open System

let (|MessageUpdate|_|) (u: Update) = 
  if u.Type = UpdateType.Message 
  then Some u.Message
  else None

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