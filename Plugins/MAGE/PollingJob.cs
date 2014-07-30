using System;

namespace MAGE {
    public class PollingJob : ThreadedJob {
        private RPC client;

        public PollingJob(RPC _client) {
            client = _client;
        }

        protected override void Run() {
            while (true) {
                try {
                    client.PullEvents(Transport.LONGPOLLING);
                } catch (ApplicationException error) {
                    Console.WriteLine(error.ToString());
                }
            }
        }
    }
}
