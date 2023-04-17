using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Android;

public class Enemy : MonoBehaviour
{
	[SerializeField] private float _runSpeed = 8f;

	[SerializeField] private float _senseRadius = 20f;

	public TextMeshProUGUI DebugText;
	/*
		Has a radius of sense (scent/smell)
		Stops at each node to try to sense
		If no sense chooses a random path
		If makes line of sight - speeds up.
		Will follow the player and continues down a pathway if loses sight.
		Once speed is up, does not slow down until outside of the lesser scent.
	*/

	/// <summary>
	/// Wandering: No Sense of Player
	/// Hunting: Can Sense Player but has not Seen player
	/// Chasing: Has Seen Player
	/// Frustrated: Has Lost Player -- will return to Wandering
	/// </summary>
	private enum State
	{
		Uninitialized,
		Wandering,
		Hunting,
		Chasing,
		Frustrated,
	}

	private State _state = State.Uninitialized;
	private Node _currentNode;
	private Node _lastNode;
	private Node _targetNode;

	private Vector3 _lastSensedPlayerPosition;
	private float _frustrationTime = 5f;

	private bool _playerInVision = false;
	private Vector3 _cachedTarget;

	public void Spawn()
	{
		_currentNode = Maze.EndNode;
		_lastNode = Maze.EndNode;
		_targetNode = Maze.EndNode;
		transform.position = _currentNode.Position + Vector3.up;
		FindNewTarget();
		_state = State.Wandering;
	}

	private void Update()
	{

		if(_state == State.Uninitialized)
		{
			return;
		}

		if (_state == State.Frustrated)
		{
			WaitInFrustration();
			return;
		}

		MoveTowardsTarget();

		if(DistanceToTarget <= 0.05f)
		{
			OnTargetReached();
		}

	}

	private void WaitInFrustration()
	{
		if (_frustrationTime == 5.0f)
		{
			Debug.Log("Enemy is FRUSTRATED");
		}

		_frustrationTime -= Time.deltaTime;
		if (_frustrationTime > 0f)
		{
			return;
		};

		_state = State.Wandering;
		Debug.Log("Enemy is WANDERING");
		// todo: find current nodes close to last sensed player target position

	}

	private void MoveTowardsTarget()
	{

		_cachedTarget = TargetPosition();
		var speed = _state == State.Chasing ? _runSpeed * 2f : _runSpeed;
		transform.position += speed * Time.deltaTime * DirectionToTarget;
	}

	private void OnTargetReached()
	{
		switch (_state)
		{
			case State.Chasing:
				if (_playerInVision)
				{
					Debug.Log("YOU WERE CAUGHT");
					return;
				}
				PrepareFrustration();
				break;
			case State.Hunting:
			case State.Wandering:
				FindNewTarget();
				break;
			default:
				throw new System.ArgumentException($"Invalid Enemy State {_state}");
		}
		
	}

	private void OnTriggerEnter(Collider collider)
	{
		if(collider.gameObject.CompareTag("Player"))
		{
			_state = State.Chasing;
			Debug.Log("Enemy is CHASING");
			_lastSensedPlayerPosition = Player.Position;
			_playerInVision = true;
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (collider.gameObject.CompareTag("Player"))
		{
			// when switching from chasing to hunting, use wicket base points rather than intersections?
			// really should still be "chasing" as long as player is sensed.

			_playerInVision = false;;
		}
	}

	private void OnDrawGizmos()
	{
		if(_state == State.Uninitialized) { return; }
		Debug.DrawLine(transform.position, _cachedTarget, Color.cyan);
		DebugText.text = $"{_state}\ncanSee: {_playerInVision}\ncanSense: {DistanceToPlayer <= _senseRadius}";
	}


	private Vector3 TargetPosition()
	{
		switch (_state)
		{
			case State.Wandering:
			case State.Frustrated:
			case State.Hunting:
				return _targetNode.Position;
			case State.Chasing:
				if (DistanceToPlayer <= _senseRadius)
				{
					_lastSensedPlayerPosition = Player.Position;
				}
				return _playerInVision ? Player.Position : _lastSensedPlayerPosition;
			default:
				break;
		}
		throw new System.ArgumentException($"Invalid Enemy State {_state}");
	}
	private float DistanceToPlayer => (Player.Position - transform.position).magnitude;
	private Vector3 VectorToTarget()
	{
		return new(_cachedTarget.x - transform.position.x, 0f, _cachedTarget.z - transform.position.z);
	}
	private float DistanceToTarget => VectorToTarget().magnitude;
	private Vector3 DirectionToTarget => VectorToTarget().normalized;

	private void FindNewTarget()
	{
		if (WithinHuntRange())
		{
			Debug.Log("enemy is HUNTING");
			Hunt();
			return;
		}
		if (_state == State.Frustrated)
		{
			return;
		}
		Wander();
	}

	private bool WithinHuntRange()
	{
		if (_state == State.Hunting)
		{
			PrepareFrustration();
		}
		if(DistanceToPlayer > _senseRadius)
		{
			return false;
		}
		_state = State.Hunting;
		_lastSensedPlayerPosition = Player.Position;
		return true;
	}

	private void PrepareFrustration()
	{
		_state = State.Frustrated;
		_frustrationTime = 5f;
	}

	private void Hunt()
	{
		var allNodesInArea = new List<NodeAddress>();

		// one of these is likely unnessecary. figure out which
		// hmmm if we were chasing for a while, maybe we don't have the best option?
		// need a GetNearestNodeToPosition method
		allNodesInArea.AddRange(GetNodeNeighbors(_currentNode));
		allNodesInArea.AddRange(GetNodeNeighbors(_lastNode));
		allNodesInArea.AddRange(GetNodeNeighbors(_targetNode));

		var nearestToPlayer = (_lastSensedPlayerPosition - transform.position).magnitude;
		Node nearestOption = null;
		foreach (var option in allNodesInArea)
		{
			var node = Maze.NodeMap[option];
			var distanceToPlayer = (_lastSensedPlayerPosition - node.Position).magnitude;
			if (distanceToPlayer < nearestToPlayer)
			{
				nearestToPlayer = distanceToPlayer;
				nearestOption = node;
			}
		}
		if (nearestOption != null)
		{
			_targetNode = nearestOption;
			return;
		}
	}

	private void Wander()
	{
		_lastNode = _currentNode;
		_currentNode = _targetNode;


		var validOptions = GetNodeNeighbors(_currentNode).ToList();

		if (validOptions.Count > 1)
		{
			validOptions.Remove(_lastNode.Address);
		}

		var randomSelection = Random.Range(0, validOptions.Count);
		_targetNode = Maze.NodeMap[validOptions[randomSelection]];
	}

	private IEnumerable<NodeAddress> GetNodeNeighbors(Node node)
	{
		foreach (var neighbor in node.Neighbors)
		{
			if (Maze.Paths.ContainsKey(new PathID(node.Address, neighbor)))
			{
				yield return neighbor;
			}
		}
	}

}
