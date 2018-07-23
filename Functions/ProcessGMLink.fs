module LyricsBot.Functions.ProcessGMLink

open System
open Microsoft.Azure.WebJobs.Host
open Microsoft.Azure.WebJobs
open LyricsBot.Model
open LyricsBot.Telegram
open LyricsBot.Grabbers.GoggleMusic
open LyricsBot
open LyricsBot.HtmlAgilityWrappers

[<FunctionName("ProcessGoogleMusicLink")>]
let run 
  ([<QueueTrigger("gm-link-requests")>] searchLyricsReqData: Int64 * Uri, 
   [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>,
   log: TraceWriter, 
   context: ExecutionContext) =

  let (chatId, url) = searchLyricsReqData
  let telegramClient = telegramClient context
  
  sprintf "ProcessGMLinkRequest started. ChatId: %d; Url: %s." chatId (url.ToString()) |> log.Info

  let addSearchRequest artist track = searchLyricsRequests.Add(chatId, {Artist = artist; Track = track } |> toQuery)
  let sendMessage = Core.printResponse >> sendTextMessage telegramClient chatId
  let onError err = 
    ErrorOccured err |> sendMessage
    log.Error err

  loadDoc url 
  |> Result.bind(getRedirectLink)
  |> Result.bind(loadDoc)
  |> Result.map(fun doc -> (extractArtist doc, extractTrack doc, extractLyrics doc))
  |> function
    | Ok (Ok artist, Ok track, lyrics) -> 
      match lyrics with
      | Ok lyrics -> 
        LyricsFound ({Artist = artist; Track = track}, lyrics) |> sendMessage
        log.Info "Lyrics found on GM."
      | Error err -> 
        addSearchRequest artist track
        log.Error err
        log.Info "Search request added."
    | Ok (artist, track, lyrics) -> Utils.getError [artist; track; lyrics] |> onError
    | Error err -> onError err

  log.Info "ProcessGMLinkRequest completed."