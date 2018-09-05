module LyricsBot.Functions.TelegramBotHook

open Microsoft.Azure.WebJobs.Host
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open LyricsBot.Core
open LyricsBot.Model
open LyricsBot.Telegram
open System
open Telegram.Bot.Types

[<FunctionName("TelegramBotHook")>]
let run 
  ([<HttpTrigger(AuthorizationLevel.Function, "post")>] update: Update, 
   [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>,      
   [<Queue("gm-link-requests")>] gmLinkRequests: ICollector<Int64 * Uri>, 
   [<Queue("itunes-link-requests")>] itunesLinkRequests: ICollector<Int64 * Uri>,
   log: TraceWriter, 
   context: ExecutionContext) = 

  log.Info "Telegram bot hook started."

  let telegramClient = telegramClient context
  let sendNotFound chatId = 
    LyricsNotFound
    |> printResponse 
    |> sendTextMessage telegramClient chatId

  let processRequest chatId req = 
    match req with
    | SearchLyricsQuery query -> searchLyricsRequests.Add (chatId, query)
    | GMLink link -> gmLinkRequests.Add (chatId, link)
    | ItunesLink link -> itunesLinkRequests.Add (chatId, link)

  match update with
    | MessageUpdate(message) -> parseMessage message.Text |> function
      | Some req -> processRequest message.Chat.Id req
      | None -> 
        sendNotFound message.Chat.Id; 
        log.Error "Telegram bot failed to parse message.";
    | _ -> log.Error "Not supported update type."

  log.Info "Telegram bot hook ended."