module LyricsBot.Core

open Model

let renderResponseLog =
    function
    | HelpDoc -> "Help doc requested."
    | LyricsFound(song, _) -> sprintf "Lyrics found for song: %s" song.Pretty
    | LyricsNotFound -> "Lyrics not found."

let renderResponse =
    function
    | HelpDoc ->
        "Now type song name or share link from your music app "
        + "(only Google Music and Spotify allowed at the moment)."
    | LyricsFound(song, lyrics) ->
        sprintf "%s \n\n%s" song.Pretty (lyrics.Trim('\n'))
    | LyricsNotFound -> "Lyrics not found."
