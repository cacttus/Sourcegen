

# Sourcegen
WPF source generating application for projects.
Generates:
* Java classes
* C++ classes
* C++ H/CPP files
* Qt classes

Configurable with:
* name,
* copyright, 
* license, 
* cryptic ifguard

![Sourcegen Screenshot](https://github.com/metalmario971/Sourcegen/blob/master/sg_screenshot.png)

Automatic BSD license included.

# TODOs
Made an attempt to package Newtonsoft and Ookii into the application using ILMerge -  to create a single exe we can move around.
Failed, however I left the ILMerge .bat file sitting there in case we decide to do this in the future.  Until then,
you have to copy Newtonsoft.json.dll and Ooaki.wpf.dll along with the Sourcegen.exe to run it in different folders.
