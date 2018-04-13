open System.IO
open System.Net
open System

let apiKey = "<api-key>"
let apiSampleRequest = "https://orion.apiseeds.com/api/music/lyric/Scorpions/Holiday?apikey=" + apiKey

let readStreamAsString (stream: Stream) = 
  let reader = new StreamReader(stream)
  reader.ReadToEnd()

new Uri(apiSampleRequest) 
    |> WebRequest.Create 
    |> (fun request -> request.GetResponse() :?> HttpWebResponse) 
    |> (fun response -> File.WriteAllText("./origion.apiseeds.com.sample.json", response.GetResponseStream() |> readStreamAsString) )