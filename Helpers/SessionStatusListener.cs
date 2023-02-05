using fxcore2;

namespace AutoTrader.Helpers
{
    public class SessionStatusListener : IO2GSessionStatus
    {
        private O2GSession mSession;
        private object mEvent = new object();

        public bool Connected { get; private set; } = false;
        public bool Disconnected { get; private set; } = false;
        public bool Error { get; private set; } = false; 

        public SessionStatusListener(O2GSession session)
        {
            mSession = session;
        }

        public void onLoginFailed(string error)
        {
            Console.WriteLine("Login error: " + error);
            this.Error = true;
            lock (mEvent)
                Monitor.PulseAll(mEvent);
        }

        public void onSessionStatusChanged(O2GSessionStatusCode status)
        {
            if (status == O2GSessionStatusCode.Connected)
                Connected = true;
            else
                Connected = false;

            if (status == O2GSessionStatusCode.Disconnected)
                Disconnected = true;
            else
                Disconnected = false;

            if (status == O2GSessionStatusCode.TradingSessionRequested)
            {
                throw new NotImplementedException();
                /*
                if (Program.SessionID == "")
                    Console.WriteLine("Argument for trading session ID is missing");
                else
                    mSession.setTradingSession(sessionId, Program.Pin);
                */
            }
            else if (status == O2GSessionStatusCode.Connected)
            {
                lock (mEvent)
                    Monitor.PulseAll(mEvent);
            }
        }

    }
}
