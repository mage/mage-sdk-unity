namespace MAGE {
    public class ThreadedJob {
        private System.Threading.Thread m_Thread = null;

        public virtual void Start() {
            m_Thread = new System.Threading.Thread(Run);
            m_Thread.Start();
        }

        public virtual void Abort() {
            m_Thread.Abort();
        }

        protected virtual void Run() { }
    }
}
