using JetBrains.Annotations;
using UnityEngine;

namespace NinevaStudios.AwarenessApi
{
	[PublicAPI]
	public static class BitmapUtils
	{
		const string AndroidGraphicsBitmapFactory = "android.graphics.BitmapFactory";
		
		public static AndroidJavaObject Texture2DToAndroidBitmap(this Texture2D tex2D, bool isPng = true)
		{
			var encoded = isPng ? tex2D.EncodeToPNG() : tex2D.EncodeToJPG();
			return AndroidGraphicsBitmapFactory.AJCCallStaticOnce<AndroidJavaObject>("decodeByteArray", encoded, 0, encoded.Length);
		}

		public static Texture2D Texture2DFromBitmap(AndroidJavaObject bitmapAjo)
		{
			var compressFormatPng = new AndroidJavaClass("android.graphics.Bitmap$CompressFormat").GetStatic<AndroidJavaObject>("PNG");
			var outputStream = new AndroidJavaObject("java.io.ByteArrayOutputStream");
			bitmapAjo.CallBool("compress", compressFormatPng, 100, outputStream);
			var buffer = outputStream.Call<byte[]>("toByteArray");

			var tex = new Texture2D(2, 2);
			tex.LoadImage(buffer);
			return tex;
		}
	}
}