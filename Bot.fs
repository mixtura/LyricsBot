module LyricsBot.Bot

open LyricsBot.HtmlAgilityWrappers
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
  GM.extractSongIdFromLink url 
  |> Option.bind(GM.createGMPreviewLink)
  |> Option.map(loadDoc)
  |> Option.map(fun doc -> 
    (GM.extractArtist doc,
     GM.extractTrack doc, 
     GM.extractLyrics doc))
  |> function
    | Some (Some artist, Some track, lyricsRes) -> 
      let songName = {Artist = artist; Track = track}
      match lyricsRes with
      | Some lyrics -> LyricsFound (songName, lyrics) |> Response        
      | None -> songName.AsQuery |> SearchQuery
    | _ -> LyricsNotFound |> Response

let processItunesLink url =
  loadDoc url 
  |> (fun doc -> (IT.extractArtist doc, IT.extractTrack doc)) 
  |> function
    | (Some artist, Some track) -> 
      {Artist = artist; Track = track}.AsQuery |> SearchQuery
    | _ -> LyricsNotFound |> Response

let processSearchQuery query =
  AZ.createSearchLyricsUrl query 
  |> Option.map(loadDoc)
  |> Option.bind(AZ.getFirstSearchResultLink)
  |> Option.map(loadDoc)
  |> Option.map(fun doc -> 
    (AZ.extractLyrics doc, 
     AZ.extractArtist doc, 
     AZ.extractTrack doc))
  |> function 
    | Some (Some lyrics, Some artist, Some track) -> 
      LyricsFound ({Artist = artist; Track = track }, lyrics)
    | _ -> LyricsNotFound