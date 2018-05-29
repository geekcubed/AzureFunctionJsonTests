# Azure Function Json Tests
This repo contains a simple setup for exploring the behaviour of version dependencies with json.net when running as part of an Azure Function (v1).  The solution contains two Projects:

## JsonTests.Data 
A basic repository-pattern style class library (C# Class Library compiling to .net Standard 2).  Contains:

 - abstract base class modelling data entities plus a single concrete implementation
 - simple repostiory for supporting R + W operations
 - Utils package for working with CosmosDb
 
##  JsonTests.Func
An Azure Functions (V1) app (.Net Full Framework 4.7.2).

 - Contains a single function triggered via a HTTP request.  
 - Function parses some hard-coded JSON to build up an object, stores it in CosmosDB and examines the Types of data present at various points
 - Outputs results of tests to the Function console log

This setup is a representative example of our production architecture. Both Projects include an explict reference to `Newtonsoft.Json` version 10.0.3

# Observed Behaviour
Deserializing JSON with "complex" properties (nested objects) to anonymous objects (e.g. `Dictionary<string, object>`) results in those complex properties being represented as `JObjects (`Newtonsoft.Json.Linq.JObject`).  At the point of deserisling, and passing these into the data project, Type checking is working as expected:

```
sprocketItem["Supplier"].GetType().FullName; //"Newtonsoft.Json.Linq.JObject"
sprocketItem["Supplier"].GetType() == typeof(Newtonsoft.Json.Linq.JObject); //true
```

However **after** the entity passes through the CosmosDB Client, these Type checks fail

```
itemAfterSave["Supplier"].GetType().FullName; //"Newtonsoft.Json.Linq.JObject"
itemAfterSave["Supplier"].GetType() == typeof(Newtonsoft.Json.Linq.JObject); //false
```

_Additionally, with our production stack, we are seeing different behaviour with how CosmosDB is serialising DynamicObjects when compared to running in a WebAPI with binding redirects to force json.net to 10.0.3_
