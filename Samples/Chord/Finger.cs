using Microsoft.Coyote;

namespace Chord
{
    public class Finger
    {
        public int Start;
        public int End;
        public MachineId Node;

        public Finger(int start, int end, MachineId node)
        {
            this.Start = start;
            this.End = end;
            this.Node = node;
        }
    }
}
