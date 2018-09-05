module LyricsBot.Grabbers

open Utils
open HtmlAgilityPack

// Auto-generated xpath selectors
module private Selectors = 
  module AZ =
    let lyricsSelector = "/html/body/div[3]/div/div[2]/div[5]"
    let lyricsSearchResultSelector = "//*[@class='text-left visitedlyr']/a"
    let songNameSelector = "/html/body/div[3]/div/div[2]/b"
    let artistNameSelector = "/html/body/div[3]/div/div[2]/div[3]/h2/b"

  module GM =
    let lyricsParagraphsSelector = "//*[@id='main-content-container']/div/p"
    let trackNameSelector = "//*[@id='main-content-container']/div[1]/div/div/div[1]/a"
    let artistNameSelector = "//*[@id='main-content-container']/div[1]/div/div/div[2]/a"
    let redirectLinkSelector = "/html/body/a"

  module Itunes =    
    let trackNameSelector = "//*[@class='table__row popularity-star we-selectable-item is-active is-available we-selectable-item--allows-interaction ember-view']/td[2]/div/div/div";
    let artistNameSelector = "//*[@class='product-header__identity']/a";

module AZLyrics =
  open HtmlAgilityWrappers

  let createSearchLyricsUrl = 
    sprintf "https://search.azlyrics.com/search.php?q=%s&w=songs"
    >> createUri
    >> optionToResult "Can't create url for azlyrics."
  
  let getFirstSearchResultLink lyricsPageDoc = 
    lyricsPageDoc
    |> extractFirstNode Selectors.AZ.lyricsSearchResultSelector 
    |> optionToResult "Can't extract search result from azlyrics search page."
    |> Result.bind (extractAttr "href" >> optionToResult "Can't extract href attribute from azlyrics search result.")
    |> Result.bind (createUri >> optionToResult "Can't create link to azlyrics lyrics page.")

  let extractArtist lyricsPageDoc =
    lyricsPageDoc 
    |> extractFirstNode Selectors.AZ.artistNameSelector 
    |> optionToResult "Can't extract artist name from azlyrics lyrics page."
    |> Result.map (fun n -> n.InnerText)

  let extractTrack lyricsPageDoc =        
    lyricsPageDoc 
    |> extractFirstNode Selectors.AZ.songNameSelector 
    |> optionToResult "Can't extract track name from azlyrics lyrics page."
    |> Result.map (fun n -> n.InnerText.Replace("Lyrics", "").Trim('"'))

  let extractLyrics lyricsPageDoc=
    lyricsPageDoc
    |> extractFirstNode Selectors.AZ.lyricsSelector 
    |> optionToResult "Can't extract lyrics from azlyrics lyrics page."
    |> Result.map (fun node -> node.InnerText)

module GoggleMusic = 
  open HtmlAgilityWrappers
 
  let extractSongName googlePlayLink = 
    extractQueryValueFromUri "t" googlePlayLink
  
  let getRedirectLink doc = 
    doc
    |> extractFirstNode Selectors.GM.redirectLinkSelector 
    |> optionToResult "Can't extract redirect link from GM lyrics page."
    |> Result.bind (extractAttr "href" >> optionToResult "Can't extract href attribute from GM redirect link.")
    |> Result.bind ( (+) "https://play.google.com"  >> createUri >> optionToResult "Can't create GM link.")

  let extractLyricsText = 
    List.ofSeq
    >> List.map (fun (node: HtmlNode) -> node.InnerHtml )
    >> List.map (fun p -> p.Replace("<br>", "\n") |> HtmlEntity.DeEntitize) 
    >> String.concat "\n"

  let extractLyrics lyricsPageDoc = 
    lyricsPageDoc
    |> extractAllNodes Selectors.GM.lyricsParagraphsSelector 
    |> optionToResult "Can't extract lyrics from GM page."
    |> Result.map (extractLyricsText)

  let extractTrack lyricsPageDoc =
    lyricsPageDoc
    |> extractFirstNode Selectors.GM.trackNameSelector 
    |> optionToResult "Can't extract track name from GM page."
    |> Result.map (fun node -> node.InnerText)

  let extractArtist lyricsPageDoc =
    lyricsPageDoc 
    |> extractFirstNode Selectors.GM.artistNameSelector 
    |> optionToResult "Can't extract artist name from GM page."
    |> Result.map (fun node -> node.InnerText)

module Itunes = 
  open HtmlAgilityWrappers

  let extractTrack itunesPageDoc = 
    itunesPageDoc 
    |> extractFirstNode Selectors.Itunes.trackNameSelector 
    |> optionToResult "Can't extract track from Itunes song page." 
    |> Result.map(fun node -> node.InnerText)
  
  let extractArtist itunesPageDoc = 
    itunesPageDoc 
    |> extractFirstNode  Selectors.Itunes.artistNameSelector 
    |> optionToResult "Can't extract artist from Itunes song page." 
    |> Result.map(fun node -> node.InnerText)