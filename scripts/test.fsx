#r "../bin/Debug/netstandard2.0/bin/LyricsBot.dll"
#r "./lib/FSharp.Data.DesignTime.dll"
#r "./lib/FSharp.Data.dll"

open LyricsBot.LyricsWikiaGrabber
open FSharp.Data

type private LyricsWikiaProvider = HtmlProvider<"http://lyrics.wikia.com/Green Day:Holiday">
let getLyricsLink artist song = sprintf "http://lyrics.wikia.com/wiki/%s:%s" artist song
let loadLyricsPage artist song = 
    getLyricsLink artist song |>
    LyricsWikiaProvider.AsyncLoad

// loadLyricsPage "Green day" "Holiday" |> Async.RunSynchronously |> fun x -> x.Html.ToString() |> printf "%s" 

//findLyrics "Green day" "Holiday" |> Async.RunSynchronously |> function
  //  | Some s -> s |> printf "%s"
    //| None -> printf "error"

let fun1 : Option<string> = printf " fun1"; None
let fun2 = printf " fun2"; Some " fun2res"

Option.orElse fun1 fun2