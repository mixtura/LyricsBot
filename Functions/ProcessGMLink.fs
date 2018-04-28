module LyricsBot.Functions.ProcessGMLink

open System
open Microsoft.Azure.WebJobs.Host
open Microsoft.Azure.WebJobs
open LyricsBot.Model
open LyricsBot.Telegram
open LyricsBot.Grabbers.GoggleMusic

[<FunctionName("ProcessGoogleMusicLink")>]
let run 
  ([<QueueTrigger("gm-link-requests")>] searchLyricsReqData: Int64 * Uri, 
   [<Queue("search-lyrics-requests")>] searchLyricsRequests: ICollector<Int64 * string>,
   log: TraceWriter, 
   context: ExecutionContext) =

  log.Info "ProcessGMLinkRequest started."

  let (chatId, url) = searchLyricsReqData
  let telegramClient = telegramClient context

  let songNameAsString {Artist = artist; Track = track} = 
    sprintf "%s - %s" artist track

  let sendLyrics song lyrics = 
    sprintf "%s \n %s" (songNameAsString song) lyrics
    |> sendTextMessage telegramClient chatId

  getLyrics url |> function
  | Some s -> s |> function
    | Lyrics (songName, lyrics) -> 
      sendLyrics songName lyrics 
      log.Info "Lyrics found on GM."

    | SongName s -> 
      (chatId, songNameAsString s) |> searchLyricsRequests.Add
      log.Info "Lyrics not found on GM. Search request added."
  | None -> log.Error "Can't process GM link request."

  log.Info "ProcessGMLinkRequest completed."