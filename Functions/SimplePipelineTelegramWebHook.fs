module LyricsBot.Functions.SimplePipelineTelegramWebHook

open LyricsBot.Bot
open LyricsBot.Core
open LyricsBot.Model
open LyricsBot.Telegram
open Microsoft.Extensions.Logging
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Telegram.Bot.Types

[<FunctionName("SimplePipelineTelegramWebHook")>]
let run 
  ([<HttpTrigger(AuthorizationLevel.Function, "post")>] update: Update,
   log: ILogger, 
   context: ExecutionContext) = 

  let telegramBotClient = createTelegramBotClient context
  let sendTextMessage chatId response =
    sprintf "Sending response to chat with id %i. Response:\n%A" chatId response
    |> log.LogInformation 

    let send = telegramBotClient |> splitAndSendMessages
    response |> (printResponse >> send chatId)

  let processMessage req =
    try
      sprintf "Parsed message: %A" req |> log.LogInformation 

      let processLinkResult result =
        match result with
        | SearchQuery q -> processSearchQuery q
        | Response r -> r

      match req with
      | SearchLyricsQuery query -> processSearchQuery query
      | GMLink link -> processGMLink link |> processLinkResult
      | ItunesLink link -> processItunesLink link |> processLinkResult
      | Start -> Response.HelpDoc
    with ex -> 
      log.LogError(ex, "Error happened while processing message.") 
      LyricsNotFound

  log.LogInformation "SimplePipelineTelegramWebHook started."

  match update with
    | MessageUpdate(message) -> 
      sprintf "Received message from chat with id %i. The message text: %s" message.Chat.Id message.Text 
      |> log.LogInformation
      
      parseMessage message.Text 
      |> processMessage 
      |> (sendTextMessage message.Chat.Id)
    | _ -> log.LogError "Not supported update type."

  log.LogInformation "SimplePipelineTelegramWebHook ended."