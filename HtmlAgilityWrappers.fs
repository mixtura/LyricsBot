module LyricsBot.HtmlAgilityWrappers

open System
open HtmlAgilityPack

let loadDoc (url: Uri) = 
  try (new HtmlWeb()).Load(url) |> Ok 
  with | _ -> sprintf "Can't load document by url %s" url.AbsoluteUri |> Error

let extractFirstNode (selector: string) (doc: HtmlDocument) =       
  try doc.DocumentNode.SelectSingleNode(selector) |> Option.ofObj with | _ -> None

let extractAllNodes (selector: string) (doc: HtmlDocument) =
  try doc.DocumentNode.SelectNodes(selector) |> Option.ofObj with | _ -> None 

let extractAttr attrName (node: HtmlNode) =
  node.GetAttributeValue(attrName, "")
  |> Some
  |> Option.bind (function | "" -> None | x -> Some x)