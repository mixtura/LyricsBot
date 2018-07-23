module LyricsBot.Core

open Utils
open Model
open System

let printResponse response =
  match response with
  | LyricsFound (song, lyrics) ->  
    let songNameAsString {Artist = artist; Track = track} = 
      sprintf "%s - %s" artist track
    
    sprintf "%s \n\n %s" (songNameAsString song) lyrics
  | ErrorOccured err -> "error happened"
  | NotFound -> "lyrics not found"

let parseMessage message =
  let extractLinks (str:string) =
    str.Split [|' '; '\n'; '\t'|]
    |> List.ofArray 
    |> List.filter (fun x -> x.StartsWith "http://" || x.StartsWith "https://")
    |> List.map createUri
    |> List.filter Option.isSome
    |> List.map Option.get

  let links = extractLinks message

  let tryFindLinkByHost hostName (links: Uri list) = 
    links |> List.tryFind (fun uri -> uri.Host.Equals hostName)

  let tryFindGMLink (links : Uri list) = 
    links 
    |> tryFindLinkByHost "play.google.com"
    |> Option.map GMLink

  let tryFindItunesLink (links : Uri list) = 
    links
    |> tryFindLinkByHost "itunes.apple.com"
    |> Option.map ItunesLink

  [tryFindGMLink; tryFindItunesLink;]
  |> List.map (fun f -> fun _ -> links |> f)
  |> firstSome
  |> Option.orElse(SearchLyricsQuery message |> Some)