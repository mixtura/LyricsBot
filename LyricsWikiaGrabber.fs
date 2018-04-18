namespace LyricsBot

open FSharp.Data

module LyricsWikiaGrabber =    
    type private LyricsWikiaProvider = HtmlProvider<"http://lyrics.wikia.com/Green Day:Holiday">
    let findLyrics artist song = 

        let getLyricsLink artist track = sprintf "http://lyrics.wikia.com/wiki/%s:%s" artist track
    
        let loadLyricsPage artist track = 
            getLyricsLink artist track |>
            LyricsWikiaProvider.AsyncLoad

        let extractLyrics (page:LyricsWikiaProvider) = 
            page.Html.CssSelect ".lyricbox" |>
            List.tryHead |> 
            Option.bind (fun node -> Some <| node.InnerText())
    
        async {
            let! page = loadLyricsPage artist song
            return extractLyrics page
        }
