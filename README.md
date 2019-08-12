# Unity-Android JNI Toolkit

A collection of methods for Unity-Android Plugin development simplification. The library is production ready and used in the following Unity Asset Store products:
* [Android Native Goodies PRO](https://assetstore.unity.com/packages/tools/integration/android-native-goodies-pro-67473)
* [Google Maps View](https://www.assetstore.unity3d.com/#!/content/82542)

# Usage

These scripts are intended to be added as a submodule

```
git submodule add git@github.com:TarasOsiris/Unity-Android-JNI-Toolkit.git unity-proj/Assets/Scripts/JNI-Toolkit/
```

But you might as well can just copy the contents of the `Scripts` folder over to your project

# Functionality

## JNI Extension methods

* Shortcut methods to avoid passing a type parameter all the time, also helps with autocomplete:

```csharp
var key = iterator.CallStr("next"); // same as iterator.Call<string>("next")
```

* Call `void remove()` method on some `AndroidJavaObject` on the main thread

```csharp
_ajo.MainThreadCall("remove");
```
