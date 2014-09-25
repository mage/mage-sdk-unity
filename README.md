mage-sdk-cpp
============

![MAGE Logo](./img/logo.jpg)

What is this MAGE thing anyway?
-------------------------------

- English
	- [http://www.wizcorp.jp/#portfolio](http://www.wizcorp.jp/#portfolio)
- 日本語
	- [http://www.wizcorp.jp/#portfolio](http://www.wizcorp.jp/ja/#portfolio)
	- [http://www.spiralsense.jp/products/m-a-g-e/](http://www.spiralsense.jp/products/m-a-g-e/)

Description
------------

This is a C# library and a [Unity Native Plugin](http://docs.unity3d.com/Manual/Plugins.html)
that enables you to interact with a MAGE server.
More specifically, it allows you to call any user commands made available on a given server.

Installation
-------------

### Setup

```bash
git clone https://github.com/MiLk/mage-sdk-unity.git
cd mage-sdk-unity
```

### Build

A script is provided to help you to build the dependencies
and copy the files into your Unity project directory.

Run the following command:
```bash
./scripts/build.sh path/to/your/unity/project
```

#### [mage-sdk-cpp](https://github.com/mage/mage-sdk-cpp)

This plugin uses the mage-sdk-cpp, it will be downloaded by the build script.

If you already have it on your computer, you can use the `MAGE_SDK_CPP_PATH`
environment variable to specify the path to your installation.

```bash
export MAGE_SDK_CPP_PATH=path/to/mage-sdk-cpp
./scripts/build.sh path/to/your/unity/project
```

#### Integration in your Unity project

All the files from the `Editor` and `Plugins` directories will be copied.


Usage
-----

See the [examples](./examples).


See also
---------

- [mage-sdk-cpp](https://github.com/mage/mage-sdk-cpp)
- [Unity](http://unity3d.com/)
- [UnityAutomatePostProcess](https://github.com/tuo/UnityAutomatePostProcess)
- [JSONObject](http://wiki.unity3d.com/index.php?title=JSONObject)

