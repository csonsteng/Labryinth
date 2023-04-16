using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Singleton<T>: MonoBehaviour where T : MonoBehaviour
{
	private static T _instance;

	/// <summary>
	/// Returns cached instance.
	/// If no instance is cached, will try to find one in scene.
	/// If no instance is found, will create an instance.
	/// </summary>
	public static T Instance
	{
		get
		{
			if(_instance != null) {
				return _instance;
			}


			_instance = FindObjectOfType<T>(includeInactive: true);	
			if(_instance == null)
			{
				var newInstance = new GameObject(typeof(T).ToString());
				newInstance.SetActive(true);
				_instance = newInstance.AddComponent<T>();
			}
			return _instance;
		}
	}

	/// <summary>
	/// Unlike Instance, will not create a new instance.
	/// Used in placed like scene change cleanup so new gameobjects are not inadvertently created
	/// </summary>
	public static T NullableInstance
	{
		get
		{
			if (_instance != null)
			{
				return _instance;
			}


			_instance = FindObjectOfType<T>(includeInactive: true);
			return _instance;
		}
	}

}
