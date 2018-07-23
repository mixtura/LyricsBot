module LyricsBot.TestFunctions

// open LyricsBot.Core
// open LyricsBot.Model
// open Microsoft.Azure.WebJobs
// open Microsoft.Azure.WebJobs.Extensions.Http
// open System

// module TestGM =
//   open LyricsBot.Grabbers.GoggleMusic

//   [<FunctionName("TestGMLink")>]
//   let run 
//     ([<HttpTrigger(AuthorizationLevel.Function, "post")>] req: string,
//      [<Queue("gm-link-requests")>] gmLinkRequests: ICollector<Int64 * Uri>) = 
    
//     parseMessage req |> function
//       | Some s -> s |> function
//         | GMLink link -> 
//           gmLinkRequests.Add (Int64.MaxValue, link)
//           getLyrics link 
//             |> Result.map(function 
//               | Lyrics (song, l) -> LyricsFound(song, l) 
//               | SongName _ -> NotFound) 
//             |> function | Ok(value) -> value | _ -> NotFound 
//           | _ -> NotFound
//       | _ -> NotFound
//     |> printResponse 

// module TestAZLyrics = 
//   open LyricsBot.Grabbers.AZLyrics
  
//   [<FunctionName("TestSearch")>]
//   let run
//     ([<HttpTrigger(AuthorizationLevel.Function, "post")>] req: string) = 
    
//     searchLyrics req |> function
//     | Ok (Lyrics (song, lyrics)) -> LyricsFound (song, lyrics)
//     | _ -> NotFound
//     |> printResponse