module LyricsBot.Grabbers

  open FSharp.Data
  open Model
  
  type private LyricsWikiaProvider = HtmlProvider<"http://lyrics.wikia.com/Green Day:Holiday">
  
  let getLyrics song = 

    let getLyricsLink {Artist = artist; Track = track} = sprintf "http://lyrics.wikia.com/wiki/%s:%s" artist track
    
    let loadLyricsPage song = 
        getLyricsLink song |>
        LyricsWikiaProvider.AsyncLoad

    let extractLyrics (page:LyricsWikiaProvider) = 
        page.Html.CssSelect ".lyricbox" |>
        List.tryHead |> 
        Option.bind (fun node -> Some <| node.InnerText())

    async {
        let! page = loadLyricsPage song
        return extractLyrics page
    }