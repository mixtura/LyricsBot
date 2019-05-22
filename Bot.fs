module LyricsBot.Bot

open LyricsBot.Grabbers.HtmlAgilityWrappers
open LyricsBot.Model
open LyricsBot.Utils
open System

module AZ = LyricsBot.Grabbers.AZLyrics
module GM = LyricsBot.Grabbers.GoggleMusic
module IT = LyricsBot.Grabbers.Itunes

let parseMessage message =  
  let extractLinks (str:string) =
    str.Split [|' '; '\n'; '\t'|]
    |> List.ofArray
    |> List.map (fun x -> x.Trim())
    |> List.filter (fun x -> x.StartsWith "http://" || x.StartsWith "https://")
    |> List.map createUri
    |> List.choose id

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
  |> GM.extractSongTitle
  |> function
    | Some (title) -> SearchQuery title
    | _ -> Response LyricsNotFound

let processItunesLink url =
  loadDoc url 
  |> (fun doc -> (IT.extractArtist doc, IT.extractTrack doc)) 
  |> function
    | (Some artist, Some track) -> 
      let songName = {Artist = artist; Track = track}
      SearchQuery songName.Full
    | _ -> Response LyricsNotFound

let processSearchQuery =
  AZ.createSearchLyricsUrl 
  >> Option.map(loadDoc)
  >> Option.bind(AZ.getFirstSearchResultLink)
  >> Option.map(loadDoc)
  >> Option.map(fun doc -> 
    (AZ.extractLyrics doc, 
     AZ.extractArtist doc, 
     AZ.extractTrack doc))
  >> function 
    | Some (Some lyrics, Some artist, Some track) -> 
      LyricsFound ({Artist = artist; Track = track }, lyrics)
    | _ -> LyricsNotFound