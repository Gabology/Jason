#load "Jason.fsx"
open Jason
open FSharp.Data
open System

// Demo of the map function
let json = JsonValue.Parse """{ "birthdate":"2010-05-07" }"""
let mappedStr : MappedJson<string> = json ?| "birthdate"
// Using the map function
mappedStr |> map DateTime.Parse
// Using the operator
DateTime.Parse <!> mappedStr

// APPLY IS AWESOME
let json2 =
    JsonValue.Parse """ { "name": "John", "age": 30, "married": true } """

type Person =
    { Name    : string
      Age     : int
      Married : bool }
    static member Create name age married =
        { Name = name; Age = age; Married = married }

// Step 1 -> Use map to get wrapped function for apply
Person.Create
<!> json2.Read "name"
// Step 2 -> Now use apply to reduce the wrapped multi parameter function into a value
<*> json2.Read "age"
<*> json2.Read "married"
