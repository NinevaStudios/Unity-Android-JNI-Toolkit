using UnityEngine;

namespace DeadMosquito.JniToolkit
{
	public static class BitmapUtils
	{
		const string AndroidGraphicsBitmapFactory = "android.graphics.BitmapFactory";
		
		public static AndroidJavaObject Texture2DToAndroidBitmap(this Texture2D tex2D, bool isPng = true)
		{
			var encoded = isPng ? tex2D.EncodeToPNG() : tex2D.EncodeToJPG();
			return AndroidGraphicsBitmapFactory.AJCCallStaticOnce<AndroidJavaObject>("decodeByteArray", encoded, 0, encoded.Length);
		}
	}
}