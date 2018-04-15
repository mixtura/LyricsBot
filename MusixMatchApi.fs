namespace LyricsBot

open FSharp.Data

[<RequireQualifiedAccess>]
module MusixMatchApi =
  let apiEndPoint = "http://api.musixmatch.com/ws/1.1/" 
  let getLyrics apiKey (track, artist) = 
    sprintf "%smatcher.lyrics.get?apikey=%s&q_track=%s&q_artist=%s" apiEndPoint apiKey track artist
  let searchLyrics apiKey query = 
    sprintf "%s"

  type MusixMatchLyricsProvider = JsonProvider<"./apiSamples/api.musixmatch.com.sample.json">