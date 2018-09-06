module LyricsBot.Model

open System

type SongName = { Artist:string; Track:string }

type Response = 
  | HelpDoc
  | LyricsFound of SongName * string
  | LyricsNotFound

type ParsedMessage = 
  | Start
  | GMLink of Uri
  | ItunesLink of Uri 
  | SearchLyricsQuery of string

let toQuery {Artist = artist; Track = track} =
  let cleanName (name : String) = 
    match name.IndexOf("(") with
    | index when index > 0 -> name.Substring(0, index)
    | _ -> name

  List.map cleanName [artist; track] |> String.concat " "