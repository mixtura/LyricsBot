module LyricsBot.Bot

open LyricsBot.Core
open LyricsBot.Model
open LyricsBot.Grabbers
open LyricsBot.HtmlAgilityWrappers

module GM = GoggleMusic
module IT = Itunes
module AZ = AZLyrics

let parseMessage message =  
  let tryFindGMLink = 
    tryFindLinkByHost "play.google.com" >> Option.map GMLink

  let tryFindItunesLink = 
    tryFindLinkByHost "itunes.apple.com" >> Option.map ItunesLink

  parser
    [tryFindGMLink; tryFindItunesLink]
    (extractLinks message) 
    message

let processGMLink url=
  GM.extractSongIdFromLink url 
  |> Option.bind(GoggleMusic.createGMPreviewLink)
  |> Option.map(loadDoc)
  |> Option.map(fun doc -> 
    (GM.extractArtist doc,
     GM.extractTrack doc, 
     GM.extractLyrics doc))
  |> function
    | Some (Some artist, Some track, lyricsRes) -> 
      let songName = {Artist = artist; Track = track}
      match lyricsRes with
      | Some lyrics -> 
        LyricsFound (songName, lyrics) |> Response        
      | None -> 
        songName.AsQuery |> SearchQuery
    | _ -> 
       LyricsNotFound |> Response

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