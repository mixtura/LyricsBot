module LyricsBot.MouritsApi

open FSharp.Data
open Model
open System
open Microsoft.Extensions.Configuration;

type MouritsApiProvider = JsonProvider<"Data/mouritsApiResponseSample.json"> 

let apiKey = 
  let config = 
    (ConfigurationBuilder())
      .AddEnvironmentVariables()
      .Build()

  config.["MouritsApiKey"]

let makeRequest apiKey query =
  let headers = [
    "x-rapidapi-host", "mourits-lyrics.p.rapidapi.com"; 
    "x-rapidapi-key", apiKey]

  Http.AsyncRequestString("https://mourits-lyrics.p.rapidapi.com", query, headers) 
  |> Async.RunSynchronously 
  |> MouritsApiProvider.Parse

let getLyrics api {Artist = artist; Track = track} =
  ["a", artist; "s", track] |> api

let searchLyrics api query = api ["q", query]