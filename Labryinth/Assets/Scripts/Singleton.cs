using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Singleton<T>: MonoBehaviour where T : MonoBehaviour
{
	private static T _instance;
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
}
