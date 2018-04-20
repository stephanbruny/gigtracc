namespace Gigtracc.Web

open Suave;

open FSharp.Json

type JsonNetCookieSerialiser () =
    let utf8 = System.Text.Encoding.UTF8
    let jsonConf = JsonConfig.create(allowUntyped = true)

    interface CookieSerialiser with
        member x.serialise m =
            utf8.GetBytes (Json.serializeEx jsonConf m)
        member x.deserialise m =
            Json.deserializeEx<Map<string, obj>> jsonConf (utf8.GetString m)