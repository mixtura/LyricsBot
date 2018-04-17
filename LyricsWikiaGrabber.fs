namespace LyricsBot

open FSharp.Data

module LyricsWikiaGrabber =    
    type private LyricsWikiaProvider = HtmlProvider<"http://lyrics.wikia.com/Green Day:Holiday">
    let findLyrics artist song = 

        let getLyricsLink artist song = sprintf "http://lyrics.wikia.com/wiki/%s:%s" artist song
    
        let loadLyricsPage artist song = 
            getLyricsLink artist song |>
            LyricsWikiaProvider.AsyncLoad

        let extractLyrics (page:LyricsWikiaProvider) = 
            page.Html.CssSelect ".lyricbox" |>
            List.tryHead |> 
            Option.bind (fun node -> Some <| node.InnerText())
    
        async {
            let! page = loadLyricsPage artist song
            return extractLyrics page
        }
