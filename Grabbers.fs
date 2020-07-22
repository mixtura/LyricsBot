module LyricsBot.Grabbers

open System
open System.Web
open Utils

module HtmlAgilityWrappers =
    open HtmlAgilityPack

    let loadDoc (url: Uri) =
        try
            (HtmlWeb()).Load(url)
        with ex -> raise (new Exception("Error loading " + url.ToString(), ex))

    let extractFirstNode (selector: string) (doc: HtmlDocument) =
        doc.DocumentNode.SelectSingleNode(selector) |> Option.ofObj

    let extractAllNodes (selector: string) (doc: HtmlDocument) = doc.DocumentNode.SelectNodes(selector) |> Option.ofObj

    let extractAttr attrName (node: HtmlNode) =
        node.GetAttributeValue(attrName, "")
        |> Some
        |> Option.bind (function
            | "" -> None
            | x -> HttpUtility.HtmlDecode(x) |> Some)

    let extractText (node: HtmlNode) = node.InnerText |> HttpUtility.HtmlDecode

// Xpath selectors
module private Selectors =
    module Spotify =
        let metaTitleSelector = "/html/head/meta[@property='twitter:title']"
        let metaAuthorSelector = "/html/head/meta[@property='twitter:audio:artist_name']"

    module GM =
        let metaTitleSelector = "/html/head/meta[@property='og:title']"

    module Itunes =
        let trackNameSelector = "//*[@class='table__row popularity-star we-selectable-item is-active is-available we-selectable-item--allows-interaction ember-view']/td[2]/div/div/div"
        let artistNameSelector = "//*[@class='product-header__identity']/a"

[<RequireQualifiedAccess>]
module Spotify =
    open HtmlAgilityWrappers
    open LyricsBot.Model

    let extractSongName doc =
        let title =
            doc
            |> extractFirstNode Selectors.Spotify.metaTitleSelector
            |> Option.bind (extractAttr "content")

        let author =
            doc
            |> extractFirstNode Selectors.Spotify.metaAuthorSelector
            |> Option.bind (extractAttr "content")

        let toSongName t a =
            { Track = t
              Artist = a }

        Option.map2 toSongName title author

[<RequireQualifiedAccess>]
module GoggleMusic =
    open HtmlAgilityWrappers
    open LyricsBot.Model

    let trim (text: String) = text.Trim()

    let removeParentheses (text: String) =
        let openParIndex = text.IndexOf("(")
        let closeParIndex = text.IndexOf(")") + 1

        if openParIndex > 0 && closeParIndex > 0
        then text.Substring(0, openParIndex) + text.Substring(closeParIndex, text.Length - closeParIndex)
        else text

    let songName name artist =
        { Artist = artist |> trim
          Track = name |> trim }

    let extractSongName metaDoc =
        metaDoc
        |> extractFirstNode Selectors.GM.metaTitleSelector
        |> Option.bind (extractAttr "content")
        |> Option.map (removeParentheses)
        |> Option.map (fun s -> s.Split([| " - " |], 2, StringSplitOptions.RemoveEmptyEntries))
        |> Option.bind (function
            | [| name; artist |] -> songName name artist |> Some
            | _ -> None)

// TODO
[<RequireQualifiedAccess>]
module Itunes =
    open HtmlAgilityWrappers

    let extractTrack itunesPageDoc =
        itunesPageDoc
        |> extractFirstNode Selectors.Itunes.trackNameSelector
        |> Option.map (fun node -> node.InnerText)

    let extractArtist itunesPageDoc =
        itunesPageDoc
        |> extractFirstNode Selectors.Itunes.artistNameSelector
        |> Option.map (fun node -> node.InnerText)
