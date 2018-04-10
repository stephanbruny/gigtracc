namespace Gigtracc.Tracking

open System
open Gigtracc.Tracking.Entry

module Project =

    type Payment = {
        currency : string;
        pricePerHour : float;
    }

    type Client = {
        name : string;
        address : string;
    }

    type Project = {
        id : string;
        name : string;
        client : Client;
        startDate : DateTime;
        endDate : DateTime;
        mutable entries : Entry list;
        payment: Payment;
    }

    let getEntriesByMonth project month year =
        project.entries |> List.filter(fun entry -> entry.date.Month = month && entry.date.Year = year)
    let addProjectEntries (project : Project) entries = { project with entries = project.entries |> List.append entries }

    let calculateTotalHours project startDate endDate =
        let entries = project.entries |> List.filter(fun entry -> entry.date >= startDate && entry.date <= endDate )
        if entries.Length > 0 then
            entries |> List.map(fun entry -> entry.duration) |> List.reduce(+)
        else
           0.0

    let calculatePrice project startDate endDate =
        let totalHours = calculateTotalHours project startDate endDate
        totalHours * project.payment.pricePerHour