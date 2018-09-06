module LyricsBot.Core

open Utils
open Model
open System

let printResponse response =
  match response with
  | HelpDoc -> "Now type song name or share link from your music app (only Google Music allowed at the moment)."
  | LyricsFound (song, lyrics) ->  
    let songNameAsString {Artist = artist; Track = track} = 
      sprintf "%s - %s" artist track
    
    sprintf "%s \n\n %s" (songNameAsString song) lyrics
  | LyricsNotFound -> "Lyrics not found."

let parseMessage message =
  let extractLinks (str:string) =
    str.Split [|' '; '\n'; '\t'|]
    |> List.ofArray 
    |> List.filter (fun x -> x.StartsWith "http://" || x.StartsWith "https://")
    |> List.map createUri

  let tryFindLinkByHost hostName (links: Result<Uri, string> list) = 
    links 
    |> List.tryFind (function | Ok uri -> uri.Host.Equals hostName | _ -> false) 
    |> function 
      | Some(Ok uri) -> Some uri 
      | _ -> None

  let tryFindGMLink = 
    tryFindLinkByHost "play.google.com"
    >> Option.map GMLink

  let tryFindItunesLink = 
    tryFindLinkByHost "itunes.apple.com"
    >> Option.map ItunesLink

  let rec tryFindLink linkFinders links =
    match linkFinders with
    | [] -> SearchLyricsQuery message |> Some
    | f::rest -> f links |> function 
      | Some v -> Some v 
      | None -> tryFindLink rest links

  match message with
  | "/start" -> Some Start
  | message -> extractLinks message |> tryFindLink [tryFindGMLink; tryFindItunesLink;]
