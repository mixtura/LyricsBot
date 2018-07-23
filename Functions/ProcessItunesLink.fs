module LyricsBot.Functions.ProcessItunesLink

open System
open Microsoft.Azure.WebJobs.Host
open Microsoft.Azure.WebJobs
open LyricsBot.Model
open LyricsBot.Telegram
open LyricsBot.Grabbers.Itunes
open LyricsBot
open LyricsBot.HtmlAgilityWrappers

[<FunctionName("ProcessItunesLink")>]
let run 
  ([<QueueTrigger("itunes-link-requests")>] searchLyricsReqData: Int64 * Uri, 
   [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>,
   log: TraceWriter, 
   context: ExecutionContext) =

  let (chatId, url) = searchLyricsReqData
  let telegramClient = telegramClient context

  sprintf "ProcessItunesLinkRequest started. ChatId: %d; Url: %s." chatId (url.ToString()) |> log.Info
  
  let addSearchRequest s = searchLyricsRequests.Add(chatId, s)
  let sendMessage = Core.printResponse >> sendTextMessage telegramClient chatId  
  let onError err = ErrorOccured err |> sendMessage; log.Error err

  loadDoc url 
  |> Result.map(fun doc -> (extractArtist doc, extractTrack doc)) 
  |> function
    | Ok (Ok artist, Ok track) -> 
        sprintf "%s %s" artist track |> addSearchRequest
        log.Info "Song name extracted and forwarded to lyrics searcher."
    | Ok (artist, track) -> Utils.getError [artist; track] |> onError
    | Error msg -> onError msg

  log.Info "ProcessItunesLink completed."