using System.Threading;
using FireTime.Response;

namespace FireTime.Private
{
    internal class ReConnector
    {
        private bool HasDestroyed = false;
        private Timer AliveChecker { get; set; }
        private StreamResponse Caller { get; set; }

        private const int CheckDelay = (300 + 5) * 1000;

        internal ReConnector(StreamResponse _SResp)
        {
            Caller = _SResp;
            AliveChecker = new Timer
                (ReConnect, null, -1, -1);
        }

        private void ReConnect(object state)
        {
            if (HasDestroyed) return;
            AliveChecker.Change(-1, -1);
            Caller.StartDetection();
        }

        internal void ReStartDetection()
            => AliveChecker.Change(CheckDelay, -1);

        internal void Destroy()
        {
            HasDestroyed = true;
            AliveChecker.Dispose();
        }
    }
}
