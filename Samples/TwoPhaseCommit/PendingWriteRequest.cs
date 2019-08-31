using Microsoft.Coyote;

namespace TwoPhaseCommit
{
    internal class PendingWriteRequest
    {
        public MachineId Client;
        public int SeqNum;
        public int Idx;
        public int Val;

        public PendingWriteRequest(int seqNum, int idx, int val)
        {
            this.SeqNum = seqNum;
            this.Idx = idx;
            this.Val = val;
        }

        public PendingWriteRequest(MachineId client, int idx, int val)
        {
            this.Client = client;
            this.Idx = idx;
            this.Val = val;
        }
    }
}
