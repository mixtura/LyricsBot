module LyricsBot.HtmlAgilityWrappers

open System
open HtmlAgilityPack

let loadDoc (url: Uri) = 
  try (new HtmlWeb()).Load(url) |> Ok 
  with | _ -> sprintf "Can't load document by url %s" url.AbsoluteUri |> Error

let extractFirstNode (selector: string) (doc: HtmlDocument) =       
  doc.DocumentNode.SelectSingleNode(selector) |> Option.ofObj

let extractAllNodes (selector: string) (doc: HtmlDocument) =
  doc.DocumentNode.SelectNodes(selector) |> Option.ofObj 

let extractAttr attrName (node: HtmlNode) =
  node.GetAttributeValue(attrName, "")
  |> Some
  |> Option.bind (function | "" -> None | x -> Some x)