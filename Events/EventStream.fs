namespace Gigtracc.Events

open Gigtracc.Messaging

open System

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
