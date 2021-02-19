# SharedCounter class

A thread-safe counter that can be shared in-memory by actors.

```csharp
public class SharedCounter
```

## Public Members

| name | description |
| --- | --- |
| static [Create](SharedCounter/Create.md)(…) | Creates a new shared counter. |
| virtual [Add](SharedCounter/Add.md)(…) | Adds a value to the counter atomically. |
| virtual [CompareExchange](SharedCounter/CompareExchange.md)(…) | Sets the counter to a value atomically if it is equal to a given value. |
| virtual [Decrement](SharedCounter/Decrement.md)() | Decrements the shared counter. |
| virtual [Exchange](SharedCounter/Exchange.md)(…) | Sets the counter to a value atomically. |
| virtual [GetValue](SharedCounter/GetValue.md)() | Gets the current value of the shared counter. |
| virtual [Increment](SharedCounter/Increment.md)() | Increments the shared counter. |

## Remarks

See also [Sharing Objects](/coyote/advanced-topics/actors/sharing-objects).

## See Also

* namespace [Microsoft.Coyote.Actors.SharedObjects](../Microsoft.Coyote.Actors.SharedObjectsNamespace.md)
* assembly [Microsoft.Coyote](../Microsoft.Coyote.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->