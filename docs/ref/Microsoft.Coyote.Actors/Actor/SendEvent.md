# Actor.SendEvent method

Sends an asynchronous [`Event`](../../Microsoft.Coyote/Event.md) to a target.

```csharp
protected void SendEvent(ActorId id, Event e, EventGroup eventGroup = null, 
    SendOptions options = null)
```

| parameter | description |
| --- | --- |
| id | The id of the target. |
| e | The event to send. |
| eventGroup | An optional event group associated with this Actor. |
| options | Optional configuration of a send operation. |

## See Also

* class [ActorId](../ActorId.md)
* class [Event](../../Microsoft.Coyote/Event.md)
* class [EventGroup](../EventGroup.md)
* class [SendOptions](../SendOptions.md)
* class [Actor](../Actor.md)
* namespace [Microsoft.Coyote.Actors](../Actor.md)
* assembly [Microsoft.Coyote](../../Microsoft.Coyote.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
