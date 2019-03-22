module LyricsBot.Functions.ProcessSearchQuery

open LyricsBot.Telegram
open LyricsBot.Model
open LyricsBot.Core
open LyricsBot.Bot
open Microsoft.Azure.WebJobs
open Microsoft.Extensions.Logging

[<FunctionName("ProcessSearchQuery")>]
let run
  ([<QueueTrigger("search-lyrics-requests")>] searchLyricsReqData, 
   logger: ILogger, 
   context: ExecutionContext) =

  let (chatId, query) = searchLyricsReqData
  let telegramBotClient = createTelegramBotClient context
  let sendMessage = printResponse >> sendTextMessage telegramBotClient chatId
  let logResponse response = 
    let log = printResponseLog response

    match response with
    | LyricsNotFound -> logger.LogError log
    | _ -> logger.LogInformation log

  sprintf "ProcessSearchQuery started. ChatId: %d; Query: %s." chatId query 
  |> logger.LogInformation
  
  let response = processSearchQuery query

  response |> sendMessage
  response |> logResponse  

  logger.LogInformation "ProcessSearchQuery ended."