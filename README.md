# ContractOnlyStoredProcedures
[![Build status](https://ci.appveyor.com/api/projects/status/yp49ar9h9c2kgqn1/branch/master?svg=true)](https://ci.appveyor.com/project/abe545/contractonlystoredprocedures/branch/master)
[![Stable](https://img.shields.io/nuget/v/ContractOnlyStoredProcedures.svg)](https://www.nuget.org/packages/ContractOnlyStoredProcedures/)
[![Pre](https://img.shields.io/nuget/vpre/ContractOnlyStoredProcedures.svg)](https://www.nuget.org/packages/ContractOnlyStoredProcedures/)
[![NuGet](https://img.shields.io/nuget/dt/ContractOnlyStoredProcedures.svg)](https://www.nuget.org/packages/ContractOnlyStoredProcedures/)

A library for easily calling Stored Procedures in .NET. It automatically generates proxies to stored procedures based on an interface. It uses
[CodeOnlyStoredProcedures](//github.com/abe545/CodeOnlyStoredProcedures) to execute the stored procedures. As a consequence, most of the
features in that library can be used by this one.

This library is released via NuGet. You can go to the [ContractOnlyStoredProcedures NuGet Page](https://www.nuget.org/packages/ContractOnlyStoredProcedures) for more information on releases, or just grab it:

```
Install-Package ContractOnlyStoredProcedures
```

## Getting Started
All you need is a database proxy:
```cs
public interface IDatabase
{
    int usp_GetId(string name);
    IEnumerable<string> usp_GetNames();
}
```

Then you can call the methods like so:
```cs
using CodeOnlyStoredProcedure;

public static void Main()
{
    // create your connection however you normally do it
    IDbConnection connection = ...;
    var proxy = connection.GenerateProxy<IDatabase>();
    
    // you can now call the methods, and they will execute the stored procedures.
    var names = proxy.usp_GetNames();
    foreach (var n in names)
    {
        Console.WriteLine("\"{0}\"'s id: {1}", n, proxy.usp_GetId(n));
    }
}
```

## Async operations
We automatically strip `Async` off any method that returns a task:

```cs
// these call the same sproc
void usp_DoIt();
Task usp_DoItAsync();

// as do these
IEnumerable<string> GetNames();
Task<IEnumerable<string>> GetNamesAsync();
```

## Return Values
You can get a return value by specifying a method that returns an int
```cs
int CreateId();
Task<int> CreateIdAsync();
``` 

Or by having an out paramter named returnValue (case insensitive)
```cs
void CreateId(out int returnValue);
```

## Output Values
You can get an output value by declaring it with the out keyword.
```cs
void GetId(string name, out int id);
```
*Output values do not work in async contexts, as the method can return control to the caller before the value is returned from 
the stored procedure. As a result, attempting to do generate a proxy with a method like the following will throw an exception.*
```cs
// This is completely valid C#, but since we're generating a proxy to a database, we can't actually know what the id is before
// the task is completed. This will not be able to generate a valid proxy, so we won't attempt to.
Task GetIdAsync(string name, out int id);
```

## Input/Output values
Just like output values, you can use input/output parameters by specifying them as ref parameters:
```cs
// This can not be an async method, either
void MutateThis(ref int inOut);
```