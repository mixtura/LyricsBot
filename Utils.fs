namespace LyricsBot

module Utils = 
  open Newtonsoft.Json
  open System
  
  let rec firstSome fs = 
    match fs with
    | x::xs -> Option.orElseWith (fun _ -> firstSome xs) (x())
    | [] -> None

  let tryToOption result = 
    match result with 
    | (true, uri) -> Some uri
    | (false, _) -> None

  let creatUri str = Uri.TryCreate(str, UriKind.Absolute) |> tryToOption

  let extractQueryValueFromUri key (uri : Uri) = 
    uri.Query.Split [|'&'; '='|] |>
    Array.chunkBySize 2 |>
    Array.tryFind (fun pair -> pair.[0] = key) |>
    Option.bind (fun pair -> Some pair.[1])
  
  let serializeObj obj = JsonConvert.SerializeObject obj  
  let deserializeObj<'a> str = JsonConvert.DeserializeObject<'a> str