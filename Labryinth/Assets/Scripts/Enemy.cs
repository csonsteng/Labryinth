using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Android;
using UnityEngine.Experimental.AI;
using UnityEngine.UIElements;

public class Enemy : Singleton<Enemy>
{
	[SerializeField] private float _sightRange = 20f;
	[SerializeField] private float _smellRange = 50f;
	[SerializeField] private float _wanderSpeed = 10f;
	[SerializeField] private float _huntSpeed = 15f;
	[SerializeField] private float _chaseSpeed = 25f;

	public TextMeshProUGUI DebugText;
	/*
		Has a radius of sense (scent/smell)
		Stops at each node to try to sense
		If no sense chooses a random path
		If makes line of sight - speeds up.
		Will follow the player and continues down a pathway if loses sight.
		Once speed is up, does not slow down until outside of the lesser scent.
	*/

	// todo:
	// 
	// when chasing, do a cross product between the direction to player and direction to nearest node.
	// move a vector that is partially towards the player and partially towards the node based on the cross product.
	// (cross product is assessing how colinear the lines are. colinear = 0)
	// this should hopefully ensure we maintain adequate distance from walls.
	// if we lose sight we should follow the last known player location.
	// once reaching that point, we should continue down the same pathway until we reach an intersection
	// only then should we hunt

	// also can probably find where we are on the map by checking our radius and angle.
	// this way we don't have to rely on a collider in each node that may or may not be bypassed

	// potential: pre-bake all paths, so we can look up paths rather than calc every time. only n*n potential paths

	// also need smarter wandering, so we don't get stuck in areas of the cave. 


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

	private List<NodeAddress> _huntPath = new();
	private int _layerMask;

	public void Spawn()
	{
		_layerMask = LayerMask.GetMask(new string[] { "Characters" });
		_currentNode = Maze.EndNode;
		_lastNode = Maze.EndNode;
		_targetNode = Maze.EndNode;
		transform.position = _currentNode.Position + Vector3.up;
		FindNewTarget();
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
		// we should switch to hunting if we ever smell the player. Right now they wait till intersections to be able to smell
		// maybe scent range increases when hunting at an intersection? They need some passive scent though

		if (_state == State.Frustrated)	// frustration delay
		{
			WaitInFrustration();
			return;
		}

		if(DistanceToTarget <=2.5f)
		{
			OnTargetReached();
		}
		Move();
	}

	private void Move()
	{
		var speed = _wanderSpeed;
		if (_state == State.Hunting)
		{
			speed = _huntSpeed;
		}else if (_state == State.Chasing)
		{
			speed = _chaseSpeed;
		}

		// todo: since node addresses are in radial coordinates, we should be able to move along the curvature of the maze to make movement look more natural
		// good idea, buuuuuut our maze isn't actually made as a circle. Enemy ends up pathing outside the walls. Straight lines better (and easier)
		transform.position += speed * Time.deltaTime * VectorToTarget().normalized;
	}


	private bool CanSeePlayer()
	{
		if (Physics.Raycast(transform.position, (Player.Position - transform.position), out var hitInfo, _sightRange, _layerMask))
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


		foreach (var kvp in _visitedNodes)
		{
			switch (kvp.Value)
			{
				case 0:
					continue;
				case 1:
					Gizmos.color = Color.green;
					break;
				case 2:
					Gizmos.color = Color.yellow;
					break;
				case 3:
					Gizmos.color = new Color(1f, 0.65f, 0f); // orange
					break;
				case 4:
					Gizmos.color = Color.red;
					break;
				default:
					Gizmos.color = Color.black;
					break;
			}
			Gizmos.DrawSphere(Maze.NodeMap[kvp.Key].Position, 10f);
		}
	}

	private float DistanceToPlayer => (Player.Position - transform.position).magnitude;
	private Vector3 VectorToTarget()
	{
		return new(_cachedTarget.x - transform.position.x, 0f, _cachedTarget.z - transform.position.z);
	}
	private float DistanceToTarget => VectorToTarget().magnitude;

	private void FindNewTarget()
	{
		UpdateCurrentNode();
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
		_visitedNodes.Clear();
	}

	private void Hunt()
	{
		// todo: animation delay while tries to find correct direction.
		_state = State.Hunting;
		_visitedNodes.Clear();

		if (DistanceToPlayer > _smellRange)	// if we can smell the player, we want to update our path. otherwise keep the sme hunt
		{
			TargetNextHuntPathPoint();
			return;
		}

		if(!Maze.Instance.TryGetNearestNode(Player.Position, out var closestPlayerNode))
		{
			throw new System.Exception($"Enemy has no fucking clue where the player node is {closestPlayerNode.Address}");
		}

		if(!Maze.Instance.TryFindPath(_currentNode.Address, closestPlayerNode.Address, out _huntPath))
		{
			throw new System.Exception($"Cannot find path from {_currentNode.Address} to {closestPlayerNode.Address}");
		}

		TargetNextHuntPathPoint();
	}

	private void TargetNextHuntPathPoint()
	{
		_lastNode = _currentNode;
		_cachedTarget = Maze.NodeMap[_huntPath[0]].Position;
		_huntPath.RemoveAt(0);
	}

	private Dictionary<NodeAddress, int> _visitedNodes = new();

	private void Wander()
	{
		if (!_visitedNodes.TryGetValue(_currentNode.Address, out var visitCount))
		{
			visitCount = 0;
		}

		_visitedNodes[_currentNode.Address] = visitCount+1;

		NodeAddress targetNodeAddress = _currentNode.Address;
		var visits = new Dictionary<int, List<NodeAddress>>();
		foreach(var neighbor in _currentNode.AccessibleNeighbors)
		{
			if(neighbor.Equals(_lastNode.Address))
			{
				if (_currentNode.AccessibleNeighbors.Count > 1)
				{
					continue;
				}
				targetNodeAddress = neighbor;
				_visitedNodes[targetNodeAddress]--; // if we're forced to turn around cause its our only option, don't penalize that node
			}
			if(!_visitedNodes.TryGetValue(neighbor, out visitCount))
			{
				visitCount = 0;
			}
			if(!visits.TryGetValue(visitCount, out var visitors))
			{
				visitors = new List<NodeAddress>();
				visits[visitCount] = visitors;
			}
			visitors.Add(neighbor);
		}
		for(var i = 0; i < int.MaxValue; i++)
		{
			if (!visits.TryGetValue(i, out var visitors))
			{
				continue;
			}
				
			var randomIndex = Random.Range(0, visitors.Count);
			targetNodeAddress = visitors[randomIndex];
			break;
		}

		

		_lastNode = _currentNode;
		_state = State.Wandering;
		_targetNode = Maze.NodeMap[targetNodeAddress];
		_cachedTarget = _targetNode.Position;
	}

	private void UpdateCurrentNode()
	{
		if(!Maze.Instance.TryGetNearestNode(transform.position, out var nearestNode))
		{
			throw new System.Exception($"Enemy has no fucking clue where they are {nearestNode.Address}");
		}
		_currentNode = nearestNode;
	}
}
