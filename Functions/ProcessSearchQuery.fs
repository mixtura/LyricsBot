module LyricsBot.Functions.ProcessSearchQuery

open Microsoft.Azure.WebJobs.Host
open Microsoft.Azure.WebJobs
open LyricsBot.Telegram
open LyricsBot.Grabbers.AZLyrics
open LyricsBot.Model
open LyricsBot.Core
open LyricsBot.Utils
open LyricsBot.HtmlAgilityWrappers

[<FunctionName("ProcessSearchQuery")>]
let run
  ([<QueueTrigger("search-lyrics-requests")>] searchLyricsReqData, 
   log: TraceWriter, 
   context: ExecutionContext) =

  let (chatId, query) = searchLyricsReqData
  let telegramClient = telegramClient context
  let sendTextMessage = sendTextMessage telegramClient chatId

  sprintf "ProcessSearchQuery started. ChatId: %d; Query: %s." chatId query |> log.Info
  
  createSearchLyricsUrl query 
  |> Result.bind(loadDoc)
  |> Result.bind(getFirstSearchResultLink)
  |> Result.bind(loadDoc)
  |> Result.map(fun doc -> (extractLyrics doc, extractArtist doc, extractTrack doc))
  |> function 
    | Ok (Ok lyrics, Ok artist, Ok track) -> LyricsFound ({Artist = artist; Track = track }, lyrics)
    | hasError -> 
      match hasError with  
        | Ok (lyricsRes, artistRes, trackRes) -> aggregateErrors [lyricsRes; artistRes; trackRes] 
        | Error err -> err
      |> log.Error
      LyricsNotFound
  |> printResponse
  |> sendTextMessage

  log.Info "ProcessSearchQuery ended."