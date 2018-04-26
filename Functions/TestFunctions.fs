module LyricsBot.TestFunctions

open LyricsBot.Core
open LyricsBot.Grabbers.GoggleMusic
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open LyricsBot.Model

[<FunctionName("TestGetLyrics")>]
let run ([<HttpTrigger(AuthorizationLevel.Function, "post")>] req: string) = 
    parseMessage req |> function
    | Some s -> s |> function
        | GMLink link -> 
            getLyrics link 
            |> Option.map(function | Lyrics (_, l) -> l | SongName _ -> "not found") 
            |> Option.defaultValue "no found" 
        | _ -> "not found"
    | _ -> "not found"