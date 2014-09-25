import os
from sys import argv
from mod_pbxproj import XcodeProject

path = argv[1]

print('----------------------------------prepare for excuting our magic scripts to tweak our xcode ----------------------------------')
print('post_process.py xcode build path --> ' + path)

print('Step 1: start add libraries ')
project = XcodeProject.Load(path +'/Unity-iPhone.xcodeproj/project.pbxproj')
project.add_file('System/Library/Frameworks/Security.framework', tree='SDKROOT')
project.add_file('System/Library/Frameworks/AdSupport.framework', tree='SDKROOT', weak=True)

print('Step2: add search paths')
project.add_header_search_paths([
	"MAGE_SDK_CPP_PATH/src",
	"MAGE_SDK_CPP_PATH/vendor/libjson-rpc-cpp/src",
	"MAGE_SDK_CPP_PATH/platforms/externals/curl/include/ios"
])

print('Step3: add custom flags')
project.add_other_cflags([
	"-DHTTP_CONNECTOR=YES",
	"-DUNITY=YES"
])

print('Step4: enable C++ exceptions')
project.add_other_buildsetting('GCC_ENABLE_CPP_EXCEPTIONS', 'YES')

if project.modified:
  project.backup()
  project.saveFormat3_2()

print('----------------------------------end for excuting our magic scripts to tweak our xcode ----------------------------------')

