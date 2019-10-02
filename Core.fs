module LyricsBot.Core

open Model

let printResponseLog = function
| HelpDoc -> "Help doc requested."
| LyricsFound (song, _) -> sprintf "Lyrics found for song: %s" song.Pretty
| LyricsNotFound -> "Lyrics not found."  

let printLinkProcessingResultLog = function
| SearchQuery q -> sprintf "Search query added: %s" q
| Response r -> printResponseLog r

let printResponse = function
| HelpDoc -> "Now type song name or share link from your music app (only Google Music allowed at the moment)."
| LyricsFound (song, lyrics) -> sprintf "%s \n %s" song.Pretty (lyrics.Trim('\n'))
| LyricsNotFound -> "Lyrics not found."