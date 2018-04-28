module LyricsBot.TestFunctions

open LyricsBot.Core
open LyricsBot.Grabbers.GoggleMusic
open LyricsBot.Grabbers.AZLyrics
open LyricsBot.Model
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open System

[<FunctionName("TestGMLink")>]
let run 
  ([<HttpTrigger(AuthorizationLevel.Function, "post")>] req: string,
   [<Queue("gm-link-requests")>] gmLinkRequests: ICollector<Int64 * Uri>) = 
  
  parseMessage req |> function
    | Some s -> s |> function
      | GMLink link -> 
        gmLinkRequests.Add (Int64.MaxValue, link)
        getLyrics link 
          |> Option.map(function | Lyrics (_, l) -> l | SongName _ -> "not found") 
          |> Option.defaultValue "no found" 
        | _ -> "not found"
    | _ -> "not found"

[<FunctionName("TestSearch")>]
let run'
  ([<HttpTrigger(AuthorizationLevel.Function, "post")>] req: string) = 
  
  searchLyrics req |> function
  | Some l -> l
  | None -> "not found"