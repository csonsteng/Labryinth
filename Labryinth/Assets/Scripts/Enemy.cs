using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Android;

public class Enemy : Singleton<Enemy>
{
	[SerializeField] private float _runSpeed = 8f;

	[SerializeField] private float _sightRange = 20f;
	[SerializeField] private float _smellRange = 50f;

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

	private NavMeshAgent _agent;

	public void Spawn()
	{
		_agent = GetComponent<NavMeshAgent>();
		_currentNode = Maze.EndNode;
		_lastNode = Maze.EndNode;
		_targetNode = Maze.EndNode;
		transform.position = _currentNode.Position + Vector3.up;
		FindNewTarget();
	}

	public void InformOfPosition(NodeAddress newAddress)
	{
		_currentNode = Maze.NodeMap[newAddress];
		Debug.Log($"at {_currentNode.Address}");
	}

	private void Update()
	{

		if(_state == State.Uninitialized)
		{
			return;
		}

		_playerInVision = false;
		//_agent.autoBraking = true;
		if (CanSeePlayer())
		{
			_state = State.Chasing;
			_lastSensedPlayerPosition = Player.Position;
			_cachedTarget = Player.Position;
			_playerInVision = true;
			//_agent.autoBraking = false;
			_agent.SetDestination(_cachedTarget);
		}

		if (_state == State.Frustrated)
		{
			WaitInFrustration();
			return;
		}

		if(DistanceToTarget <= 0.1f)
		{
			OnTargetReached();
		}

	}

	private bool CanSeePlayer()
	{
		if(Physics.Raycast(transform.position, (Player.Position - transform.position), out var hitInfo, _sightRange, LayerMask.NameToLayer("Interactables")))
		{
			return hitInfo.collider.gameObject.CompareTag("Player");
		}

		return false;
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
		_agent.SetDestination(_cachedTarget);
		//transform.position += speed * Time.deltaTime * DirectionToTarget;
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
				Frustrate();	// we lost the player
				break;
			case State.Hunting:
			case State.Wandering:
				FindNewTarget();
				break;
			default:
				throw new System.ArgumentException($"Invalid Enemy State {_state}");
		}
		
	}

	private void OnDrawGizmos()
	{
		if(_state == State.Uninitialized) { return; }
		
		Debug.DrawLine(transform.position, transform.position + _sightRange * (Player.Position - transform.position).normalized, Color.red);
		Debug.DrawLine(transform.position, _cachedTarget, Color.cyan);
		DebugText.text = $"{_state}\ncanSee: {_playerInVision}\n{_agent.destination}";
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

	private void FindNewTarget()
	{
		if (WithinHuntRange)
		{
			Debug.Log("enemy is HUNTING");
			Hunt();
			return;
		}
		if (_state == State.Frustrated)
		{
			return;
		}
		if (_state == State.Hunting)
		{
			Frustrate();
			return;
		}
		Wander();
	}

	private bool WithinHuntRange => DistanceToPlayer <= _smellRange;
	private void Frustrate()
	{
		_state = State.Frustrated;
		_frustrationTime = 5f; // todo: make this scale based on how long the chase was
	}

	private void Hunt()
	{
		// todo: animation delay while tries to find correct direction.
		// todo: figure out which node is closer to the player
		_state = State.Hunting;
		_lastSensedPlayerPosition = Player.Position;
		_cachedTarget = _lastSensedPlayerPosition;
		_agent.SetDestination(_lastSensedPlayerPosition);
	}

	private void Wander()
	{
		if(!_currentNode.TryGetRandomTraversableNeighbor(out var targetNodeAddress, new List<NodeAddress> { _lastNode.Address }))
		{
			if(!_currentNode.TryGetRandomTraversableNeighbor(out targetNodeAddress)){
				throw new System.Exception("$Enemy's current node has no traversable neighbors {_currentNode}");
			}
		}

		_state = State.Wandering;
		Debug.Log($"target node {targetNodeAddress}");
		_targetNode = Maze.NodeMap[targetNodeAddress];
		_cachedTarget = _targetNode.Position;
		_agent.SetDestination(_cachedTarget);
	}
}
