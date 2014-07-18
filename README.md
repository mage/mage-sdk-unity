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

### Requirements

#### [mage-sdk-cpp](https://github.com/mage/mage-sdk-cpp)

This plugin uses the mage-sdk-cpp, you need to download the last version and build the library.

```bash
git clone https://github.com/mage/mage-sdk-cpp.git
cd mage-sdk-cpp
git submodule update --init
make ios-unity
```

You will obtain the following file: `platforms/ios/build/UnityRelease-iphoneos/libmage-sdk.a`.
You have to put it in the `Assets/Plugins/iOS` directory of your Unity project.

### Setup

```bash
git clone https://github.com/MiLk/mage-sdk-unity.git
cd mage-sdk-unity
```

### Integration in your Unity project

#### Plugin

Copy all the files from the `Plugin` directory to the `Assets/Plugin` directory of your project.

It contains the `MAGE` namespace that you can use in your project.

#### Editor

Copy all the files from the `Editor` directory to the `Assets/Editor` directory of your project.

It contains scripts to automate some steps of the build.

You have to edit the `Editor/post_process.py` file to use your path to the `mage-sdk-cpp` directory.

Usage
-----

See the [examples](./examples).


See also
---------

- [mage-sdk-cpp](https://github.com/mage/mage-sdk-cpp)
- [Unity](http://unity3d.com/)
- [UnityAutomatePostProcess](https://github.com/tuo/UnityAutomatePostProcess)

