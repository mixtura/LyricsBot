module LyricsBot.Bot

open CanaradoApi
open Grabbers.HtmlAgilityWrappers
open Model
open Utils
open System

let parseMessage message =
    let extractLinks (str: string) =
        str.Split [| ' '; '\n'; '\t' |]
        |> List.ofArray
        |> List.map (fun x -> x.Trim())
        |> List.filter (fun x -> x.StartsWith "http://" || x.StartsWith "https://")
        |> List.choose createUri

    let (|LinkWithHost|_|) hostName (link: Uri) =
        if link.Host.Equals hostName then Some link else None

    let rec findValidLink links =
        match links with
        | [] -> None
        | head :: rest ->
            match head with
            | LinkWithHost "play.google.com" link -> GMLink link |> Some
            | LinkWithHost "itunes.apple.com" link -> ItunesLink link |> Some
            | LinkWithHost "open.spotify.com" link -> SpotifyLink link |> Some
            | _ -> findValidLink rest

    match message with
    | "/start" -> Start
    | message ->
        extractLinks message
        |> findValidLink
        |> Option.defaultValue (SearchLyricsQuery message)

let processSpotifyLink url =
    loadDoc url
    |> Grabbers.Spotify.extractSongName
    |> function
    | Some(songName) -> SongInfo songName
    | _ -> Response LyricsNotFound

let processGMLink url =
    loadDoc url
    |> Grabbers.GoggleMusic.extractSongName
    |> function
    | Some(songName) -> SongInfo songName
    | _ -> Response LyricsNotFound

let processItunesLink url =
    loadDoc url
    |> (fun doc -> (Grabbers.Itunes.extractArtist doc, Grabbers.Itunes.extractTrack doc))
    |> function
    | (Some artist, Some track) ->
        SongInfo
            { Artist = artist
              Track = track }
    | _ -> Response LyricsNotFound

let processMessage req =
    let processApiResult (result: CanaradoApiProvider.Root) =
        match result with
        | result when result.Status.Failed = true -> LyricsNotFound
        | result ->
            LyricsFound
                ({ Artist = result.Content.[0].Artist
                   Track = result.Content.[0].Title }, result.Content.[0].Lyrics)

    let api = makeRequest apiKey
    let searchLyrics q = searchLyrics api q |> processApiResult

    let processLinkResult =
        function
        | SongInfo s -> searchLyrics s.SearchQuery
        | Response r -> r

    match req with
    | SearchLyricsQuery query -> searchLyrics query
    | SpotifyLink link -> processSpotifyLink link |> processLinkResult
    | GMLink link -> processGMLink link |> processLinkResult
    | ItunesLink link -> processItunesLink link |> processLinkResult
    | Start -> Response.HelpDoc
