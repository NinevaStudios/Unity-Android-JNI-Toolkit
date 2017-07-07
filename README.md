# Unity-Android-JNI-Toolkit
A collection of method for Unity-Android Plugin development simplification.

# To add as submodule to subfolder

```
git submodule add git@github.com:TarasOsiris/Unity-Android-JNI-Toolkit.git unity-proj/Assets/Scripts/JNI-Toolkit/
```

# Functionality

## CallJniExtensionMethods

Provides functionality to easier call JNI methods and also execute calls on main Android thread blocking Unity thread while operation is not finished.

```csharp
var key = iterator.CallStr("next");
```

```csharp
_ajo.MainThreadCall("remove");
```
