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
}

void MAGE_free (void* ptr) {
	free(ptr);
}

mage::RPC* MAGE_RPC_Connect (const char* mageApplication,
                             const char* mageDomain,
                             const char* mageProtocol) {
    mage::RPC* client = new mage::RPC(mageApplication, mageDomain, mageProtocol);
    return client;
}

void MAGE_RPC_Disconnect(mage::RPC* client) {
    delete client;
}

const char* MAGE_RPC_Call(mage::RPC* client,
                    const char* methodName,
                    const char* strParams,
                    int* code) {
	Json::Reader reader;
    Json::Value params;
    if (!reader.parse(strParams, params)) {
		*code = -3;
		return NULL;
	}
	std::string str;

	*code = 0;

	try {
		Json::Value result = client->Call(methodName, params);
		Json::FastWriter writer;
		str = writer.write(result);
	} catch (mage::MageRPCError e) {
		str = e.what();
		*code = e.code();
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

