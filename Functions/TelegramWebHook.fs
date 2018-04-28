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