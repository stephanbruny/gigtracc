namespace Gigtracc.Commands

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

open Suave
open Suave.Filters
open Suave.Operators

open FSharp.Json
open Suave.Logging
open System.Runtime.Serialization
open Suave

module Command =

    let modifyFromJson<'T, 'TCommand> (modifyFn : 'T -> 'TCommand -> 'T) (instance : 'T) (data : string) =
        let command = Json.deserialize<'TCommand> data
        modifyFn instance command

    let modifyUserFromJson = modifyFromJson<User, ModifyUserCommand> modifyUser
    let modifyProjectFromJson = modifyFromJson<Project, ModifyProjectCommand> modifyProject

    let modifyEntryFromJson = modifyFromJson<Entry, ModifyEntryCommand> modifyEntry

    let initializeUserStream stream =
        replay<User> stream "name" modifyUserFromJson "users" 0

    let getUserData stream name =
        replay<User> stream "name" modifyUserFromJson "users" 0
        |> List.tryFind(fun usr -> usr.name = name)

    let getProjectFromStream stream projectId =
        replay<Project> stream "id" modifyProjectFromJson (projectId + "-project") 0
        |> List.head

    let streamCreate<'T> stream name data =
        addEventItem stream name None (EventReplay<'T>.Created data |> Json.serialize)

    let streamUpdate<'T, 'TModified> stream name id (data : 'TModified) =
        let updateData = (id, data |> Json.serialize)
        addEventItem stream name None (EventReplay<'T>.Modified updateData |> Json.serialize)