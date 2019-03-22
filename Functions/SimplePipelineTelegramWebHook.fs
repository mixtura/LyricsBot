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
    let send = telegramBotClient |> sendTextMessage
    response |> (printResponseLog >> log.LogInformation)
    response |> (printResponse >> send chatId) 

  let processMessage req =
    let processLinkResult result =
      match result with
      | SearchQuery q -> processSearchQuery q
      | Response r -> r

    match req with
    | SearchLyricsQuery query -> processSearchQuery query
    | GMLink link -> processGMLink link |> processLinkResult
    | ItunesLink link -> processItunesLink link |> processLinkResult
    | Start -> Response.HelpDoc

  log.LogInformation "SimplePipelineTelegramWebHook started."

  match update with
    | MessageUpdate(message) -> 
      parseMessage message.Text 
      |> processMessage 
      |> (sendTextMessage message.Chat.Id)
    | _ -> log.LogError "Not supported update type."

  log.LogInformation "SimplePipelineTelegramWebHook ended."