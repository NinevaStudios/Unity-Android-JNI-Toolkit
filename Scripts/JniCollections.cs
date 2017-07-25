﻿namespace DeadMosquito.JniToolkit
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	public static class JniCollections
	{
		public static List<T> FromJavaList<T>(this AndroidJavaObject javaList)
		{
			if (javaList == null || javaList.IsJavaNull())
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

		public static List<T> FromJavaList<T>(this AndroidJavaObject javaList, Func<AndroidJavaObject, T> converter)
		{
			if (javaList == null || javaList.IsJavaNull())
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

		public static AndroidJavaObject ToJavaList<T>(this List<T> items, Func<T, AndroidJavaObject> converter)
		{
			var list = new AndroidJavaObject("java.util.ArrayList");
			foreach (var item in items)
			{
				list.Call<bool>("add", converter(item));
			}

			return list;
		}

		public static Dictionary<string, object> FromJavaMap(this AndroidJavaObject javaMap)
		{
			if (javaMap == null || javaMap.IsJavaNull())
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
			if (boxedValueAjo == null || boxedValueAjo.IsJavaNull())
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