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

open Suave
open Suave.Filters
open Suave.Operators

open FSharp.Json
open Suave.Logging

[<EntryPoint>]
let main argv =

    let eventStreamFileName = "app-data/testproject.json"

    let today = DateTime.Today

    let testProject =
        {
            id = "testproject";
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
            projects = [ testProject.id ];
            permission = All
        }

    let loadProject basePath fileExtension (id : string) =
        let filePath = Path.Combine( basePath, (id + fileExtension) )
        File.ReadAllText filePath

    let loadProjectLocal = loadProject "app-data" ".json"

    let loadUserProjectStreams (user : User) =
        user.projects |> List.map (fun id -> loadProjectLocal id |> createStreamFromJson id)

    let loadUserProjects (user : User) =
        let streams = loadUserProjectStreams user
        streams
        |> List.map(fun stream ->
            getEventContents<ProjectCommand> stream "project" 0 |> replayProjects
        )
        |> List.fold (fun acc i -> acc |> List.append i) []

    let eventFile =
        if File.Exists eventStreamFileName then
            File.ReadAllText eventStreamFileName |> Some
        else
            None

    let entryEvents =
        match eventFile with
        | Some data -> createStreamFromJson "Entries" data
        | _ -> { name = "Entries"; items = []; streamActor = new Actor<EventItem>() }

    let addEntryEvent ev (data : EntryCommand) =
        let entryData = Json.serialize data
        addEventItem entryEvents ev None entryData

    entryEvents.streamActor.AddAction(fun ev  ->
        let data = Json.serialize entryEvents.items
        File.WriteAllText(eventStreamFileName, data, Encoding.UTF8);
    )

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

    let getEntriesCsv startDate endDate =
        let csvHead = "date;location;duration;description"
        let cols =
            replayEntries 0 entryEvents
            |> List.filter(fun e -> e.date >= startDate && e.date <= endDate)
            |> List.map(fun e -> [e.date.ToString(); e.location; e.duration.ToString(); e.description] |> String.concat ";" )
        csvHead::cols |> String.concat("\n")

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

    let checkAuth auth =
        printfn "Auth: %A" auth
        Either.Left ()

    let queries =
        [
            pathScan "/api/entries/%s/%s" (fun (startDateStr, endDateStr) ->
                let startDate = DateTime.Parse(startDateStr)
                let endDate =  DateTime.Parse(endDateStr)
                Writers.setHeader "Content-Type" "application/json"
                >=> Successful.OK (getEntries startDate endDate);
            );
        ]

    let commandApi =
        {
            updateCommands =
                [
                    ("/api/entries/%s/description", modifyDescription);
                    ("/api/entries/%s/location", modifyLocation);
                    ("/api/entries/%s/duration", modifyDuration);
                ];
            createCommands =
                [
                   ("/api/entries", createEntryCommand)
                ];
            deleteCommands =
                [
                    ("/api/entries/%s", deleteEntryCommand)
                ]
        }

    let auth = new Authorizer<User> (fun (name, password) ->
        printfn "Auth: %s, %s" name password
        match (name, password) with
        | "test", "test" -> Some testUser
        | _ -> None
    )

    Server.start serverConf auth queries commandApi
    0 // return an integer exit code