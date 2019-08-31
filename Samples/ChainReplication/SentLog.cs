using Microsoft.Coyote;

namespace ChainReplication
{
    public class SentLog
    {
        public int NextSeqId;
        public MachineId Client;
        public int Key;
        public int Value;

        public SentLog(int nextSeqId, MachineId client, int key, int val)
        {
            this.NextSeqId = nextSeqId;
            this.Client = client;
            this.Key = key;
            this.Value = val;
        }
    }
}
