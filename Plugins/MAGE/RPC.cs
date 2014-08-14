#if UNITY_IPHONE && !UNITY_EDITOR
#define MOBILE
#endif

using System;
using UnityEngine;
#if MOBILE
using System.Runtime.InteropServices;
#else
using System.Collections;
using System.Collections.Generic;
#endif

namespace MAGE {

    public enum Transport {
        SHORTPOLLING = 0,
        LONGPOLLING
    };

    public class RPC {

        // Import from libraries
        #if UNITY_IPHONE && !UNITY_EDITOR
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
        private static int MAGE_RPC_PullEvents(IntPtr client, int transport) {
            return 0;
        }
        #endif

        // Attributes
#if MOBILE
        private IntPtr client;
        private PollingJob pollingJob = null;
#else
        private string mageApplication;
        private string mageDomain;
        private string mageProtocol;
        private string sessionKey = null;
        private List<string> confirmIds;

        private bool shouldPoll = true;
#endif

        // Methods
        public RPC(string mageApplication,
                   string mageDomain = "localhost:8080",
                   string mageProtocol = "http") {
#if MOBILE
            client = MAGE_RPC_Connect(mageApplication, mageDomain, mageProtocol);
            if (client == IntPtr.Zero) {
                throw new ApplicationException("Unable to instantiate the RPC client.");
            }
#else
            this.mageApplication = mageApplication;
            this.mageDomain = mageDomain;
            this.mageProtocol = mageProtocol;

            confirmIds = new List<string>();
#endif
        }

#if !MOBILE
        private string GetUrl() {
            return mageProtocol + "://" + mageDomain + "/" + mageApplication + "/jsonrpc";
        }

        private string GetMsgStreamUrl(Transport transport) {
            string url = mageProtocol + "://" + mageDomain + "/msgstream?transport=";
            switch (transport) {
            case Transport.SHORTPOLLING:
                url += "shortpolling";
                break;
            case Transport.LONGPOLLING:
                url += "longpolling";
                break;
            default:
                throw new ApplicationException("Unsupported transport.");
            }

            if (sessionKey == null) {
                throw new ApplicationException("No session key registered.");
            }

            url += "&sessionKey=" + sessionKey;

            if (confirmIds.Count > 0) {
                url += "&confirmIds=";
                Boolean first = true;
                foreach (string id in confirmIds) {
                    url += id;
                    if (!first) {
                        url += ",";
                    } else {
                        first = false;
                    }
                }
                confirmIds.Clear();
            }

            return url;
        }
#endif

        public void Disconnect() {
#if MOBILE
            MAGE_RPC_Disconnect(client);
#endif
        }

#if MOBILE
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
#endif

        public void Call(string methodName,
                         JSONObject parameters,
                         Action<ApplicationException,JSONObject> callback) {
#if MOBILE
            String result = null;
            try {
                result = Call(methodName, parameters);
            } catch (ApplicationException e) {
                callback(e, null);
                return;
            }
            callback(null, new JSONObject(result));
#else
            JSONObject data = new JSONObject(JSONObject.Type.OBJECT);
            data.AddField("id", 1);
            data.AddField("jsonrpc", "2.0");
            data.AddField("method", methodName);
            data.AddField("params", parameters);

            HTTP.Request httpRequest = new HTTP.Request( "post", GetUrl(), data );
            if (sessionKey != null) {
                httpRequest.AddHeader("X-MAGE-SESSION", sessionKey);
            }
            httpRequest.Send( ( request ) => {
                JSONObject result = request.response.Json;
                if (result.IsNull) {
                    callback(new ApplicationException("Could not parse JSON response."), null);
                    return;
                }

                if (!result.HasField("result")) {
                    callback(new ApplicationException("No result field in the response."), null);
                    return;
                }

                if (result.GetField("result").HasField("errorCode")) {
                    JSONObject errorCode = result.GetField("result").GetField("errorCode");
                    callback(new MageErrorMessage(errorCode.str), null);
                    return;
                }

                if (result.GetField("result").HasField("myEvents")) {
                    ExtractEventsFromCommandResponse(result.GetField("result").GetField("myEvents"));
                }

                if (!result.GetField("result").HasField("response")) {
                    callback(null, null);
                    return;
                }

                callback(null, result.GetField("result").GetField("response"));
            });
#endif
        }

#if !MOBILE
        public void ExtractEventsFromCommandResponse(JSONObject events) {
            foreach(JSONObject evt in events.list) {
                JSONObject parsedEvent = new JSONObject(evt.str.Replace("\\\"","\""));

                JSONObject eventToSend = new JSONObject(JSONObject.Type.OBJECT);
                switch (parsedEvent.list.Count) {
                case 1:
                    eventToSend.AddField("name", parsedEvent.list[0]);
                    eventToSend.AddField("data", new JSONObject(JSONObject.Type.NULL));
                    break;
                case 2:
                    eventToSend.AddField("name", parsedEvent.list[0]);
                    eventToSend.AddField("data", parsedEvent.list[1]);
                    break;
                default:
                    throw new ApplicationException("One of the received events has an invalid format.");
                }

                if (eventToSend["name"].str == "session.set") {
                    SetSession(eventToSend["data"]["key"].str);
                    return;
                }

                GameObject.Find("Network").SendMessage("ReceiveEvent", eventToSend.Print());
            }
        }
#endif

        public void SetSession(string sessionKey) {
#if MOBILE
            MAGE_RPC_SetSession(client, sessionKey);
#else
            this.sessionKey = sessionKey;
#endif
        }

        public void ClearSession() {
            StopPolling();
#if MOBILE
            MAGE_RPC_ClearSession(client);
#else
            this.sessionKey = null;
#endif
        }

#if MOBILE
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
#else
        public void PullEvents(Transport transport, Action callback = null) {
            HTTP.Request httpRequest = new HTTP.Request("get", GetMsgStreamUrl(transport));
            httpRequest.Send((request) => {
                JSONObject obj = request.response.Json;

                if (obj.IsNull) {
                    callback();
                    return;
                }

                for(int i = 0; i < obj.list.Count; i++){
                    string msgId = (string)obj.keys[i];
                    JSONObject events = (JSONObject)obj.list[i];
                    foreach (JSONObject evt in events.list) {

                        JSONObject eventToSend = new JSONObject(JSONObject.Type.OBJECT);
                        switch (evt.list.Count) {
                        case 1:
                            eventToSend.AddField("name", evt.list[0]);
                            eventToSend.AddField("data", new JSONObject(JSONObject.Type.NULL));
                            break;
                        case 2:
                            eventToSend.AddField("name", evt.list[0]);
                            eventToSend.AddField("data", evt.list[1]);
                            break;
                        default:
                            throw new ApplicationException("One of the received events has an invalid format.");
                        }
                        GameObject.Find("Network").SendMessage("ReceiveEvent", eventToSend.Print());
                    }
                    confirmIds.Add(msgId);
                }

                callback();
            });
        }
#endif

#if !MOBILE
        private void Poll() {
            if (!shouldPoll) {
                return;
            }

            PullEvents(Transport.LONGPOLLING, () => {
                Poll ();
            });
        }
#endif

        public void StartPolling() {
#if MOBILE
            if (pollingJob != null) {
                return;
            }
            pollingJob = new PollingJob(this);
            pollingJob.Start();
#else
            if (shouldPoll) {
                return;
            }

            shouldPoll = true;
            Poll ();
#endif
        }

        public void StopPolling() {
#if MOBILE
            if (pollingJob == null) {
                return;
            }
            pollingJob.Abort();
            pollingJob = null;
#else
            shouldPoll = false;
#endif
        }
    }
}
