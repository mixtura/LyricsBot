module LyricsBot.Core

open Model

let private songNameAsString {Artist = artist; Track = track} = 
  sprintf "%s - %s" artist track

let printResponseLog response =
    match response with
    | HelpDoc -> "Help doc requested."
    | LyricsFound (song, _) ->
      sprintf "Lyrics found for song: %s" (songNameAsString song)
    | LyricsNotFound -> "Lyrics not found."  

let printLinkProcessingResultLog result =
  match result with
  | SearchQuery q -> sprintf "Search query added: %s" q
  | Response r -> printResponseLog r

let printResponse response =
  match response with
  | HelpDoc -> "Now type song name or share link from your music app (only Google Music allowed at the moment)."
  | LyricsFound (song, lyrics) ->     
    sprintf "%s \n\n %s" (songNameAsString song) lyrics
  | LyricsNotFound -> "Lyrics not found."