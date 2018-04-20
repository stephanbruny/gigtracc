namespace Gigtracc.Web

open System

open Gigtracc.Utils

type Authorizer<'TSession> (checkCredentials : string * string -> 'TSession option) =
    let authSalt = "foobar123"

    let tokenizer = new Crypto.CryptoHash(authSalt, Convert.ToBase64String)

    let mutable sessions : Map<string, 'TSession> = Map.empty

    member this.GetToken (credentials : string * string) =
        checkCredentials credentials
        |> Option.map(fun result ->
            let token = System.Guid.NewGuid().ToString() |> tokenizer.CreateSha256
            (token, result)
        )

    member this.Login (credentials : string * string) =
        match this.GetToken(credentials) with
        | Some (token, data) ->
            sessions <- sessions |> Map.add token data
            Some (token, data)
        | None -> None

    member this.CheckToken (token : string) = 
        sessions |> Map.tryFind token

