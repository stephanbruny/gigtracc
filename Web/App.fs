namespace Gigtracc.Web

open System
open Suave
open Suave.Filters
open Suave.Files
open Suave.Operators
open Suave.Sockets
open System.Net

open Gigtracc.Web
open Suave.Headers.Fields
open Suave.State.CookieStateStore

open FSharp.Json

module App =

    type ApiCommandAction =
    | ModifyEntryDescription of (string -> string -> string)
    | ModifyEntryDuration of (string -> string -> string)

    type UriPart =
    | Static of string
    | Param of string

    type ApiDefinition =
        {
            createCommands : (string * (string -> string -> string option)) list;
            updateCommands : (HttpContext -> Async<HttpContext option>) list;
            deleteCommands : (string * (string -> string -> string -> string option )) list;
        }

    let setSessionValue (key : string) (value : 'T) : WebPart =
        context (fun ctx ->
            match HttpContext.state ctx with
            | Some state ->
                state.set key value
            | _ ->
                never // fail
            )

    let getSessionValue (ctx : HttpContext) (key : string) : 'T option =
        match HttpContext.state ctx with
        | Some state ->
            state.get key
        | _ ->
            None

    let requestAction applicative = context(fun ctx ->
        let contents = ctx.request.rawForm |> System.Text.Encoding.UTF8.GetString
        let result = applicative (contents)
        Writers.setHeader "Content-Type" "application/json"
        >=> Successful.OK result
    )
    let apiRequest applicative =
        context(fun ctx ->
            let contents = ctx.request.rawForm |> System.Text.Encoding.UTF8.GetString
            getSessionValue ctx "token"
            |> Option.map(fun token ->
                applicative token contents
                |> Option.map(fun result ->
                    Writers.setHeader "Content-Type" "application/json"
                    >=> Successful.OK result
                )
                |> Option.defaultValue(RequestErrors.NOT_FOUND "Not found")
            )
            |> Option.defaultValue (RequestErrors.FORBIDDEN "Unauthorized")
        )

    let buildUriRoute uri action =
        let typedPath = new PrintfFormat<(string -> string), unit, string, string, string> (uri)
        pathScan typedPath (action >> apiRequest)

    let getRoutes queries =
        let defaultGetRoutes =
            [
                path "/" >=> browseFileHome "index.html";
                path "/login" >=> browseFileHome "index.html";
                browseHome;
                RequestErrors.NOT_FOUND "404 - not found";
            ]
        let routes = defaultGetRoutes |> List.append queries
        printfn "Routes: %A" routes
        choose routes

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

    let contextNotAuthorized ctx =
        {
            ctx with
                response =
                    {
                        ctx.response with
                            status = { code = 403; reason = "Not authorized" }
                    }
        }

    let checkHeader (checkAuth : string -> Either<_,_>) ctx = asyncOption {
        match ctx.request.header "Authorization" with
        | Choice1Of2 auth ->
            match checkAuth auth with
            | Either.Left _ -> return ctx
            | Either.Right _ -> return contextNotAuthorized ctx
        | Choice2Of2 _ -> return contextNotAuthorized ctx
    }

    let expectFormValue errorMessage (value : Choice<string, string>) =
        match value with
        | Choice1Of2 v -> v
        | _ -> failwith errorMessage

    let unauthorizedRoutes<'TSession> (auth : Authorizer<'TSession>) =
        Writers.setHeader "Cache-Control" "no-cache"
        >=> choose [
            GET >=>
                choose [
                    path "/" >=> browseFileHome "login.html";
                    path "/login" >=> browseFileHome "login.html";
                    browseHome;
                ];
            POST >=>
                choose [
                    path "/login" >=> request(fun req ->
                        let userName = expectFormValue "Missing name" (req.formData "name")
                        let password = expectFormValue "Missing password" (req.formData "password")
                        match auth.Login(userName, password) with
                        | Some (token, data) ->
                            setSessionValue "token" token
                            >=> setSessionValue "user" (data |> Json.serialize)
                            >=> browseFileHome "index.html"
                        | None -> RequestErrors.UNAUTHORIZED "Invalid credentials"
                    )
                ]
        ]

    let authorizeRequest (auth: Authorizer<'TSession>) handler = context(fun ctx ->
        let userToken = getSessionValue ctx "token"
        match userToken with
        | Some token ->
            match auth.CheckToken token with
            | Some sessionData ->
                handler
            | _ -> never
        | None -> never
    )

    let serve<'TSession> (auth: Authorizer<'TSession>) queries (api : ApiDefinition) =
        let defaultRoutes = unauthorizedRoutes auth
        let authRoutes =
            choose [
                GET >=> getRoutes queries;
                PUT >=> choose api.updateCommands;
                POST >=> postRoutes api.createCommands;
                DELETE >=> deleteRoutes api.deleteCommands;
                browseHome;
            ]
        statefulForSession
        >=> choose [
                authorizeRequest auth authRoutes;
                defaultRoutes;
            ]