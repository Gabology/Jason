# Jason
Jason is yet another JSON parsing library :). 

# Usage

JSON can be parsed through the `mappedJson` computation expression or through the use of the infix operators.

## Using computation expressions

```fsharp
type Person =
    { Name    : string
      Age     : int
      Married : bool }

let json =
    """{
        "name": "John",
        "age": 30,
        "married": true 
        } """
    |> JsonValue.Parse

mappedJson { let! name = json.Read "name"
             let! age = json.Read "age"
             let! married = json.Read "married"
             return { Name = name
                      Age = age
                      Married = married } }

val it : MappedJson<Person> = Success {Name = "John";
                                       Age = 30;
                                       Married = true;}
  
```

## Using infix operators

```fsharp
type Person =
    { Name    : string
      Age     : int
      Married : bool }
    static member Create name age married =
        { Name = name; Age = age; Married = married }

Person.Create
<!> json2.Read "name"
<*> json2.Read "age"
<*> json2.Read "married"

val it : MappedJson<Person> = Success {Name = "John";
                                       Age = 30;
                                       Married = true;}

```
