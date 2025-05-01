using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private EventRelay[] _relays;
    [System.Serializable]
    public class EventRelay
    {
        public string EventName;
        public UnityEvent EventAction;
    }

    private readonly Dictionary<string, UnityEvent> _cachedRelays = new();

	private void Awake()
	{
		foreach (var relay in _relays)
        {
            _cachedRelays.Add(relay.EventName, relay.EventAction);
        }
	}

	public void FireEvent(string eventName)
    {
        if (_cachedRelays.TryGetValue(eventName, out var eventAction))
        {
            eventAction.Invoke();
        }
    }
}
