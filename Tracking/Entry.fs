namespace Gigtracc.Tracking

open System
open Gigtracc.Events.EventStream

open FSharp.Json

module Entry =

    type Entry = {
        id : string;
        date : DateTime;
        duration : float;
        description : string;
        location : string;
    }

    type EntryCommand =
    | Add of Entry
    | ChangeDescription of string * string
    | ChangeDuration of string * float
    | ChangeLocation of string * string
    | Remove of string

    type CreateEntryCommand =
        {
            location : string;
            description : string;
            duration : float;
            date : DateTime;
        }

    let createEntry date (duration : TimeSpan) description location =
        {
            id = System.Guid.NewGuid().ToString();
            date = date;
            duration = duration.TotalHours;
            description = description;
            location = location;
        }

    let createFromCommand (command : CreateEntryCommand) =
        {
            id = System.Guid.NewGuid().ToString();
            date = command.date;
            duration = command.duration;
            description = command.description;
            location = command.location;
        }

    let modifyEntryDescription (entry : Entry) (description : string) = { entry with description = description }
    let modifyEntryDuration (entry : Entry) (duration : float) = { entry with duration = duration }
    let modifyEntryLocation (entry : Entry) location = { entry with location = location }

    // TODO: supply serialization function externally
    let replayEntries version entryEvents =
        let events = getAllEvents entryEvents "entry" version |> List.map(fun ev -> Json.deserialize<EntryCommand> ev.content)
        let created =
            events
            |> List.filter(fun ev -> match ev with | EntryCommand.Add _ -> true | _ -> false)
            |> List.map(fun e -> match e with | EntryCommand.Add data -> Some data | _ -> None)
            |> List.choose id

        let updates =
            events
            |> List.filter (fun ev ->
                match ev with
                | EntryCommand.ChangeDescription  _
                | EntryCommand.ChangeDuration  _
                | EntryCommand.Remove _
                | EntryCommand.ChangeLocation _ -> true
                | _ -> false)

        created
        |> List.filter(fun e ->
            let removed =
                updates |> List.tryFind(fun up -> match up with EntryCommand.Remove id when id = e.id -> true | _ -> false)
            match removed with
            | Some _ -> false
            | _ -> true
        )
        |> List.map(fun e ->
            let mutable result = e
            updates |> List.iter(fun up ->
                match up with
                | EntryCommand.ChangeDescription (id, text) when id = e.id ->
                    result <- modifyEntryDescription result text
                | EntryCommand.ChangeDuration (id, dur) when id = e.id ->
                    result <- modifyEntryDuration result dur
                | EntryCommand.ChangeLocation (id, loc) when id = e.id ->
                    result <- modifyEntryLocation result loc
                | _ -> ()
            )
            result
        )