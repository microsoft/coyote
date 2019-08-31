namespace Raft
{
    internal class Log
    {
        public readonly int Term;
        public readonly int Command;

        public Log(int term, int command)
        {
            this.Term = term;
            this.Command = command;
        }
    }
}
