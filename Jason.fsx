#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"

open FSharp.Data
open FSharp.Data.JsonExtensions
open System.Text.RegularExpressions

type MappedJson<'TObject> =
    | Success of 'TObject
    | Error   of error:string

// Maps a wrapped value 'a to 'b using the provided mapping function
// ('a -> 'b) -> MappedJson<'a> -> MappedJson<'b>
let map f json =
    match json with
    | Success obj ->
        Success (f obj)
    | Error e ->
        Error e

// Applies a wrapped function 'a -> 'b to a wrapped value M<'a>
let apply (wrappedFunc: MappedJson<'a -> 'b>) (x: MappedJson<'a>) =
    match x, wrappedFunc with
    | Success obj, Success f ->
        Success (f obj)
    | _, Error e ->
        Error e
    | Error e, _ ->
        Error e

// Unwraps a wrapped value M<'a> inside a function that yields M<'b>
let bind m f =
    match m with
    | Success o -> f o
    | Error e -> Error e

type MappedJsonBuilder() =
    member __.Bind m f = bind m f
    member __.Return(x) = Success x
    member __.ReturnFrom(m) = m

let mappedJson = MappedJsonBuilder()

let coerceType<'TValue> =
    let coerce v = box v :?> 'TValue
    function
    | JsonValue.String s when typeof<'TValue> = typeof<string> ->
        Success (coerce s)
    // This will obviously mean that we can lose precision but that's up to the user
    | JsonValue.Number n when typeof<'TValue> = typeof<decimal> ->
        Success (coerce n)
    | JsonValue.Number n when typeof<'TValue> = typeof<float> ->
        float n |> coerce |> Success
    | JsonValue.Number n when typeof<'TValue> = typeof<int> ->
        int n |> coerce |> Success
    | JsonValue.Boolean b when typeof<'TValue> = typeof<bool> ->
        Success (box b :?> 'TValue)
    // HACK: Doesn't seem like runtime type checking works for Option<T>
    | JsonValue.Null when typeof<'TValue>.Name = "FSharpOption`1" ->
        Success (coerce None)
    | prop ->
        Error (sprintf "Could not coerce value <%A> to type <%s>" prop typeof<'TValue>.Name)

let property prop (json:JsonValue) f =
    let errorRegex =
        Regex @"Didn't find property '([\w\d]+)'"
    try
        let prop = JsonExtensions.GetProperty(json, prop)//json?property
        f prop
    with e ->
        let ``match`` = errorRegex.Match e.Message
        if ``match``.Success then
            Error ``match``.Value
        else
            printfn "Property that failed (%s), value = %A" prop json
            raise e

let parse<'TValue> prop (json:JsonValue) : MappedJson<'TValue> =
    property prop json coerceType

let tryParse property json =
    match parse property json with
    | Success o -> Success <| Some o
    | Error e   -> Success None

let inline parseObject< ^TValue when ^TValue : (static member FromJson: JsonValue -> MappedJson<'TValue>)> json =
    ( ^TValue : (static member FromJson : JsonValue -> MappedJson<'TValue>) json)

let inline parseAs prop json =
    property prop json parseObject

type JsonValue with
    member x.Read property =
        parse property x
    member x.TryRead property =
        tryParse property x

let sequenceJson st v =
    match st, v with
    | Success xs, Success x ->
        Success (x::xs)
    | _, Error e ->
        Error e

let tryParseArr<'TValue> arr (json:JsonValue) : MappedJson<'TValue list> =
    match JsonExtensions.TryGetProperty (json, arr) with
    | Some (JsonValue.Array elems) ->
        elems
        |> List.ofArray
        |> List.map coerceType
        |> List.fold sequenceJson (Success [])
    | _ -> Error "Array does not contain same elements of same type"

let (?|) = fun js str -> parse str js
let (??|) = fun js str -> tryParseArr str js
let (<!>) = map
let (<*>) = apply
let (>>=) = fun f m -> bind m f