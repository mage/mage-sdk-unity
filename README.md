MAGE Unity SDK
==============


Internal Dependencies
=====================

Async
-----
This is a C# implementation of the Async library [caolan/async](https://github.com/caolan/async). Though .Net does have
Asynchronous methods and paradigms, none of these are usable in Unity due to the fairly old version of mono. This along
with the need for familiar APIs for NodeJS developers brought about the need for this library.
Asyncライブラリーの[caolan/async](https://github.com/caolan/async)C#での実装です。　.Netに元にあるAsync方式はMonoのバージョンが古いの理油でUnityで使えません。
加えて、Node.jsの開発者は慣れやすいライブラリーの必要性があるから、こちらのライブラリーを使っています。

EventEmitter
------------
This is a wrapper around C# eventing to provide the familiar EventEmitter class and APIs for NodeJS developers.
NodeJS開発者用のEventEmitterのようなクラスのイベントラッパー(C#で実装)。

HTTPRequest
-----------
This is a simple wrapper around HTTPWebRequest which provides simple GET / POST API with callbacks.
HTTPWebRequestの上で、GET / POSTコールバックが提供するWrapperです。

JSONRPC
-------
JSONRPC is a lightweight JSON RPC library which leverages the .Net `HttpWebRequest` object along with Newtonsoft's JSON
library. Due to the use of `HttpWebRequest` a Unity Pro license is required to build for IOS and Android. However this
will build just fine in the IDE and for desktop applications.
JSONRPCというのは　.Net `HttpWebRequest`オブジェックト（Newtonsoft's JSONライブラリーに沿って）を使用する軽いJSON RPCライブラリーです。
`HttpWebRequest`が使われているため、IOSとAndroidのブイルドを作る為に、Unity Pro ライセンスが必要です。ですが、デスクトップアップとIDEのブイルドを作るために、Unity Proライセンスが必要ではありません。

Newtonsoft.Json.dll
-------------------
This is the one of the most famous and renowned JSON libraries for C#. However due to the fact that Unity's mono version
is severely outdated, many features are not available. (we tried to look for documentation, but could not find anything
on what is and isn't available to us)
C#での有名なJSONライブラリーです。ですが、UnityのMonoバージョンが古くて複数の機能は使えない状態です。（色々仕様書で調べましたが、何かが使えるか何か使えないかの情報はありませんでした。）

Singleton
---------
This is a collection of lightweight classes that can be inherited to add singleton behaviours to your own classes.
自分のクラスにシングルトン行動を追加が出来る為にこちらの軽いクラスのコレクションを使えます。

Tomes
-----
This is a C# implementation of node-tomes which can be found [here](https://github.com/Wizcorp/node-tomes)
node-tomes [詳細](https://github.com/Wizcorp/node-tomes)のC#バージョンです。