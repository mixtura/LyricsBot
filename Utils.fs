namespace LyricsBot

module Utils = 
  open System
  
  let rec firstSome fs = 
    match fs with
    | x::xs -> Option.orElseWith (fun _ -> firstSome xs) (x())
    | [] -> None

  let tryToOption result = 
    match result with 
    | (true, uri) -> Some uri
    | (false, _) -> None

  let createUri str = Uri.TryCreate(str, UriKind.Absolute) |> tryToOption

  let optionToResult error op = 
    match op with
    | Some(value) -> Ok value  
    | None -> Error error

  let map2 mapping first last =
    match first with
    | Ok(value1) -> 
      match last with
      | Ok(value2) -> mapping value1 value2 |> Ok
      | Error msg  -> Error msg 
    | Error msg  -> Error msg  

  let getError resList =
    let rec getError' resList previousErr = 
      match resList with
      | [] -> previousErr
      | x::xs -> 
        match x with 
        | Result.Error err -> getError' xs (sprintf "%s,%s" err previousErr) 
        | Ok _ -> getError' xs previousErr

    getError' resList ""

  let extractQueryValueFromUri key (uri : Uri) = 
    uri.Query.TrimStart('?').Split [|'&'; '='|] |>
    Array.chunkBySize 2 |>
    Array.tryFind (fun pair -> pair.[0] = key) |>
    Option.bind (fun pair -> Some pair.[1])