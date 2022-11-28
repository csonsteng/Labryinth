using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logger
{
	private static bool Enabled = false;

	public static void Enable() => Enabled = true;
	public static void Disable() => Enabled = false;
	public static void Log(string logMessage)
	{
		if (!Enabled)
		{
			return;
		}
		Debug.Log(logMessage);
	}
	public static void Warning(string logMessage)
	{
		if (!Enabled)
		{
			return;
		}
		Debug.LogWarning(logMessage);
	}
	public static void Error(string logMessage)
	{
		if (!Enabled)
		{
			return;
		}
		Debug.LogError(logMessage);
	}
}
