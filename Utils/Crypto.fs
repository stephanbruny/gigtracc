namespace Gigtracc.Utils

open System
open System.Text
open System.Security.Cryptography

module Crypto =

    type CryptoHash (salt : string, convert : byte [] -> string) =
        let salten value = (salt + value)
        member this.CreateSha256 (value : string) =
            let sha = SHA256.Create()
            let inputBuffer = value |> salten |> Encoding.UTF8.GetBytes
            let output = sha.ComputeHash( inputBuffer )
            output |> convert

        member this.IsEqual (inputHash, compareValue) =
            let compare = compareValue |> this.CreateSha256
            inputHash = compare