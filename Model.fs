namespace LyricsBot

module Model =
  type Song = {
    Artist: string;
    Track: string;
  }

  type Request = 
    | GetLyrics of Song 
    | SearchLyrics of string 