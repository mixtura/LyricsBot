namespace LyricsBot

module Utils = 
  open System
  
  let rec firstSome fs = 
    match fs with
    | x::xs -> Option.orElseWith (fun _ -> firstSome xs) (x())
    | [] -> None

  let tryToOption = function 
    | (true, uri) -> Some uri
    | (false, _) -> None

  let tryToResult error = function
    | (true, uri) -> Ok uri
    | (false, _) -> Error error

  let createUri str = 
    Uri.TryCreate(str, UriKind.Absolute) 
    |> tryToResult (sprintf "Can't create url from string: %s" str)

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

  let aggregateErrors resList =
    let rec aggregateErrors' resList previousErr = 
      match resList with
      | [] -> previousErr
      | x::xs -> 
        match x with 
        | Result.Error err -> aggregateErrors' xs (sprintf "%s,%s" err previousErr) 
        | Ok _ -> aggregateErrors' xs previousErr

    aggregateErrors' resList ""

  let extractQueryValueFromUri key (uri : Uri) = 
    uri.Query.TrimStart('?').Split [|'&'; '='|] |>
    Array.chunkBySize 2 |>
    Array.tryFind (fun pair -> pair.[0] = key) |>
    Option.bind (fun pair -> Some pair.[1])