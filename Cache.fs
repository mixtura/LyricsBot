module LyricsBot.Cache

let getCache name =
  let cache key = None 
  let cacheProxy key f = 
    match cache key with
    | Some v -> v
    | None -> f key

  cacheProxy