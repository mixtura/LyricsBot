module LyricsBot.CanaradoApi

open FSharp.Data
open Model
open System
open Microsoft.Extensions.Configuration

type CanaradoApiProvider = JsonProvider<"Data/canaradoApiResponseSample.json">

let apiKey =
    let config = (ConfigurationBuilder()).AddEnvironmentVariables().Build()

    config.["CanaradoApiKey"]

let makeRequest apiKey path =
    let headers =
        [ "x-rapidapi-host", "canarado-lyrics.p.rapidapi.com"
          "x-rapidapi-key", apiKey ]

    Http.AsyncRequestString("https://canarado-lyrics.p.rapidapi.com/" + path, List.empty, headers, "GET")
    |> Async.RunSynchronously
    |> CanaradoApiProvider.Parse

let searchLyrics api query = api ("lyrics/" + query)
