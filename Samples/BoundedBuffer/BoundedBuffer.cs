// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// With thanks to Tom Cargill and
// http://wiki.c2.com/?ExtremeProgrammingChallengeFourteen

using System.Threading;

namespace Microsoft.Coyote.Samples.BoundedBuffer
{
    public class BoundedBuffer
    {
        private readonly object SyncObject = new object();
        private readonly object[] Buffer;
        private readonly bool PulseAll;
        private int PutAt;
        private int TakeAt;
        private int Occupied;

        public BoundedBuffer(int bufferSize, bool pulseAll = false)
        {
            this.PulseAll = pulseAll;
            this.Buffer = new object[bufferSize];
        }

        public void Put(object x)
        {
            lock (this.SyncObject)
            {
                while (this.Occupied == this.Buffer.Length)
                {
                    Monitor.Wait(this.SyncObject);
                }

                ++this.Occupied;
                this.PutAt %= this.Buffer.Length;
                this.Buffer[this.PutAt++] = x;
                this.Pulse();
            }
        }

        public object Take()
        {
            object result = null;
            lock (this.SyncObject)
            {
                while (this.Occupied == 0)
                {
                    Monitor.Wait(this.SyncObject);
                }

                --this.Occupied;
                this.TakeAt %= this.Buffer.Length;
                result = this.Buffer[this.TakeAt++];
                this.Pulse();
            }

            return result;
        }

        private void Pulse()
        {
            if (this.PulseAll)
            {
                Monitor.PulseAll(this.SyncObject);
            }
            else
            {
                Monitor.Pulse(this.SyncObject);
            }
        }
    }
}
