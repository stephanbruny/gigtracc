namespace Gigtracc.Events

open Gigtracc.Messaging

open System
open System.IO
open System.Text
open FSharp.Json
open System.Reflection
open Microsoft.FSharp.Reflection.FSharpReflectionExtensions
open Gigtracc.Messaging

module EventStream =

    type EventItem = {
        streamVersion : int;
        id : string;
        name : string;
        date : DateTime;
        content : string;
        meta : Map<string, obj> option;
    }

    type StreamSource = {
        name : string;
        mutable items : EventItem list;
        streamActor : Actor<EventItem>;
    }

    type EventReplay<'T> =
    | Created of 'T
    | Modified of string * string
    | Removed of string

    type Snapshop<'T> = {
        streamSourceName : string;
        streamSourceVersion : int;
        content : 'T list;
    }

    let createEmptyStream name =
        {
            name = name;
            items = [];
            streamActor = new Actor<EventItem>()
        }

    let getStreamVersion stream = stream.items.Length

    let addEventItem stream name meta data =
        let item =
            {
                streamVersion = getStreamVersion stream;
                id = System.Guid.NewGuid().ToString();
                name = name;
                date = DateTime.Now;
                content = data;
                meta = meta;
            }
        stream.items <- stream.items |> List.append [item]
        stream.streamActor.Send item

    let getAllEvents stream name version =
        stream.items
        |> List.filter(fun i -> i.name = name && i.streamVersion >= version)
        |> List.sortBy (fun { streamVersion = ver } -> ver)

    let getEventContents<'T> stream name version =
        getAllEvents stream name version |> List.map(fun ev -> Json.deserialize<'T> ev.content)

    let saveStreamOnChange stream filePath =
        stream.streamActor.AddAction(fun _ ->
            let jsonData = stream.items |> Json.serialize
            File.WriteAllText(filePath, jsonData, Encoding.UTF8)
        )

    let createStreamFromJson streamName fileData =
        let entries = Json.deserialize<EventItem list> fileData
        {
            name = streamName;
            items = entries;
            streamActor = new Actor<EventItem>()
        }

    let snapshopFrom<'T> (stream : StreamSource) from =
        let content = stream.items |> List.skip from |> List.map(fun item -> item.content |> Json.deserialize<'T>)
        let version = getStreamVersion stream
        {
            streamSourceName = stream.name;
            streamSourceVersion = version;
            content = content;
        }

    let getPropertyValue<'T> (instance : 'T) (propertyName : string) =
        let instanceType = instance.GetType()
        let propInfo = instanceType.GetProperty(propertyName)
        propInfo.GetValue(instance)

    let replay<'T> (stream : StreamSource) (idPropertyName : string) modifyFn name from =
        let jsonConf = JsonConfig.create(allowUntyped = true)
        let events = getAllEvents stream name from
        let eventContents = events |> List.map (fun ev -> ev.content |> Json.deserializeEx<EventReplay<'T>> jsonConf)
        let contents = eventContents |> List.map (function Created item -> Some item | _ -> None) |> List.choose id
        contents |> List.map (fun item ->
            let itemId = (getPropertyValue<'T> item idPropertyName) :?> string
            let maybeDeleted = eventContents |> List.tryFind(function Removed id when id = itemId -> true | _ -> false)
            if (maybeDeleted.IsSome) then
                None
            else
                Some (itemId, item)
        )
        |> List.choose id
        |> List.map (fun (itemId, item) ->
            let updates =
                eventContents
                |> List.filter(function Modified (id, _) when id = itemId -> true | _ -> false)
                |> List.map(function Modified (_, data) -> Some data | _ -> None)
                |> List.choose id
            updates |> List.fold(modifyFn) item
        )
