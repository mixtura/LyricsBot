module LyricsBot.Functions.TelegramWebHook

open LyricsBot.Bot
open LyricsBot.Core
open LyricsBot.Model
open LyricsBot.Telegram
open Microsoft.Extensions.Logging
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Telegram.Bot.Types

[<FunctionName("TelegramWebHook")>]
let run ([<HttpTrigger(AuthorizationLevel.Function, "post")>] update: Update, log: ILogger, context: ExecutionContext) =

    let telegramBotClient = createTelegramBotClient

    let sendTextMessage chatId messageId response =
        sprintf "Sending response to chat with id %i. Response:\n%A" chatId response
        |> log.LogInformation

        response |> (renderResponse >> splitAndSendMessages telegramBotClient chatId messageId)

    let processMessage req =
        try
            sprintf "Parsed message: %A" req |> log.LogInformation
            processMessage req
        with ex ->
            log.LogError(ex, "Error happened while processing message.")
            LyricsNotFound

    log.LogInformation "TelegramWebHook started."

    match update with
    | MessageUpdate(message) ->
        sprintf "Received message from chat with id %i. The message text: %s" message.Chat.Id message.Text
        |> log.LogInformation

        parseMessage message.Text
        |> processMessage
        |> (sendTextMessage message.Chat.Id message.MessageId)
    | _ -> log.LogError "Not supported update type."

    log.LogInformation "TelegramWebHook ended."
