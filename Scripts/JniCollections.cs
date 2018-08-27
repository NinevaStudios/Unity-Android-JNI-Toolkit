using JetBrains.Annotations;

namespace NinevaStudios.AwarenessApi
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	[PublicAPI]
	public static class JniCollections
	{
		public static List<T> FromJavaList<T>(this AndroidJavaObject javaList)
		{
			if (javaList.IsJavaNull())
			{
				return new List<T>();
			}

			int size = javaList.CallInt("size");
			var list = new List<T>(size);

			for (int i = 0; i < size; i++)
			{
				var listItem = javaList.Call<T>("get", i);
				list.Add(listItem);
			}

			javaList.Dispose();
			return list;
		}
		
		public static List<T> FromJavaIterable<T>(this AndroidJavaObject javaIterable)
		{
			if (javaIterable.IsJavaNull())
			{
				return new List<T>();
			}

			var size = javaIterable.CallInt("size");
			var iterator = javaIterable.CallAJO("iterator");
			var list = new List<T>(size);

			while (iterator.CallBool("hasNext"))
			{
				list.Add(iterator.Call<T>("next"));
			}

			javaIterable.Dispose();
			return list;
		}

		public static List<T> FromJavaList<T>(this AndroidJavaObject javaList, Func<AndroidJavaObject, T> converter)
		{
			if (javaList.IsJavaNull())
			{
				return new List<T>();
			}

			int size = javaList.CallInt("size");
			var list = new List<T>(size);

			for (int i = 0; i < size; i++)
			{
				var listItem = converter(javaList.CallAJO("get", i));
				list.Add(listItem);
			}

			javaList.Dispose();
			return list;
		}

		public static AndroidJavaObject ToJavaList<T, TJAVA>(this List<T> items, Func<T, TJAVA> converter)
		{
			var list = new AndroidJavaObject("java.util.ArrayList");

			if (items == null || items.Count == 0)
			{
				return list;
			}

			foreach (var item in items)
			{
				list.Call<bool>("add", converter(item));
			}

			return list;
		}

		public static Dictionary<string, object> FromJavaMap(this AndroidJavaObject javaMap)
		{
			if (javaMap.IsJavaNull())
			{
				return new Dictionary<string, object>();
			}

			int size = javaMap.CallInt("size");
			var dictionary = new Dictionary<string, object>(size);

			var iterator = javaMap.CallAJO("keySet").CallAJO("iterator");
			while (iterator.CallBool("hasNext"))
			{
				var key = iterator.CallStr("next");
				var value = javaMap.CallAJO("get", key);
				dictionary.Add(key, ParseJavaBoxedValue(value));
			}

			javaMap.Dispose();
			return dictionary;
		}

		public static object ParseJavaBoxedValue(AndroidJavaObject boxedValueAjo)
		{
			if (boxedValueAjo.IsJavaNull())
			{
				return null;
			}

			var className = boxedValueAjo.GetClassSimpleName();
			switch (className)
			{
				case "Boolean":
					return boxedValueAjo.CallBool("booleanValue");
				case "Float":
					return boxedValueAjo.CallFloat("floatValue");
				case "Integer":
					return boxedValueAjo.CallInt("intValue");
				case "Long":
					return boxedValueAjo.CallLong("longValue");
				case "String":
					return boxedValueAjo.CallStr("toString");
			}

			return boxedValueAjo;
		}
	}
}