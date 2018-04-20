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

    type ModifyEntryCommand =
    | ChangeDescription of string * string
    | ChangeDuration of string * float
    | ChangeLocation of string * string

    type CreateEntryCommand =
        {
            projectId : string;
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

    let modifyEntry (entry : Entry) up =
        match up with
        | ModifyEntryCommand.ChangeDescription (id, text) when id = entry.id ->
            modifyEntryDescription entry text
        | ModifyEntryCommand.ChangeDuration (id, dur) when id = entry.id ->
            modifyEntryDuration entry dur
        | ModifyEntryCommand.ChangeLocation (id, loc) when id = entry.id ->
            modifyEntryLocation entry loc
        | _ -> entry

    let getEntriesBetween (startDate, endDate) (entries : Entry list) =
        entries |> List.filter(fun e -> e.date >= startDate && e.date <= endDate)