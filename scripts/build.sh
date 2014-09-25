#!/bin/bash

usage(){
    echo -e "\033[1mUsage\033[0m: $0 project_directory"
    echo ""
    echo -e -n "\033[1mproject direcory\033[0m is the directory which "
    echo "contains your Unity project."
    echo ""
    echo -e -n "You can use the \033[1mMAGE_SDK_CPP_PATH\033[0m environment "
    echo "variable to specify the path of "
    echo "the mage-sdk-cpp library if you already have it."
    exit 1
}

if [ -z "${1}" ]; then
    usage
fi

project_directory=${1}

if [ -z "${MAGE_SDK_CPP_PATH}" ]; then
  MAGE_SDK_CPP_PATH="vendor/mage-sdk-cpp"
fi

if [ ! -e "${MAGE_SDK_CPP_PATH}" ]; then
    mkdir -p vendor
    git clone https://github.com/mage/mage-sdk-cpp.git "${MAGE_SDK_CPP_PATH}"
    pushd "${MAGE_SDK_CPP_PATH}" >/dev/null
else
    pushd "${MAGE_SDK_CPP_PATH}" >/dev/null
    git pull
fi

git submodule update --init

make ios-unity
make android-unity

popd >/dev/null

mkdir -p Plugins/iOS
mkdir -p Plugins/Android

cp "${MAGE_SDK_CPP_PATH}"/platforms/ios/build/UnityRelease-iphoneos/libmage-sdk.a Plugins/iOS/
cp "${MAGE_SDK_CPP_PATH}"/platforms/android-unity/libs/armeabi/libmage.so Plugins/Android/

mkdir -p "${project_directory}/Assets/Editor"
mkdir -p "${project_directory}/Assets/Plugins"

for file in Editor/*.{cs,py}; do
    cp ${file} "${project_directory}/Assets/Editor/"
done

sed "s#MAGE_SDK_CPP_PATH#${MAGE_SDK_CPP_PATH}#g" Editor/post_process.py.tpl > "${project_directory}/Assets/Editor/post_process.py"

cp -R Plugins/ "${project_directory}/Assets/Plugins/"

echo "MAGE SDK files copied to ${project_directory}."

exit 0

