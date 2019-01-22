module LyricsBot.Core

open Utils
open Model
open System

let songNameAsString {Artist = artist; Track = track} = 
  sprintf "%s - %s" artist track

let printResponseLog response =
    match response with
    | HelpDoc -> "Help doc requested."
    | LyricsFound (song, _) ->
      sprintf "Lyrics found for song: %s" (songNameAsString song)
    | LyricsNotFound -> "Lyrics not found."  

let pringLinkProcessingResultLog result =
  match result with
  | SearchQuery q -> sprintf "Search query added: %s" q
  | Response r -> printResponseLog r

let printResponse response =
  match response with
  | HelpDoc -> "Now type song name or share link from your music app (only Google Music allowed at the moment)."
  | LyricsFound (song, lyrics) ->  
    let songNameAsString {Artist = artist; Track = track} = 
      sprintf "%s - %s" artist track
    
    sprintf "%s \n\n %s" (songNameAsString song) lyrics
  | LyricsNotFound -> "Lyrics not found."

let extractLinks (str:string) =
    str.Split [|' '; '\n'; '\t'|]
    |> List.ofArray 
    |> List.filter (fun x -> x.StartsWith "http://" || x.StartsWith "https://")
    |> List.map createUri
    |> List.choose id

let tryFindLinkByHost hostName (links: Uri list) = 
  links |> List.tryFind (function uri -> uri.Host.Equals hostName)

let parser linkFinders links =
  function
  | "/start" -> Start
  | message ->
    linkFinders
    |> List.map(fun finder -> finder links)  
    |> List.choose id 
    |> List.tryHead
    |> Option.defaultValue(SearchLyricsQuery message)
