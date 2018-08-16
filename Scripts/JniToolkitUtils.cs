using JetBrains.Annotations;

namespace NinevaStudios.AwarenessApi
{
	using System;
	using UnityEngine;

	[PublicAPI]
	public static class JniToolkitUtils
	{
		const string JavaLangSystemClass = "java.lang.System";

		static AndroidJavaObject _activity;

		public static bool IsAndroidRuntime
		{
			get { return Application.platform == RuntimePlatform.Android; }
		}

		public static bool IsNotAndroidRuntime
		{
			get { return !IsAndroidRuntime; }
		}

		public static AndroidJavaObject Activity
		{
			get
			{
				if (_activity == null)
				{
					var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
					_activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				}

				return _activity;
			}
		}

		public static AndroidJavaObject ActivityDecorView
		{
			get { return Activity.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView"); }
		}

		public static AndroidJavaObject PackageManager
		{
			get { return Activity.CallAJO("getPackageManager"); }
		}

		public static AndroidJavaObject ContentResolver
		{
			get { return Activity.CallAJO("getContentResolver"); }
		}

		public static bool HasSystemFeature(string feature)
		{
			using (var pm = PackageManager)
			{
				return pm.CallBool("hasSystemFeature", feature);
			}
		}

		public static long CurrentTimeMillis
		{
			get
			{
				using (var system = new AndroidJavaClass(JavaLangSystemClass))
				{
					return system.CallStaticLong("currentTimeMillis");
				}
			}
		}

		#region reflection

		public static AndroidJavaObject ClassForName(string className)
		{
			using (var clazz = new AndroidJavaClass("java.lang.Class"))
			{
				return clazz.CallStaticAJO("forName", className);
			}
		}

		public static AndroidJavaObject Cast(this AndroidJavaObject source, string destClass)
		{
			using (var destClassAJC = ClassForName(destClass))
			{
				return destClassAJC.Call<AndroidJavaObject>("cast", source);
			}
		}

		public static bool IsJavaNull(this AndroidJavaObject ajo)
		{
			return ajo == null || ajo.GetRawObject().ToInt32() == 0;
		}

		#endregion

		public static void RunOnUiThread(Action action)
		{
			Activity.Call("runOnUiThread", new AndroidJavaRunnable(action));
		}

		public static void StartActivity(AndroidJavaObject intent, Action fallback = null)
		{
			try
			{
				Activity.Call("startActivity", intent);
			}
			catch (AndroidJavaException exception)
			{
				if (Debug.isDebugBuild)
				{
					Debug.LogWarning("Could not start the activity with " + intent.JavaToString() + ": " + exception.Message);
				}

				if (fallback != null)
				{
					fallback();
				}
			}
			finally
			{
				intent.Dispose();
			}
		}

		public static void StartActivityWithChooser(AndroidJavaObject intent, string chooserTitle)
		{
			try
			{
				AndroidJavaObject jChooser = intent.CallStaticAJO("createChooser", intent, chooserTitle);
				Activity.Call("startActivity", jChooser);
			}
			catch (AndroidJavaException exception)
			{
				Debug.LogWarning("Could not start the activity with " + intent.JavaToString() + ": " + exception.Message);
			}
			finally
			{
				intent.Dispose();
			}
		}

		public static void SendBroadcast(AndroidJavaObject intent)
		{
			Activity.Call("sendBroadcast", intent);
		}
	}
}