using System;
using System.Runtime.InteropServices;

namespace MAGE {

    public enum Transport {
        SHORTPOLLING = 0,
        LONGPOLLING
    };

    public class RPC {

        // Import from libraries
        #if UNITY_IPHONE
        [DllImport ("__Internal")]
        private static extern void MAGE_free (IntPtr ptr);
        
        [DllImport ("__Internal")]
        private static extern IntPtr MAGE_RPC_Connect(string mageApplication,
                                                      string mageDomain,
                                                      string mageProtocol);
        
        [DllImport ("__Internal")]
        private static extern void MAGE_RPC_Disconnect(IntPtr client);
        
        
        [DllImport ("__Internal")]
        private static extern IntPtr MAGE_RPC_Call(IntPtr client, string methodName, string parameters, IntPtr ok);
        
        [DllImport ("__Internal")]
        private static extern void MAGE_RPC_SetSession(IntPtr client, string sessionKey);
        
        [DllImport ("__Internal")]
        private static extern void MAGE_RPC_ClearSession(IntPtr client);

        [DllImport ("__Internal")]
        private static extern int MAGE_RPC_PullEvents(IntPtr client, int transport);
        
        #else
        private static void MAGE_free (IntPtr ptr) {}
        private static IntPtr MAGE_RPC_Connect(string mageApplication,
                                               string mageDomain,
                                               string mageProtocol) {
            return IntPtr.Zero;
        }
        private static void MAGE_RPC_Disconnect(IntPtr client) {}
        private static IntPtr MAGE_RPC_Call(IntPtr client, string methodName, string parameters, IntPtr ok) {
            return IntPtr.Zero;
        }
        private static void MAGE_RPC_SetSession(IntPtr client, string sessionKey) {}
        private static void MAGE_RPC_ClearSession(IntPtr client) {}
        private static void MAGE_RPC_PullEvents(IntPtr client, int transport) {}
        #endif

        // Attributes
        private IntPtr client;
        private PollingJob pollingJob = null;

        // Methods
        public RPC(string mageApplication,
                   string mageDomain = "localhost:8080",
                   string mageProtocol = "http") {
            client = MAGE_RPC_Connect(mageApplication, mageDomain, mageProtocol);
            if (client == IntPtr.Zero) {
                throw new ApplicationException("Unable to instantiate the RPC client.");
            }
        }

        public void Disconnect() {
            MAGE_RPC_Disconnect(client);
        }

        public string Call(string methodName, JSONObject parameters = null) {
            if (parameters == null) {
                parameters = new JSONObject(JSONObject.Type.OBJECT);
            } else if (parameters.type != JSONObject.Type.OBJECT) {
                throw new ApplicationException("Parameters should be an object.");
            }

            IntPtr pCode = Marshal.AllocCoTaskMem(4);
            IntPtr p = MAGE_RPC_Call(client, methodName, parameters.Print(), pCode);

            int[] code = new int[1];
            Marshal.Copy(pCode, code, 0, 1);
            Marshal.FreeCoTaskMem(pCode);

            // JSON parsing error
            if (code[0] == -4) {
                throw new ApplicationException("Unable to parse the parameters");
            }

            // Unexpected error
            if (code[0] == -1) {
                throw new ApplicationException("Unexpected error");
            }

            String result = Marshal.PtrToStringAuto(p);
            MAGE_free(p);

            // Success
            if (code[0] == 0) {
                return result;
            }

            // MAGE error
            if(code[0] == -2) {
                int index = result.IndexOf(" - ");
                throw new MageErrorMessage(result.Substring(0, index), result.Substring(index + 3));
            }

            // RPC error
            if(code[0] == -3) {
                int index = result.IndexOf(" - ");
                throw new MageRPCError(result.Substring(0, index), result.Substring(index + 3));
            }

            throw new ApplicationException("Unexpected error");
        }

        public void Call(string methodName,
                         JSONObject parameters,
                         Action<ApplicationException,string> callback) {
            String result = null;
            try {
                result = Call(methodName, parameters);
            } catch (ApplicationException e) {
                callback(e, null);
                return;
            }
            callback(null, result);
        }

        public void SetSession(string sessionKey) {
            MAGE_RPC_SetSession(client, sessionKey);
        }

        public void ClearSession() {
            MAGE_RPC_ClearSession(client);
        }

        public void PullEvents(Transport transport) {
            int errorCode = MAGE_RPC_PullEvents(client, (int)transport);
            switch (errorCode) {
            case 0:
                return;
            case -1:
                throw new ApplicationException("The MAGE plugin has encountered an unexpected error.");
            case -2:
                throw new ApplicationException("The MAGE plugin has encountered a connection error.");
            default:
                throw new ApplicationException("Unexpected exception.");
            }
        }

        public void StartPolling() {
            if (pollingJob != null) {
                return;
            }
            pollingJob = new PollingJob(this);
            pollingJob.Start();
        }

        public void StopPolling() {
            if (pollingJob == null) {
                return;
            }
            pollingJob.Abort();
            pollingJob = null;
        }
    }
}
