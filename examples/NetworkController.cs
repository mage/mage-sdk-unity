using UnityEngine;
using System;
using System.Collections;

/*
 * To receive the events from the server,
 * your NetworkController script should be attached to
 * a GameObject called Network.
 */
public class NetworkController : MonoBehaviour {

    public static NetworkController instance;

    private MAGE.RPC client = null;
    private bool isAuthenticated = false;

    void Awake() {
        if (instance) {
            DestroyImmediate(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(this);
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

            client.StartPolling();

            isAuthenticated = true;
            callback();
        });
    }

    void Call(string methodName,
             JSONObject parameters,
             Action<ApplicationException,string> callback) {
        Login (() => {
            client.Call(methodName, parameters, callback);
        });
    }

    void OnDestroy() {
        if (client != null) {
            client.Disconnect();
        }
    }

    void Start() {
        try {
            client = new MAGE.RPC("game", "127.0.0.1:8080");
        } catch (ApplicationException e) {
            Debug.Log (e.Message);
            return;
        }

        JSONObject parameters = new JSONObject(JSONObject.Type.OBJECT);
        Call ("mymodule.mycommand", parameters, (error, result) => {
            if (error != null) {
                Debug.Log (error.Message);
                return;
            }

            Debug.Log("Result: " + result);
        });
    }

    public void ReceiveEvent(string message) {
        JSONObject receivedEvent = new JSONObject(message);

        if (!receivedEvent.HasField("name") || !receivedEvent.HasField("data")) {
            Debug.LogError("The received event has an invalid format.");
            return;
        }

        string name = receivedEvent.GetField("name").str;
        JSONObject data = receivedEvent.GetField("data");

        Debug.Log ("Receive Event: " + name + " - " + data.Print(true));
    }
}
