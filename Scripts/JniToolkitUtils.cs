using JetBrains.Annotations;

namespace DeadMosquito.JniToolkit
{
	using System;
	using System.Runtime.ExceptionServices;
	using System.Threading;
	using UnityEngine;

	[PublicAPI]
	public static class JniToolkitUtils
	{
		const string JavaLangSystemClass = "java.lang.System";
		const string UnityPlayerClass = "com.unity3d.player.UnityPlayer";

		static readonly object Lock = new object();
		static AndroidJavaClass _unityPlayerClass;
		static AndroidJavaObject _activity;
		static AndroidJavaObject _mainLooper;

		/// <summary>
		/// How long a blocking call on the UI thread (<see cref="RunOnUiThreadBlocking{T}"/>, MainThreadCall*) waits before giving up.
		/// The timeout prevents a deadlock when the UI thread itself waits for the Unity thread (e.g. while the activity is being paused).
		/// </summary>
		public static int UiThreadCallTimeoutMilliseconds { get; set; } = 2000;

		public static bool IsAndroidRuntime => Application.platform == RuntimePlatform.Android;

		public static bool IsNotAndroidRuntime => !IsAndroidRuntime;

		/// <summary>
		/// The current activity (<c>UnityPlayer.currentActivity</c>). It is re-read on every access, so the value is valid after the activity is recreated;
		/// the same <see cref="AndroidJavaObject"/> is returned while the activity doesn't change. Don't dispose it.
		/// </summary>
		public static AndroidJavaObject Activity
		{
			get
			{
				lock (Lock)
				{
					_unityPlayerClass ??= new AndroidJavaClass(UnityPlayerClass);
					var current = _unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
					if (current == null)
					{
						return _activity;
					}

					if (_activity != null && AndroidJNI.IsSameObject(_activity.GetRawObject(), current.GetRawObject()))
					{
						current.Dispose();
						return _activity;
					}

					// The previous activity object may still be used by callers, it is released by the GC
					_activity = current;
					return _activity;
				}
			}
		}

		/// <summary>
		/// Whether the calling thread is the Android UI (main) thread.
		/// </summary>
		public static bool IsOnUiThread
		{
			get
			{
				AndroidJavaObject mainLooper;
				lock (Lock)
				{
					if (_mainLooper == null)
					{
						using var looperClass = new AndroidJavaClass("android.os.Looper");
						_mainLooper = looperClass.CallStatic<AndroidJavaObject>("getMainLooper");
					}

					mainLooper = _mainLooper;
				}

				return mainLooper.Call<bool>("isCurrentThread");
			}
		}

		public static AndroidJavaObject ActivityDecorView
		{
			get
			{
				using var window = Activity.Call<AndroidJavaObject>("getWindow");
				return window.Call<AndroidJavaObject>("getDecorView");
			}
		}

		public static AndroidJavaObject PackageManager => Activity.CallAJO("getPackageManager");

		public static AndroidJavaObject ContentResolver => Activity.CallAJO("getContentResolver");

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
			return ajo == null || ajo.GetRawObject() == IntPtr.Zero;
		}

		#endregion

		/// <summary>
		/// Posts the action to the Android UI thread. Exceptions thrown by the action are logged instead of crashing the app.
		/// </summary>
		public static void RunOnUiThread(Action action)
		{
			Activity.Call(
				"runOnUiThread",
				new AndroidJavaRunnable(() =>
				{
					try
					{
						action();
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				})
			);
		}

		/// <summary>
		/// Runs the function on the Android UI thread and waits for the result (directly if already on the UI thread).
		/// Exceptions thrown by the function are rethrown on the calling thread.
		/// If the UI thread doesn't start the function within <see cref="UiThreadCallTimeoutMilliseconds"/> (e.g. it waits for the Unity thread while
		/// the activity is being paused) the call is cancelled - it will not run later - and a <see cref="TimeoutException"/> is thrown.
		/// If the function already started but didn't finish in time, a <see cref="TimeoutException"/> is thrown too and its late result is disposed.
		/// </summary>
		/// <exception cref="TimeoutException">The UI thread didn't run the function in time.</exception>
		public static T RunOnUiThreadBlocking<T>(Func<T> func)
		{
			if (IsOnUiThread)
			{
				return func();
			}

			var call = new BlockingCall<T>();
			RunOnUiThread(() => call.Run(func));

			if (!call.Wait(UiThreadCallTimeoutMilliseconds, out var started))
			{
				var message = started
					? $"[JniToolkit] The UI thread call didn't finish within {UiThreadCallTimeoutMilliseconds} ms, its result will be discarded."
					: $"[JniToolkit] The UI thread didn't run the call within {UiThreadCallTimeoutMilliseconds} ms (is it waiting for the Unity thread?), the call was cancelled.";
				throw new TimeoutException(message);
			}

			if (call.Error != null)
			{
				ExceptionDispatchInfo.Capture(call.Error).Throw();
			}

			return call.Result;
		}

		sealed class BlockingCall<T>
		{
			readonly object _lock = new object();
			// Not disposed on purpose: after a timeout the runnable may still signal it
			readonly ManualResetEventSlim _done = new ManualResetEventSlim(false);
			bool _timedOut;
			bool _started;

			public T Result { get; private set; }

			public Exception Error { get; private set; }

			public void Run(Func<T> func)
			{
				lock (_lock)
				{
					if (_timedOut)
					{
						return; // the caller gave up, don't run the call at all
					}

					_started = true;
				}

				var result = default(T);
				Exception error = null;
				try
				{
					result = func();
				}
				catch (Exception e)
				{
					error = e;
				}

				lock (_lock)
				{
					if (_timedOut)
					{
						(result as IDisposable)?.Dispose();
						if (error != null)
						{
							Debug.LogException(error);
						}
					}
					else
					{
						Result = result;
						Error = error;
					}

					_done.Set();
				}
			}

			public bool Wait(int timeoutMilliseconds, out bool started)
			{
				_done.Wait(timeoutMilliseconds);
				lock (_lock)
				{
					started = _started;
					if (_done.IsSet)
					{
						return true;
					}

					_timedOut = true;
					return false;
				}
			}
		}

		/// <summary>
		/// Runs the action on the Android UI thread and waits for it to complete. See <see cref="RunOnUiThreadBlocking{T}"/>.
		/// </summary>
		public static void RunOnUiThreadBlocking(Action action)
		{
			RunOnUiThreadBlocking(() =>
			{
				action();
				return true;
			});
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
				using var intentClass = new AndroidJavaClass("android.content.Intent");
				using var jChooser = intentClass.CallStaticAJO("createChooser", intent, chooserTitle);
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
