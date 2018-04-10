// Learn more about F# at http://fsharp.org

open System
open System.Text
open System.IO

open Gigtracc
open Gigtracc.Messaging
open Gigtracc.Tracking.Entry
open Gigtracc.Tracking.Project
open Gigtracc.Tracking.User
open Gigtracc.Events.EventStream
open Gigtracc.Web
open Gigtracc.Web.App

open FSharp.Json
open Suave.Logging

[<EntryPoint>]
let main argv =

    let eventStreamFileName = "eventstream.json"

    let today = DateTime.Today

    let testProject =
        {
            id = System.Guid.NewGuid().ToString();
            name = "Testproject";
            client =
                {
                    name = "Testclient";
                    address = "";
                };
            startDate = DateTime.Now;
            endDate = (DateTime.Now.AddDays(1.0));
            entries = [];
            payment =
                {
                    currency = "EUR";
                    pricePerHour = 70.0;
                }
        }

    let testUser =
        {
            name = "Testuser";
            address = "";
            password = "test";
            email = "test@test.de";
            projects = [ testProject ];
            permission = All
        }

    let entryEvents : StreamSource = { name = "Entries"; items = []; streamActor = new Actor<EventItem>() }

    let testEntry = createEntry DateTime.Now (new TimeSpan(8, 0, 0)) "Some testing" "Localhost";

    let addEntryEvent ev (data : EntryCommand) =
        let entryData = Json.serialize data
        addEventItem entryEvents ev None entryData

    let eventFile =
        if File.Exists eventStreamFileName then
            File.ReadAllText eventStreamFileName |> Some
        else
            None

    entryEvents.streamActor.AddAction(fun ev  ->
        let data = Json.serialize entryEvents.items
        File.WriteAllText(eventStreamFileName, data, Encoding.UTF8);
    )

    match eventFile with
    | Some data ->
        entryEvents.items <- Json.deserialize<EventItem list> data
    | None ->
        [
            EntryCommand.Add testEntry;
            EntryCommand.Add (createEntry (DateTime.Now.AddDays(1.0)) (new TimeSpan(8, 0, 0)) "Some hacking" "Localhost");
            EntryCommand.Add (createEntry (DateTime.Now.AddDays(2.0)) (new TimeSpan(7, 0, 0)) "Some thinking" "Localhost");
            EntryCommand.Add (createEntry (DateTime.Now.AddDays(3.0)) (new TimeSpan(7, 30, 0)) "Some designing" "Localhost");
        ] |> List.iter(addEntryEvent "entry")

        addEntryEvent "entry" (EntryCommand.ChangeDescription (testEntry.id, "Do some real testing"))
        addEntryEvent "entry" (EntryCommand.ChangeLocation (testEntry.id, "At home"))

    testProject.entries <- replayEntries 0 entryEvents

    printfn "SORTED: %A" (getAllEvents entryEvents "entry" 0)

    printfn "E: %A" testProject.entries

    let total = calculatePrice testProject today (DateTime.Now.AddDays(10.0))
    printfn "Total: %.2f %s"  total testProject.payment.currency

    let log =
        testProject.entries
        |> List.sortBy(fun data -> data.date)
        |> List.map(fun data -> sprintf "%s: %s (%s)" (data.date.ToString()) data.description data.location)

    printfn "Log: %s" (log |> String.concat "\n")

    let serverJson = File.ReadAllText("server.json", Encoding.UTF8);
    let serverConf = Json.deserialize<Server.WebserverConfig>(serverJson)

    let getEntries startDate endDate =
        replayEntries 0 entryEvents
        |> List.filter(fun e -> e.date >= startDate && e.date <= endDate)
        |> Json.serialize

    let modifyDescription (id : string) (data : string) =
        let entry = testProject.entries |> List.tryFind(fun e -> e.id = (string id) )
        entry |> Option.map(fun _ ->
            addEntryEvent "entry" (EntryCommand.ChangeDescription ((string id), data))
            testProject.entries <- replayEntries 0 entryEvents
        ) |> ignore
        "ok"

    let modifyDuration (id : string) (data : string) =
        let duration = Single.Parse(data, Globalization.CultureInfo.InvariantCulture)
        let entry = testProject.entries |> List.tryFind(fun e -> e.id = (string id) )
        entry |> Option.map(fun _ ->
            addEntryEvent "entry" (EntryCommand.ChangeDuration ((string id), (float duration)))
            testProject.entries <- replayEntries 0 entryEvents
        ) |> ignore
        "ok"

    let modifyLocation (id : string) (data : string) =
        let entry = testProject.entries |> List.tryFind(fun e -> e.id = (string id) )
        entry |> Option.map(fun _ ->
            addEntryEvent "entry" (EntryCommand.ChangeLocation ((string id), data))
            testProject.entries <- replayEntries 0 entryEvents
        ) |> ignore
        "ok"

    let createEntryCommand (data : string) =
        let entryData = Json.deserialize<CreateEntryCommand> data
        let entry = createFromCommand entryData
        addEntryEvent "entry" (EntryCommand.Add entry)
        "ok"

    let deleteEntryCommand (id : string) (_ : string) =
        addEntryEvent "entry" (EntryCommand.Remove id)
        "ok"

    let routes =
        [
            ("/api/entries/%s/description", modifyDescription);
            ("/api/entries/%s/location", modifyLocation);
            ("/api/entries/%s/duration", modifyDuration);
        ]

    let createRoutes =
         [
             ("/api/entries", createEntryCommand)
         ]

    let deleteRoutes =
        [
            ("/api/entries/%s", deleteEntryCommand)
        ]

    Server.start serverConf getEntries routes createRoutes deleteRoutes
    0 // return an integer exit code
