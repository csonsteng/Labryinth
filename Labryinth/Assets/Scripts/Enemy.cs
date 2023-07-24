using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Android;
using UnityEngine.UIElements;

public class Enemy : Singleton<Enemy>
{
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

	private float _frustrationTime = 5f;

	private bool _playerInVision = false;
	private Vector3 _cachedTarget;

	private NavMeshAgent _agent;

	private List<NodeAddress> _huntPath = new();

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
	}

	private void Update()
	{

		if(_state == State.Uninitialized)
		{
			return;
		}

		_playerInVision = false;

		if (CanSeePlayer())	// if we can see the player. chase regardless what else is happening
		{
			Chase();
		}

		if (_state == State.Frustrated)	// frustration delay
		{
			WaitInFrustration();
			return;
		}

		if(DistanceToTarget <=2.5f)
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

	private void Chase()
	{
		_state = State.Chasing;
		_cachedTarget = Player.Position;
		_playerInVision = true;
		_agent.SetDestination(_cachedTarget);
	}

	private void WaitInFrustration()
	{
		_frustrationTime -= Time.deltaTime;
		if (_frustrationTime > 0f)
		{
			return;
		};

		_state = State.Wandering;
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
				Frustrate();	// we lost the player (should actually hunt first here)
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
		Gizmos.DrawWireSphere(transform.position, _smellRange);
		DebugText.text = $"{_state}\ncanSee: {_playerInVision}\ndistanceToDestination: {DistanceToTarget.ToString("0.0")}";
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
			Hunt();
			return;
		}
		if (_state == State.Frustrated)
		{
			return;
		}
		if (_state == State.Hunting)
		{
			Frustrate();	// if we finished a hunt without finding the player, we are frustrated.
			return;
		}
		Wander();
	}

	/// <summary>
	/// We can smell the player, or have not yet finished our last hunt
	/// </summary>
	private bool WithinHuntRange => DistanceToPlayer <= _smellRange	|| _huntPath.Count > 0;
	private void Frustrate()
	{
		_state = State.Frustrated;
		_frustrationTime = 5f; // todo: make this scale based on how long the chase was
	}

	private void Hunt()
	{
		// todo: animation delay while tries to find correct direction.
		_state = State.Hunting;

		if(DistanceToPlayer > _smellRange)	// if we can smell the player, we want to update our path. otherwise keep the sme hunt
		{
			TargetNextHuntPathPoint();
			return;
		}

		if(!Maze.Instance.TryFindPath(_currentNode.Address, Player.LastNodeAddress, out _huntPath))
		{
			throw new System.Exception($"Cannot find path from {_currentNode.Address} to {Player.LastNodeAddress}");
		}

		TargetNextHuntPathPoint();
	}

	private void TargetNextHuntPathPoint()
	{
		_lastNode = _currentNode;
		_cachedTarget = Maze.NodeMap[_huntPath[0]].Position;
		_huntPath.RemoveAt(0);
		_agent.SetDestination(_cachedTarget);
	}

	private void Wander()
	{
		if(!_currentNode.TryGetRandomTraversableNeighbor(out var targetNodeAddress, new List<NodeAddress> { _lastNode.Address }))
		{
			if(!_currentNode.TryGetRandomTraversableNeighbor(out targetNodeAddress)){
				throw new System.Exception("$Enemy's current node has no traversable neighbors {_currentNode}");
			}
		}
		_lastNode = _currentNode;
		_state = State.Wandering;
		_targetNode = Maze.NodeMap[targetNodeAddress];
		_cachedTarget = _targetNode.Position;
		_agent.SetDestination(_cachedTarget);
	}
}
