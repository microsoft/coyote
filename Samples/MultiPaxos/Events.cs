using Microsoft.Coyote;

namespace MultiPaxos
{
    #region Events

    class local : Event { }
    class success : Event { }
    class goPropose : Event { }
    class response : Event { }

    #endregion
}
