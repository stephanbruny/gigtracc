namespace Gigtracc.Web

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

    let start (config : WebserverConfig) queries commands creates deletes =
        let conf =
            {
                defaultConfig with
                    bindings = [ HttpBinding.createSimple HTTP config.host config.port ];
                    homeFolder = config.homeFolder |> Option.map getHomeFolderPath
            }
        startWebServer conf (App.serve queries commands creates deletes)