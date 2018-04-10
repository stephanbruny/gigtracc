namespace Gigtracc.Tracking

open System
open FSharp.Json

open Gigtracc.Tracking.Entry
open Gigtracc.Tracking.Project

module User =

    type UserPermissions =
    | All
    | ReadOnly
    | Self

    type User = {
        name : string;
        email : string;
        password : string;
        address : string;
        projects : Project list;
        permission : UserPermissions;
    }

    let modifyUserName (user : User) name = { user with name = name }
    let modifyUserEmail (user : User) email = { user with email = email }
    let modifyUserPassword (user : User) password = { user with password = password }
    let modifyUserAddress (user : User) address = { user with address = address }
    let modifyUserPermission (user : User) permission = { user with permission = permission }
    let addUserProject (user : User) (proj : Project) = { user with projects = user.projects |> List.append [proj] }