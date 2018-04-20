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
open Gigtracc.Utils
open Gigtracc.Commands

open Suave
open Suave.Filters
open Suave.Operators

open FSharp.Json
open Suave.Logging
open System.Runtime.Serialization
open Suave

let maybeReadFile path =
    if File.Exists path then
        File.ReadAllText(path, Encoding.UTF8) |> Some
    else
        None

let getEnvOrDefault key defaultValue =
    Option.ofObj (Environment.GetEnvironmentVariable(key))
    |> Option.defaultValue defaultValue

let getDefaultUser (hash : Crypto.CryptoHash) : User =
    createUser hash (
        getEnvOrDefault "GIGTRACC_DEFAULT_USERNAME" "default",
        getEnvOrDefault "GIGTRACC_DEFAULT_EMAIL" "gigtracc@localhost",
        getEnvOrDefault "GIGTRACC_DEFAULT_PASSWORD" "admin",
        "",
        UserPermissions.All
    )

[<EntryPoint>]
let main argv =

    let streamFileName =
        Option.ofObj (Environment.GetEnvironmentVariable("GIGTRACC_STREAMFILE"))
        |> Option.defaultValue "./app-data/default-stream.json"

    let stream =
        maybeReadFile streamFileName
        |> Option.map (createStreamFromJson "default")
        |> Option.defaultValue (createEmptyStream "default")

    stream.streamActor.AddAction(fun _  ->
        let data = Json.serialize stream.items
        File.WriteAllText(streamFileName, data, Encoding.UTF8);
    )

    let hashSalt = getEnvOrDefault "GIGTRACC_SALT" "salt me 1337"
    let hash = new Crypto.CryptoHash(hashSalt, Convert.ToBase64String)

    let defaultUser = getDefaultUser hash

    let allUsers =
        let userList = Command.initializeUserStream stream
        if userList.IsEmpty then
            Command.streamCreate<User> stream "users" defaultUser
            Command.initializeUserStream stream
        else
            userList

    let authorizeUser = new Authorizer<User> (fun (name, password) ->
        allUsers
        |> List.tryFind(fun user -> user.name = name)
        |> Option.bind(fun user ->
            if (hash.CreateSha256 password) = user.password then
                Some user
            else
                None
        )
    )

    let getUserProjects (user : User) =
        Command.getUserData stream user.name
        |> Option.map(fun user ->
            user.projects |> List.map (Command.getProjectFromStream stream)
        )

    let userCommand commandFn (token : string) (json : string) =
        authorizeUser.CheckToken token
        |> Option.map (fun user -> commandFn user json)


    let userRoutes (user : User) =
        [
            pathScan "/api/entries/%s/%s/%s" (fun (projectId, startDateStr, endDateStr) ->
                let streamId = projectId + "-entry"
                let startDate = DateTime.Parse startDateStr
                let endDate = DateTime.Parse endDateStr
                let result =
                    replay<Entry> stream "id" Command.modifyEntryFromJson streamId 0
                    |> getEntriesBetween (startDate, endDate)
                Successful.OK (result |> Json.serialize)
            );
            path "/api/projects" >=> context (fun ctx ->
                let currentProjects = getUserProjects user |> Option.map (getCurrentProjects DateTime.Today) |> Option.defaultValue []
                Successful.OK (currentProjects |> Json.serialize)
            );
            pathScan "/api/projects/%s" (fun dateStr -> context (fun ctx ->
                let date = DateTime.Parse dateStr
                let currentProjects = getUserProjects user |> Option.map (getCurrentProjects date) |> Option.defaultValue []
                Successful.OK (currentProjects |> Json.serialize)
            ));
            path "/api/event-stream" >=> context(fun _ ->
                let result = stream.items |> Json.serialize
                Successful.OK result
            )
        ]

    let projectCommands = new ProjectCommands(stream)

    let queries =
        [
            Writers.setHeader "Content-Type" "application/json"
            >=> context (fun ctx ->
                App.getSessionValue ctx "token"
                |> Option.bind(authorizeUser.CheckToken)
                |> Option.bind(fun userData ->
                    let usr = authorizeUser.CheckToken
                    choose (userRoutes userData) |> Some
                )
                |> Option.defaultValue (RequestErrors.FORBIDDEN "Not authorized")
            )
        ]

    let createCommands =
        [
            ("/api/project", userCommand projectCommands.createProjectCommand);
            ("/api/project/entry", userCommand projectCommands.createProjectEntryCommand)
        ]

    let updateCommands =
        [
            pathScan "/api/project/%s/entry/%s/duration" (fun (projectId, entryId) ->
                App.requestAction ( projectCommands.modifyEntryDuration projectId entryId )
            );
            pathScan "/api/project/%s/entry/%s/location" (fun (projectId, entryId) ->
                App.requestAction ( projectCommands.modifyEntryLocation projectId entryId )
            );
            pathScan "/api/project/%s/entry/%s/description" (fun (projectId, entryId) ->
                App.requestAction ( projectCommands.modifyEntryDescription projectId entryId )
            );
            pathScan "/api/project/%s/name" (projectCommands.modifyProjectName >> App.requestAction);
            pathScan "/api/project/%s/description" (projectCommands.modifyProjectDescription >> App.requestAction);
            pathScan "/api/project/%s/clientName" (projectCommands.modifyProjectClientName >> App.requestAction);
            pathScan "/api/project/%s/clientAddress" (projectCommands.modifyProjectClientAddress >> App.requestAction);
            pathScan "/api/project/%s/pricePerHour" (projectCommands.modifyProjectPricePerHour >> App.requestAction);
            pathScan "/api/project/%s/startDate" (projectCommands.modifyProjectStartDate >> App.requestAction);
            pathScan "/api/project/%s/endDate" (projectCommands.modifyProjectEndDate >> App.requestAction);
            pathScan "/api/project/%s/tax" (projectCommands.modifyProjectTax >> App.requestAction);
        ]

    let serverJson = File.ReadAllText("server.json", Encoding.UTF8);
    let serverConf = Json.deserialize<Server.WebserverConfig>(serverJson)
    Server.start serverConf authorizeUser queries ({ updateCommands = updateCommands; createCommands = createCommands; deleteCommands = [] })

    0