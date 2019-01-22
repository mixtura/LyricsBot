module LyricsBot.Functions.TelegramBotHook

open Microsoft.Extensions.Logging
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open LyricsBot.Bot
open LyricsBot.Core
open LyricsBot.Model
open LyricsBot.Telegram
open System
open Telegram.Bot.Types

[<FunctionName("TelegramWebHook")>]
let run 
  ([<HttpTrigger(AuthorizationLevel.Function, "post")>] update: Update, 
   [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>,      
   [<Queue("gm-link-requests")>] gmLinkRequests: ICollector<Int64 * Uri>, 
   [<Queue("itunes-link-requests")>] itunesLinkRequests: ICollector<Int64 * Uri>,
   log: ILogger, 
   context: ExecutionContext) = 

  let telegramClient = telegramClient context
  let sendTextMessage response chatId =
    printResponseLog response 
    |> log.LogInformation 

    response
    |> printResponse
    |> sendTextMessage telegramClient chatId 

  let processRequest chatId req = 
    match req with
    | SearchLyricsQuery query -> searchLyricsRequests.Add (chatId, query)
    | GMLink link -> gmLinkRequests.Add (chatId, link)
    | ItunesLink link -> itunesLinkRequests.Add (chatId, link)
    | Start -> sendTextMessage HelpDoc chatId

  log.LogInformation "Telegram bot hook started."

  match update with
    | MessageUpdate(message) -> 
      parseMessage message.Text |> processRequest message.Chat.Id
    | _ -> log.LogError "Not supported update type."

  log.LogInformation "Telegram bot hook ended."