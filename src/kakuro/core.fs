module Kakuro.Core

type IDraw = interface
    abstract draw : unit -> string
end

type Empty = struct
    interface IDraw with
        member __.draw() = "   -----  "
end

type Down(down : int) = class
    interface IDraw with
        member __.draw() = sprintf "   %2d\\--  " down
    
    member __.down() = down
end

type Across(across : int) = class
    interface IDraw with
        member __.draw() = sprintf "   --\\%2d  " across
    
    member __.across() = across
end

type DownAcross(down : int, across : int) = class
    interface IDraw with
        member __.draw() = sprintf "   %2d\\%2d  " down across
    
    member __.down() = down
    member __.across() = across
end

type Value(values : Set<int>) = class
    interface IDraw with
        member __.draw() = 
            if 1 = values.Count then 
                values
                |> Set.map (fun x -> "     " + x.ToString() + "    ")
                |> String.concat ""
            else 
                " " + ([ 1..9 ]
                       |> List.map (fun x -> 
                              if Set.contains x values then x.ToString()
                              else ".")
                       |> String.concat "")
    
    member __.values = values

    override x.Equals(y) =
        match y with
        | :? Value as v -> values = v.values
        | _ -> false

    override x.GetHashCode() = values.GetHashCode()
end

let a across = Across(across)
let d down = Down(down)
let da down across = DownAcross(down, across)
let e = Empty()
let v = Value(set [ 1..9 ])

let drawRow (row : IDraw list) = 
    (row
     |> List.map (fun x -> x.draw())
     |> String.concat "")
    + "\n"

let drawGrid grid = 
    "\n" + 
    (List.map (fun row -> drawRow (row)) grid
    |> String.concat "")

let allDifferent (nums : int list) = (nums.Length = (set nums).Count)

let rec permute (vs : Value list) target (soFar: int list) = 
    if target >= 1 then 
        if soFar.Length = (vs.Length - 1) then [ soFar @ [ target ] ]
        else 
            (List.nth vs soFar.Length).values
            |> Seq.collect (fun v -> permute vs (target - v) (soFar @ [ v ]))
            |> List.ofSeq
    else []

let permuteAll vs total = permute vs total []

let isPossible (cell : Value) n = Set.contains n cell.values

let rec transpose matrix = 
    match matrix with // matrix is a list<list<T>>
    | row :: rows -> // case when the list of rows is non-empty
        match row with // rows is a list<T>
        | col :: cols -> // case when the row is non-empty
            // Take first elements from all rows of the matrix
            let first = List.map List.head matrix
            // Take remaining elements from all rows of the matrix
            // and then transpose the resulting matrix
            let rest = transpose (List.map List.tail matrix)
            first :: rest
        | _ -> []
    | _ -> []

let solveStep (cells : Value list) total = 
    let final = cells.Length - 1
    permuteAll cells total
    |> List.filter (fun p -> isPossible (List.nth cells final) (List.nth p final))
    |> List.filter allDifferent
    |> transpose
    |> List.map (fun p -> Value(set p))

let solvePairRow pair = 
    match pair with
    | [nvs] -> nvs
    | [ nvs; [] ] -> nvs
    | [ nvs : IDraw list; vs ] -> 
        nvs @ (solveStep (vs |> List.map (fun x -> x :?> Value)) 
                   (match Seq.last nvs with
                   | :? Across as x -> x.across()
                   | :? DownAcross as x -> x.across()
                   | _ -> 0) |> List.map (fun x -> x :> IDraw))
    | _ -> []

let solvePairCol pair = 
    match pair with
    | [nvs] -> nvs
    | [ nvs; [] ] -> nvs
    | [ nvs : IDraw list; vs ] -> 
        nvs @ (solveStep (vs |> List.map (fun x -> x :?> Value)) 
                   (match Seq.last nvs with
                   | :? Down as x -> x.down()
                   | :? DownAcross as x -> x.down()
                   | _ -> 0) |> List.map (fun x -> x :> IDraw))
    | _ -> []

let rec partitionBy f coll = 
    match coll with
    | [] -> []
    | x :: xs -> 
        let fx = f x
        let run = 
            coll
            |> Seq.takeWhile (fun y -> fx = f y)
            |> Seq.toList
        run :: partitionBy f (coll
                              |> Seq.skip run.Length
                              |> Seq.toList)

let rec drop n coll =
  match coll with
  | [] -> []
  | x :: xs when (n <= 1) -> xs
  | x :: xs -> drop (n - 1) xs

let rec partitionAll n step coll = 
    match coll with
    | [] -> []
    | x :: xs -> 
            let seg = Seq.truncate n coll |> Seq.toList
            seg :: partitionAll n step (coll |> drop step |> Seq.toList)

let partitionN n coll = partitionAll n n coll

let solveRow cells = 
    partitionN 2 <| partitionBy (fun (x : IDraw) -> x :? Value) cells |> List.collect solvePairRow

let solveCol cells = 
    partitionN 2 <| partitionBy (fun (x : IDraw) -> x :? Value) cells |> List.collect solvePairCol

let solveGrid grid = 
    grid
    |> List.map solveRow
    |> transpose
    |> List.map solveCol
    |> transpose

let grid1 : IDraw list list = 
    [ [ e
        (d 4)
        (d 22)
        e
        (d 16)
        (d 3) ]
      [ (a 3)
        v
        v
        (da 16 6)
        v
        v ]
      [ (a 18)
        v
        v
        v
        v
        v ]
      [ e
        (da 17 23)
        v
        v
        v
        (d 14) ]
      [ (a 9)
        v
        v
        (a 6)
        v
        v ]
      [ (a 15)
        v
        v
        (a 12)
        v
        v ] ]

let rec solver grid =
  let g = solveGrid grid
  if (g = grid) then
    g
  else
    drawGrid g |> printf "%s"
    solver g

[<EntryPoint>]
let main _ = 
    grid1 
    |> solver
    |> drawGrid
    |> printf "%s"
    0 // return an integer exit code
