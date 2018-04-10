namespace Gigtracc.Web

open System
open Suave
open Suave.Filters
open Suave.Files
open Suave.Operators
open Suave.Sockets

module App =

    type ApiCommandAction =
    | ModifyEntryDescription of (string -> string -> string)
    | ModifyEntryDuration of (string -> string -> string)

    type UriPart = 
    | Static of string
    | Param of string

    let apiRequest applicative =
        context(fun ctx ->
            let contents = ctx.request.rawForm |> System.Text.Encoding.UTF8.GetString
            Writers.setHeader "Content-Type" "application/json"
            >=> Successful.OK (applicative contents)
        )

    let buildUriRoute uri action =
        let typedPath = new PrintfFormat<(string -> string), unit, string, string, string> (uri)
        pathScan typedPath (fun (id) -> apiRequest (action id))

    let getRoutes getEntries =
        choose
            [
                pathScan "/api/entries/%s/%s" (fun (startDateStr, endDateStr) ->
                    let startDate = DateTime.Parse(startDateStr)
                    let endDate =  DateTime.Parse(endDateStr)
                    Writers.setHeader "Content-Type" "application/json"
                    >=> Successful.OK (getEntries startDate endDate);
                );
                path "/goodbye" >=> Successful.OK "Good bye GET";
                browseHome;
                RequestErrors.NOT_FOUND "404 - not found";
            ];

    let putRoutes commandActions =
        let actions = commandActions |> List.map(fun (route, command) ->
            buildUriRoute route command
        )
        choose actions

    let postRoutes commandActions =
        let actions = commandActions |> List.map(fun (route, command) ->
            path route >=> (apiRequest command)
        )
        choose actions

    let deleteRoutes commandActions =
        let actions = commandActions |> List.map(fun (route, command) ->
            buildUriRoute route command
        )
        choose actions

    let serve queries commands createCommands deleteCommands =
        request (fun req -> 
            printfn "What: %A" req
            choose [
                GET >=> getRoutes queries;
                PUT >=> putRoutes commands;
                POST >=> postRoutes createCommands;
                DELETE >=> deleteRoutes deleteCommands;
            ]
        )