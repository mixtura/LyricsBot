module LyricsBot.Functions.ProcessSearchQuery

open Microsoft.Azure.WebJobs.Host
open Microsoft.Azure.WebJobs
open LyricsBot.Telegram
open LyricsBot.Grabbers.AZLyrics

[<FunctionName("ProcessSearchQuery")>]
let run
  ([<QueueTrigger("search-lyrics-requests")>] searchLyricsReqData, 
   log: TraceWriter, 
   context: ExecutionContext) =

  log.Info "Search lyrics started."

  let (chatId, query) = searchLyricsReqData
  let telegramClient = telegramClient context
  let sendLyrics = sendTextMessage telegramClient chatId
  let lyrics = searchLyrics query

  match lyrics with
    | Some l -> sendLyrics l; log.Info "Search lyrics succeeded"
    | None -> log.Error "Search lyrics failed.";