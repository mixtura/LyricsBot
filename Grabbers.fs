module LyricsBot.Grabbers

open System
open System.Web
open Utils

module HtmlAgilityWrappers =
  open HtmlAgilityPack

  let loadDoc (url: Uri) = (HtmlWeb()).Load(url)

  let extractFirstNode (selector: string) (doc: HtmlDocument) =       
    doc.DocumentNode.SelectSingleNode(selector) |> Option.ofObj

  let extractAllNodes (selector: string) (doc: HtmlDocument) =
    doc.DocumentNode.SelectNodes(selector) |> Option.ofObj 

  let extractAttr attrName (node: HtmlNode) =
    node.GetAttributeValue(attrName, "")
    |> Some
    |> Option.bind (function 
      | "" -> None 
      | x -> HttpUtility.HtmlDecode(x) |> Some)

  let extractText (node: HtmlNode) =
    node.InnerText |> HttpUtility.HtmlDecode

// Auto-generated xpath selectors
module private Selectors = 
  module AZ =
    let lyricsSelector = "/html/body/div[3]/div/div[2]/div[5]"
    let lyricsSearchResultSelector = "//*[@class='text-left visitedlyr']/a"
    let songNameSelector = "/html/body/div[3]/div/div[2]/b"
    let artistNameSelector = "/html/body/div[3]/div/div[2]/div[3]/h2/b"

  module GM =
    let metaTitleSelector = "/html/head/meta[3]"

  module Itunes =    
    let trackNameSelector = "//*[@class='table__row popularity-star we-selectable-item is-active is-available we-selectable-item--allows-interaction ember-view']/td[2]/div/div/div";
    let artistNameSelector = "//*[@class='product-header__identity']/a";

[<RequireQualifiedAccess>]
module AZLyrics =
  open HtmlAgilityWrappers

  let createSearchLyricsUrl query = 
    let cleanQuery (query : String) = 
      match query.IndexOf("(") with
      | index when index > 0 -> query.Substring(0, index)
      | _ -> query

    cleanQuery query 
    |> HttpUtility.UrlEncode
    |> sprintf "https://search.azlyrics.com/search.php?q=%s&w=songs" 
    |> createUri

  let getFirstSearchResultLink lyricsPageDoc = 
    lyricsPageDoc
    |> extractFirstNode Selectors.AZ.lyricsSearchResultSelector 
    |> Option.bind (extractAttr "href" )
    |> Option.bind (createUri)

  let extractArtist lyricsPageDoc =
    lyricsPageDoc 
    |> extractFirstNode Selectors.AZ.artistNameSelector
    |> Option.map(extractText)
    |> Option.map(fun artist -> artist.Replace("Lyrics", ""))

  let extractTrack lyricsPageDoc =        
    lyricsPageDoc 
    |> extractFirstNode Selectors.AZ.songNameSelector 
    |> Option.map(extractText)
    |> Option.map(fun track -> track.Trim('"'))
    
  let extractLyrics lyricsPageDoc =
    lyricsPageDoc
    |> extractFirstNode Selectors.AZ.lyricsSelector 
    |> Option.map(extractText)

[<RequireQualifiedAccess>]
module GoggleMusic = 
  open HtmlAgilityWrappers
 
  let extractSongTitle metaDoc = 
    metaDoc 
    |> extractFirstNode Selectors.GM.metaTitleSelector 
    |> Option.bind(extractAttr "content")

// TODO
[<RequireQualifiedAccess>]
module Itunes = 
  open HtmlAgilityWrappers

  let extractTrack itunesPageDoc = 
    itunesPageDoc 
    |> extractFirstNode Selectors.Itunes.trackNameSelector 
    |> Option.map(fun node -> node.InnerText)
  
  let extractArtist itunesPageDoc = 
    itunesPageDoc 
    |> extractFirstNode  Selectors.Itunes.artistNameSelector 
    |> Option.map(fun node -> node.InnerText)