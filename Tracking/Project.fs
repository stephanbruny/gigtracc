namespace Gigtracc.Tracking

open System
open Gigtracc.Tracking.Entry
open Gigtracc.Events.EventStream

module Project =

    let defaultCurrency = "EUR"

    type Payment = {
        currency : string;
        pricePerHour : float;
        tax : float;
    }

    type Client = {
        name : string;
        address : string;
    }

    type Project = {
        id : string;
        name : string;
        description : string;
        client : Client;
        startDate : DateTime;
        endDate : DateTime;
        mutable entries : Entry list;
        payment: Payment;
    }

    type ProjectCreateData = {
        name : string;
        description : string;
        client : Client;
        startDate : DateTime;
        endDate : DateTime;
        pricePerHour: float;
        tax: float;
    }

    type ModifyProjectCommand =
    | ModifyName of string * string
    | ModifyStartDate of string * DateTime
    | ModifyEndDate of string * DateTime
    | ModifyClientName of string * string
    | ModifyClientAddress of string * string
    | ModifyPricePerHour of string * float
    | ModifyDescription of string * string
    | ModifyTax of string * float

    let createProject (data : ProjectCreateData) =
        {
            id = System.Guid.NewGuid().ToString();
            name = data.name;
            description = data.description;
            startDate = data.startDate;
            endDate = data.endDate;
            client = data.client;
            payment = {  currency = defaultCurrency; pricePerHour = data.pricePerHour; tax = data.tax };
            entries = []
        }

    let getProjectEventName (project : Project) = project.id + "-project"
    let getProjectEntryEventName (project : Project) = project.id + "-entry"

    let getProjectEntries (project : Project) (stream : StreamSource) version =
        getAllEvents stream (getProjectEntryEventName project) version


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

    let modifyProjectName (project : Project) name = { project with name = name }

    let modifyProjectDescription (project : Project) descr = { project with description = descr }

    let modifyProjectStartDate (project : Project) date = { project with startDate = date }
    let modifyProjectEndDate (project : Project) date = { project with endDate = date }
    let modifyProjectClientName (project : Project) name =
        { project with client =  { project.client with name = name } }
    let modifyProjectClientAddress (project : Project) address =
        { project with client =  { project.client with address = address } }

    let modifyProjectPaymentPrice (project : Project) price =
        { project with payment =  { project.payment with pricePerHour = price } }

    let modifyProjectPaymentTax (project : Project) tax =
        { project with payment =  { project.payment with tax = tax } }

    let modifyProject (project : Project) command =
        match command with
        | ModifyName (id, name) when id = project.id -> modifyProjectName project name
        | ModifyDescription (id, text) when id = project.id -> modifyProjectDescription project text
        | ModifyStartDate (id, date) when id = project.id -> modifyProjectStartDate project date
        | ModifyEndDate (id, date) when id = project.id -> modifyProjectEndDate project date
        | ModifyClientName (id, name) when id = project.id -> modifyProjectClientName project name
        | ModifyClientAddress (id, address) when id = project.id -> modifyProjectClientAddress project address
        | ModifyPricePerHour (id, price) when id = project.id -> modifyProjectPaymentPrice project price
        | ModifyTax (id, tax) when id = project.id -> modifyProjectPaymentTax project tax
        | _ -> project

    let getCurrentProjects date (projects : Project list) =
        projects |> List.filter (fun proj -> proj.startDate <= date && proj.endDate >= date)
