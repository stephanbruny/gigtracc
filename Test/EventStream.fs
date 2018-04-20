namespace Gigtracc.Test

open NUnit
open NUnit.Framework
open FsUnit
open FsUnitTyped

open Gigtracc.Events.EventStream

open FSharp.Json

module EventStreamTest =

    type TestItem = {
        id : string;
        name : string;
        foo : int;
    }

    type ModifyTestItem =
    | ModifyName of string
    | ModifyFoo of int

    let modifyTestFn (item : TestItem) data =
        let command = Json.deserialize<ModifyTestItem> data
        match command with
        | ModifyName n -> { item with name = n }
        | ModifyFoo i -> { item with foo = i }

    [<Test>]
    let ``create event stream`` () =
        let stream = createEmptyStream "test"
        let testData = { id = "1"; name = "Foobar"; foo = 0 }
        let created = EventReplay<TestItem>.Created testData |> Json.serialize
        addEventItem stream "foo" None (created)
        let rep = replay<TestItem> stream "id" modifyTestFn "foo" 0
        printfn "REPLAY: %A" rep
        rep |> List.head |> shouldEqual testData

    [<Test>]
    let ``update from event stream`` () =
        let jsonConf = JsonConfig.create(allowUntyped = true)
        let stream = createEmptyStream "test"
        let testData = { id = "1"; name = "Foobar"; foo = 0 }
        let created = EventReplay<TestItem>.Created testData |> Json.serializeEx jsonConf
        addEventItem stream "foo" None (created)
        let rep = replay<TestItem> stream "id" modifyTestFn "foo" 0
        rep |> List.head |> shouldEqual testData
        let update = ModifyFoo 42 |> Json.serialize
        addEventItem stream "foo" None (EventReplay<TestItem>.Modified ("1", update) |> Json.serialize)
        let repUp = replay<TestItem> stream "id" modifyTestFn "foo" 0
        repUp |> List.head |> shouldEqual { id = "1"; name ="Foobar"; foo = 42 }

    [<Test>]
    let ``more complex event history`` () =
        let stream = createEmptyStream "test"
        let testData =
            [
                { id = "id-1"; name = "Foo"; foo = 0 };
                { id = "id-2"; name = "Bar"; foo = 3 };
                { id = "id-3"; name = "Blob"; foo = 64 };
                { id = "id-4"; name = "Foobar"; foo = 42 };
            ]
        let createItem stream item =
            EventReplay<TestItem>.Created item |> Json.serialize
            |> addEventItem stream "foo" None

        let updateItem stream id (update : ModifyTestItem) =
            let up = update |> Json.serialize
            addEventItem stream "foo" None (EventReplay<TestItem>.Modified (id, up) |> Json.serialize)

        let removeItem stream id = addEventItem stream "foo" None (EventReplay<TestItem>.Removed id |> Json.serialize)

        testData |> List.iter (createItem stream)
        removeItem stream "id-3"
        let getSnap () = replay<TestItem> stream "id" modifyTestFn "foo" 0
        let snap = getSnap()
        snap |> List.tryFind(fun i -> i.id = "id-3") |> shouldEqual None

        updateItem stream "id-2" (ModifyFoo 123)
        updateItem stream "id-4" (ModifyName "Barfoo")
        let snap2 = getSnap()
        let barItem = snap2 |> List.tryFind(fun i -> i.id = "id-2")
        let barfooItem = snap2 |> List.tryFind(fun i -> i.id = "id-4")
        barItem |> shouldNotEqual None
        barfooItem |> shouldNotEqual None
        barItem.Value.foo |> shouldEqual 123
        barfooItem.Value.name |> shouldEqual "Barfoo"
        barfooItem.Value.foo |> shouldEqual 42