using System;

namespace MAGE {
    public class PollingJob : ThreadedJob {
        private RPC client;
        private bool stop = false;

        public PollingJob(RPC _client) {
            client = _client;
        }

        protected override void Run() {
            while (!stop) {
                try {
                    client.PullEvents(Transport.LONGPOLLING);
                } catch (ApplicationException error) {
                    Console.WriteLine(error.ToString());
                } catch (Exception error) {
                    Console.WriteLine(error.ToString());
                }
            }
        }

        public override void Abort() {
            stop = true;
        }
    }
}
