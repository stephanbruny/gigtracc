namespace Gigtracc.Commands

open System

open Gigtracc.Tracking.Entry
open Gigtracc.Tracking.Project
open Gigtracc.Tracking.User

open Suave

open FSharp.Json
open Suave.Logging
open System.Runtime.Serialization

type ProjectCommands (stream) =
    let modifyProjectAction projectId commandData =
        Command.streamUpdate<Project, ModifyProjectCommand> stream (projectId + "-project") projectId commandData
        "successful"

    member this.createProjectFromJson (json : string) =
        let template = Json.deserialize<ProjectCreateData> json
        createProject template

    member this.createProjectCommand (user : User) (json : string) =
        printfn "createProjectCommand %s" json
        let project = this.createProjectFromJson json
        Command.streamCreate<Project> stream (getProjectEventName project) project
        Command.streamUpdate<User, ModifyUserCommand> stream "users" user.name (AddProject project.id)
        "successful"

    member this.createProjectEntryCommand user json =
        let createEntryData = json |> Json.deserialize<CreateEntryCommand>
        let entryStreamName = createEntryData.projectId + "-entry"
        let entry = createFromCommand createEntryData
        Command.streamCreate<Entry> stream entryStreamName entry
        "successful"

    member this.modifyEntryDuration projectId entryId valueStr =
        let value = Single.Parse(valueStr, Globalization.CultureInfo.InvariantCulture)
        Command.streamUpdate<Entry, ModifyEntryCommand> stream (projectId + "-entry") entryId (ChangeDuration (entryId, (float value)))
        "successful"

    member this.modifyEntryDescription projectId entryId value =
        Command.streamUpdate<Entry, ModifyEntryCommand> stream (projectId + "-entry") entryId (ChangeDescription (entryId, value))
        "successful"

    member this.modifyEntryLocation projectId entryId value =
        Command.streamUpdate<Entry, ModifyEntryCommand> stream (projectId + "-entry") entryId (ChangeLocation (entryId, value))
        "successful"


    member this.modifyProjectName projectId value =
        modifyProjectAction projectId (ModifyProjectCommand.ModifyName (projectId, value))

    member this.modifyProjectDescription projectId value =
        modifyProjectAction projectId (ModifyProjectCommand.ModifyDescription (projectId, value))

    member this.modifyProjectClientName projectId value =
        modifyProjectAction projectId (ModifyProjectCommand.ModifyClientName (projectId, value))

    member this.modifyProjectClientAddress projectId value =
        modifyProjectAction projectId (ModifyProjectCommand.ModifyClientAddress (projectId, value))

    member this.modifyProjectPricePerHour projectId valueStr =
        let value = Single.Parse(valueStr, Globalization.CultureInfo.InvariantCulture)
        modifyProjectAction projectId (ModifyProjectCommand.ModifyPricePerHour (projectId, (float value)))

    member this.modifyProjectStartDate projectId valueStr =
        let value = DateTime.Parse(valueStr)
        modifyProjectAction projectId (ModifyProjectCommand.ModifyStartDate (projectId, value))

    member this.modifyProjectEndDate projectId valueStr =
        let value = DateTime.Parse(valueStr)
        modifyProjectAction projectId (ModifyProjectCommand.ModifyEndDate (projectId, value))

    member this.modifyProjectTax projectId valueStr =
        let value = Single.Parse(valueStr, Globalization.CultureInfo.InvariantCulture)
        modifyProjectAction projectId (ModifyProjectCommand.ModifyTax (projectId, (float value)))
