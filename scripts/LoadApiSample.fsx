#r "./lib/FSharp.Data.DesignTime.dll"
#r "./lib/FSharp.Data.dll"
#r "../bin/Debug/netstandard2.0/bin/LyricsBot.dll"

open System.IO
open System.Net
open System
open LyricsBot
open FSharp.Data

type Settings = JsonProvider<"../appSettings.json">
let apiKey = Settings.GetSample().ApiKeys.MusixMatch.ToString("N")
let lyricsSampleUrl = MusixMatchApi.getLyrics apiKey ("Holiday", "Scorpions")

let readStreamAsString (stream: Stream) = 
  let reader = new StreamReader(stream)
  reader.ReadToEnd()

new Uri(lyricsSampleUrl) 
    |> WebRequest.Create 
    |> (fun request -> request.GetResponse() :?> HttpWebResponse) 
    |> (fun response -> ("../apiSamples/api.musixmatch.com.sample.json", response.GetResponseStream() |> readStreamAsString) )
    |> File.WriteAllText