module LyricsBot.Bot

open MouritsApi
open Grabbers.HtmlAgilityWrappers
open Model
open Utils
open System

module GM = LyricsBot.Grabbers.GoggleMusic
module IT = LyricsBot.Grabbers.Itunes

let parseMessage message =  
  let extractLinks (str:string) =
    str.Split [|' '; '\n'; '\t'|]
    |> List.ofArray
    |> List.map (fun x -> x.Trim())
    |> List.filter (fun x -> x.StartsWith "http://" || x.StartsWith "https://")
    |> List.choose createUri

  let (|LinkWithHost|_|) hostName (link: Uri) = 
    if link.Host.Equals hostName 
    then Some link 
    else None

  let rec findValidLink links =
    match links with
    | [] -> None
    | head::rest -> 
      match head with
      | LinkWithHost "play.google.com" link -> GMLink link |> Some
      | LinkWithHost "itunes.apple.com" link -> ItunesLink link |> Some
      | _ -> findValidLink rest 

  match message with
  | "/start" -> Start
  | message ->
    extractLinks message
    |> findValidLink
    |> Option.defaultValue(SearchLyricsQuery message)

let processGMLink url=
  loadDoc url
  |> GM.extractSongName
  |> function
    | Some (songName) -> SongInfo songName
    | _ -> Response LyricsNotFound

let processItunesLink url =
  loadDoc url 
  |> (fun doc -> (IT.extractArtist doc, IT.extractTrack doc)) 
  |> function
    | (Some artist, Some track) -> 
      SongInfo {Artist = artist; Track = track}
    | _ -> Response LyricsNotFound

let processMessage req =
  let processApiResult (result : MouritsApiProvider.Root) = 
    match result with
    | result when result.Success = false -> LyricsNotFound
    | result -> LyricsFound({Artist = result.Artist; Track = result.Song}, result.Result.Lyrics)

  let api = makeRequest apiKey
  let searchLyrics q = searchLyrics api q |> processApiResult
  let getLyrics s = getLyrics api s |> function 
    | result when result.Success = false -> searchLyrics s.SearchQuery
    | result -> processApiResult result

  let processLinkResult = function
  | SongInfo s -> getLyrics s
  | Response r -> r

  match req with
  | SearchLyricsQuery query -> searchLyrics query
  | GMLink link -> processGMLink link |> processLinkResult
  | ItunesLink link -> processItunesLink link |> processLinkResult
  | Start -> Response.HelpDoc