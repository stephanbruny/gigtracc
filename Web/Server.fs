namespace Gigtracc.Web

open System
open System.IO
open Gigtracc.Messaging
open Suave

module Server =

    type WebserverConfig = {
        host : string;
        port : int;
        homeFolder : string option;
    }

    let serverActor = new Actor<string>();

    let getHomeFolderPath path = Path.GetFullPath path

    let serverKey = "foobar"

    let start (config : WebserverConfig) checkAuth queries api =
        let conf =
            {
                defaultConfig with
                    bindings = [ HttpBinding.createSimple HTTP config.host config.port ];
                    homeFolder = config.homeFolder |> Option.map getHomeFolderPath;
                    cookieSerialiser = new JsonNetCookieSerialiser()
            }
        startWebServer conf (App.serve checkAuth queries api)