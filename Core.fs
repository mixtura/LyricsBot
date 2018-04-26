module LyricsBot.Core

open Utils
open Model
open System

let parseMessage message =
  let extractLinks (str:string) =
    str.Split [|' '; '\n'; '\t'|]
    |> List.ofArray 
    |> List.filter (fun x -> x.StartsWith "http://" || x.StartsWith "https://")
    |> List.map createUri
    |> List.filter Option.isSome
    |> List.map Option.get

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
  |> List.map (fun f -> fun _ -> extractLinks message |> f)
  |> firstSome
  |> Option.orElse(SearchLyricsQuery message |> Some)