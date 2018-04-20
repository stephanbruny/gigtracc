namespace Gigtracc.Tracking

open System
open FSharp.Json

open Gigtracc.Utils
open Gigtracc.Tracking.Entry
open Gigtracc.Tracking.Project

module User =

    type UserPermissions =
    | All
    | ReadOnly
    | Self

    type User =
        {
            name : string;
            email : string;
            password : string;
            address : string;
            projects : string list;
            permission : UserPermissions;
        }

    type ModifyUserCommand =
    | ModifyName of string
    | ModifyEmail of string
    | ModifyPassword of string
    | ModifyAddress of string
    | ModifyPermission of UserPermissions
    | AddProject of string

    let createUser (hash : Crypto.CryptoHash) (name, email, password, address, permission) =
        {
            name = name;
            email = email;
            password = hash.CreateSha256 password;
            address = address;
            permission = permission;
            projects = [];
        }

    let modifyUserName (user : User) name = { user with name = name }
    let modifyUserEmail (user : User) email = { user with email = email }
    let modifyUserPassword (user : User) password = { user with password = password }
    let modifyUserAddress (user : User) address = { user with address = address }
    let modifyUserPermission (user : User) permission = { user with permission = permission }
    let addUserProject (user : User) (projectId : string) = { user with projects = user.projects |> List.append [projectId] }

    let modifyUser (user : User) (modify : ModifyUserCommand) =
        match modify with
        | ModifyName n -> modifyUserName user n
        | ModifyEmail n -> modifyUserEmail user n
        | ModifyPassword pw -> modifyUserPassword user pw
        | ModifyAddress adr -> modifyUserAddress user adr
        | ModifyPermission perm -> modifyUserPermission user perm
        | AddProject id -> addUserProject user id