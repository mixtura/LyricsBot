module LyricsBot.Model

open System

type SongName = 
  { Artist:string; Track:string }
  
  member x.AsQuery =
    let cleanName (name : String) = 
      match name.IndexOf("(") with
      | index when index > 0 -> name.Substring(0, index)
      | _ -> name

    List.map cleanName [x.Artist; x.Track] |> String.concat " "

type Response = 
  | HelpDoc
  | LyricsFound of name: SongName * content: string
  | LyricsNotFound

type Message = 
  | Start
  | GMLink of Uri
  | ItunesLink of Uri 
  | SearchLyricsQuery of string

type LinkProcessingResult =
  | Response of Response
  | SearchQuery of string