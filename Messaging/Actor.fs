namespace Gigtracc.Messaging

type Actor<'T> () =
    let mutable actions : ('T -> unit) [] = Array.empty

    member this.Mailbox = MailboxProcessor<'T>.Start(fun inbox->
        // the message processing function
        let rec messageLoop() = async{

            // read a message
            let! msg = inbox.Receive()

            // process a message
            actions |> Array.Parallel.iter(fun action -> action msg)

            // loop to top
            return! messageLoop()
            }

        // start the loop
        messageLoop()
        )

    member this.AddAction fn = actions <- actions |> Array.append [| fn |]
    member this.Send msg = this.Mailbox.Post msg