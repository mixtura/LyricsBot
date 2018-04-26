module LyricsBot.Model
open System

type SongName = { Artist:string; Track:string }

type ParsedMessage = 
  | GMLink of Uri
  | ItunesLink of Uri 
  | SearchLyricsQuery of string