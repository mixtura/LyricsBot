module LyricsBot.Grabbers

open System
open Utils
open LyricsBot.Model

module private HtmlAgilityWrappers = 
  open HtmlAgilityPack

  let loadDoc (url: Uri) = try (new HtmlWeb()).Load(url) |> Some with | _ -> None

  let extractFirstNode (selector: string) (doc: HtmlDocument) =       
    try doc.DocumentNode.SelectSingleNode(selector) |> Option.ofObj with | _ -> None

  let extractAllNodes (selector: string) (doc: HtmlDocument) =
    try doc.DocumentNode.SelectNodes(selector) |> Option.ofObj with | _ -> None 

  let extractAttr attrName (node: HtmlNode) =
    node.GetAttributeValue(attrName, "")
    |> Some
    |> Option.bind (function | "" -> None | x -> Some x)

module AZLyrics =
  open HtmlAgilityWrappers
  
  let searchLyrics query =
    let searchLyricsLink = sprintf "https://search.azlyrics.com/search.php?q=%s&w=songs" query
    
    // auto-generated with Chrome Developer Tools
    let lyricsSelector = "/html/body/div[3]/div/div[2]/div[5]"
    let lyricsFirstSearchResultSelector = "/html/body/div[2]/div/div/div/table/tr[2]/td/a"
    
    searchLyricsLink
    |> createUri 
    |> Option.bind loadDoc 
    |> Option.bind (extractFirstNode lyricsFirstSearchResultSelector)
    |> Option.bind (extractAttr "href")
    |> Option.bind createUri
    |> Option.bind loadDoc
    |> Option.bind (extractFirstNode lyricsSelector)
    |> Option.map (fun node -> node.InnerText)

module WikiaLyrics =
  open HtmlAgilityWrappers
  
  let getLyrics song = 

    let getLyricsLink {Artist = artist; Track = track} = 
        sprintf "http://lyrics.wikia.com/wiki/%s:%s" artist track 

    let lyricsSelector = "//*[@id='mw-content-text']/div[7]"
    
    getLyricsLink song 
    |> createUri 
    |> Option.bind loadDoc 
    |> Option.bind (extractFirstNode lyricsSelector) 
    |> Option.map (fun node -> node.InnerText)

module GoggleMusic = 
  open HtmlAgilityWrappers

  type GMResult = 
    | Lyrics of song: SongName * string
    | SongName of song: SongName
  
  let extractSongName googlePlayLink = 
    extractQueryValueFromUri "t" googlePlayLink
  
  let getLyrics gmLink =

    // Auto-generated with Chrome Developer Tools
    let lyricsParagraphsSelector = "//*[@id='main-content-container']/div/p"
    let trackNameSelector = "//*[@id='main-content-container']/div[1]/div/div/div[1]/a"
    let artistNameSelector = "//*[@id='main-content-container']/div[1]/div/div/div[2]/a"
    let redirectLinkSelector = "/html/body/a"

    let doc = 
      gmLink 
      |> loadDoc
      |> Option.bind (extractFirstNode redirectLinkSelector)
      |> Option.bind (extractAttr "href")
      |> Option.bind ( (+) "https://play.google.com"  >> createUri)
      |> Option.bind loadDoc
    
    let lyrics = 
      doc
      |> Option.bind(extractAllNodes lyricsParagraphsSelector)
      |> Option.map (List.ofSeq >> List.map (fun node -> node.InnerHtml.Replace("<br>", "\n")) >> String.concat "\n")

    let song = 
      let track =
        doc
        |> Option.bind(extractFirstNode trackNameSelector)
        |> Option.map (fun node -> node.InnerText)
    
      let artist =
        doc 
        |> Option.bind(extractFirstNode artistNameSelector)
        |> Option.map (fun node -> node.InnerText)
      
      Option.map2 
        (fun track artist -> {Track = track; Artist = artist}) 
        track artist

    (lyrics, song)
    ||> Option.map2 (fun song lyrics -> Lyrics (lyrics, song))
    |> Option.orElse (Option.map SongName song)