#include <iostream>
#include <mage.h>

extern "C" {
    void MAGE_free (void* ptr);

    
    mage::RPC* MAGE_RPC_Connect (const char* mageApplication,
                                 const char* mageDomain,
                                 const char* mageProtocol);
    void MAGE_RPC_Disconnect(mage::RPC* client);
    const char* MAGE_RPC_Call(mage::RPC* client,
                        const char* methodName,
                        const char* params,
                        int* code);
    void MAGE_RPC_SetSession(mage::RPC* client, const char* sessionKey);
    void MAGE_RPC_ClearSession(mage::RPC* client);
    void MAGE_RPC_PullEvents(mage::RPC* client, int transport);
}

void MAGE_free (void* ptr) {
    free(ptr);
}

class UnityEventObserver : public mage::EventObserver {
    public:
        explicit UnityEventObserver(mage::RPC* client) : m_pClient(client) {}
        virtual void ReceiveEvent(const std::string& name,
                                  const Json::Value& data = Json::Value::null) const {
            Json::Value event;
            event["name"] = name;
            event["data"] = data;
            Json::FastWriter writer;
            std::string str = writer.write(event);
            UnitySendMessage("Network", "ReceiveEvent", str.c_str());

            if (name == "session.set") {
                HandleSessionSet(data);
            }
        }

        void HandleSessionSet(const Json::Value& data) const {
            m_pClient->SetSession(data["key"].asString());
        }

    private:
        mage::RPC* m_pClient;
};

mage::RPC* MAGE_RPC_Connect (const char* mageApplication,
                             const char* mageDomain,
                             const char* mageProtocol) {
    mage::RPC* client = new mage::RPC(mageApplication, mageDomain, mageProtocol);

    UnityEventObserver *eventObserver = new UnityEventObserver(client);
    client->AddObserver(eventObserver);

    return client;
}

void MAGE_RPC_Disconnect(mage::RPC* client) {
    std::list<mage::EventObserver*> observers = client->GetObservers();
    std::list<mage::EventObserver*>::const_iterator citr;
    for (citr = observers.begin(); citr != observers.end(); ++citr) {
        delete (*citr);
    }

    delete client;
}

const char* MAGE_RPC_Call(mage::RPC* client,
                    const char* methodName,
                    const char* strParams,
                    int* code) {
    Json::Reader reader;
    Json::Value params;
    if (!reader.parse(strParams, params)) {
        *code = -4;
        return NULL;
    }
    std::string str;

    *code = 0;

    try {
        Json::Value result = client->Call(methodName, params);
        Json::FastWriter writer;
        str = writer.write(result);
    } catch (mage::MageRPCError e) {
        str = e.code() + " - " + e.what();
        *code = -3;
    } catch (mage::MageErrorMessage e) {
        str = e.code() + " - " + e.what();
        *code = -2;
    } catch (...) {
        *code = -1;
        return NULL;
    }

    char* out = (char*)malloc((str.length() + 1) * sizeof(char));
    strncpy(out, str.c_str(), str.length() + 1);
    return out;
}

void MAGE_RPC_SetSession(mage::RPC* client, const char* sessionKey) {
    client->SetSession(sessionKey);
}

void MAGE_RPC_ClearSession(mage::RPC* client) {
    client->ClearSession();
}

void MAGE_RPC_PullEvents(mage::RPC* client, int transport) {
    client->PullEvents((mage::Transport)transport);
}

