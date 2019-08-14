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

The functionality consists of lots of small granular methods that are intended to reduce the amount of boilerplate code written when using Unity JNI API.

* Acessing the Unity Activity. The object is lazy initialized and cached:

```csharp
_printHelper = new AndroidJavaObject(C.AndroidPrintHelper, AGUtils.Activity);
```

Lot's of the Android methods take `Context` object as a parameter, so you can pass `AGUtils.Activity` as Activity extends Context in Android

* Shortcut methods to avoid passing a type parameter all the time, also helps with autocomplete.

Some example code snippets:

```csharp
var key = iterator.CallStr("next"); // same as iterator.Call<string>("next")
```

```csharp
var result = new WifiInfo
{
  BSSID = wifiInfoAJO.CallStr("getBSSID"),
  SSID = wifiInfoAJO.CallStr("getSSID"),
  MacAddress = wifiInfoAJO.CallStr("getMacAddress"),
  LinkSpeed = wifiInfoAJO.CallInt("getLinkSpeed"),
  NetworkId = wifiInfoAJO.CallInt("getNetworkId"),
  IpAddress = wifiInfoAJO.CallInt("getIpAddress"),
  Rssi = wifiInfoAJO.CallInt("getRssi")
};
```

* Safe null checking

```csharp
if (networkInfo.IsJavaNull())
{
  return false;
}
```

* Calling mthods on the main (UI) thread

```csharp
_ajo.MainThreadCall("remove");
```

* Get various AJOs and other things

Like `ActivityDecorView`, `PackageManager`, `ContentResolver` etc. Check system features with `HasSystemFeature` method. Get current time in millis `CurrentTimeMillis` etc.

* Cast `AndroidJavaObject` to the class:

```csharp
static AndroidJavaObject GetSystemService(string name, string serviceClass)
{
  try
  {
    var serviceObj = AGUtils.Activity.CallAJO("getSystemService", name);
    return serviceObj.Cast(serviceClass);
  }
  catch (Exception e)
  {
    if (Debug.isDebugBuild)
    {
      Debug.LogWarning("Failed to get " + name + " service. Error: " + e.Message);
    }

    return null;
  }
}
```
