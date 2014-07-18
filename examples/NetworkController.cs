using UnityEngine;
using System;

public class NetworkController : MonoBehaviour {

    private MAGE.RPC client;
    private bool isAuthenticated = false;

    private static string extractSessionKey(JSONObject result) {
        if (!result.HasField("response")) {
            return null;
        }

        JSONObject response = result.GetField("response");
        if (!response.IsObject || !response.HasField("session")) {
            return null;
        }

        JSONObject session = response.GetField("session");
        if (!session.IsObject || !session.HasField("key")) {
            return null;
        }

        JSONObject key = session.GetField("key");
        if (!key.IsString) {
            return null;
        }

        return key.str;
    }

    private void Login(Action callback) {
        if (isAuthenticated) {
            callback();
            return;
        }

        // Build the parameters for the login command
        JSONObject parameters = new JSONObject(JSONObject.Type.OBJECT);
        parameters.AddField("engineName", "anonymous");
        parameters.AddField("credentials", new JSONObject(JSONObject.Type.NULL));
        
        JSONObject options = new JSONObject(JSONObject.Type.OBJECT);
        options.AddField("access", "user");
        
        parameters.AddField("options", options);

        client.Call("ident.login", parameters, (error, result) => {
            if (error != null) {
                Debug.Log (error.Message);
                return;
            }

            if (result == null) {
                return;
            }
            
            JSONObject parsedResult = new JSONObject(result);
            string sessionKey = extractSessionKey(parsedResult);
            if (sessionKey == null) {
                return;
            }
            
            client.SetSession(sessionKey);
            isAuthenticated = true;
            callback();
        });
    }

    void Start () {
        try {
            client = new MAGE.RPC("game", "127.0.0.1:8080");
        } catch (ApplicationException e) {
            Debug.Log (e.Message);
            return;
        }

        Login (() => {
            JSONObject parameters = new JSONObject(JSONObject.Type.OBJECT);
            client.Call ("mymodule.mycommand", parameters, (error, result) => {
                if (error != null) {
                    Debug.Log (error.Message);
                    return;
                }

                Debug.Log("Result: " + result);
            });
        });

        client.Disconnect();
	}
}

