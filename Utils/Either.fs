[<AutoOpen>]
module Either

type Either<'a, 'b> =
    | Left of 'a
    | Right of 'b

type either<'a, 'b> =
    Either<'a, 'b> // lower-case alias like option

let isLeft = function
  | Left _ -> true
  | _      -> false

let isRight = function
  | Right _ -> true
  | _       -> false

let ofOption opt none =
  match opt with
  | Some v -> Left v
  | None -> Right none