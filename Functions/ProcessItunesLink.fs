module LyricsBot.Functions.ProcessItunesLink

open Microsoft.Azure.WebJobs
open Microsoft.Extensions.Logging
open LyricsBot
open LyricsBot.Core
open LyricsBot.Bot
open LyricsBot.Model
open LyricsBot.Telegram
open System

[<FunctionName("ProcessItunesLink")>]
let run 
  ([<QueueTrigger("itunes-link-requests")>] searchLyricsReqData: Int64 * Uri, 
   [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>,
   logger: ILogger, 
   context: ExecutionContext) =

  let (chatId, url) = searchLyricsReqData
  let telegramClient = telegramClient context
  let addSearchRequest s = searchLyricsRequests.Add(chatId, s)
  let sendMessage = printResponse >> sendTextMessage telegramClient chatId
  let logResult result = 
    let log = pringLinkProcessingResultLog result

    match result with
    | Response(LyricsNotFound) -> logger.LogError log
    | _ -> logger.LogInformation log

  sprintf "ProcessItunesLinkRequest started. ChatId: %d; Url: %O." chatId url 
  |> logger.LogInformation
  
  let processingResult = processItunesLink url

  match processingResult with
  | Response r-> 
    sendMessage r
  | SearchQuery q ->
    addSearchRequest q

  processingResult |> logResult    
  
  logger.LogInformation "ProcessItunesLink completed."