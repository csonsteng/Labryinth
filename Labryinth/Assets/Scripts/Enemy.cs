using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class Enemy : MonoBehaviour
{
	[SerializeField] private float _walkSpeed = 5f;
	[SerializeField] private float _runSpeed = 8f;

	[SerializeField] private float _senseRadius = 20f;
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

	public void Spawn()
	{
		_currentNode = Maze.EndNode;
		_lastNode = Maze.EndNode;
		_targetNode = Maze.EndNode;
		transform.position = _currentNode.Position;
		SearchForNextIntersection();
		_state = State.Wandering;
	}

	private void Update()
	{

		if(_state == State.Uninitialized)
		{
			return;
		}

		// todo: check for player in sight

		transform.position += DirectionToTarget * _runSpeed * Time.deltaTime;

		if(DistanceToTarget < 1)
		{
			SearchForNextIntersection();
		}

	}

	private float DistanceToPlayer => (GameManager.Instance.PlayerPosition - transform.position).magnitude;
	private Vector3 VectorToTarget => _targetNode.Position - transform.position;
	private float DistanceToTarget => VectorToTarget.magnitude;
	private Vector3 DirectionToTarget => VectorToTarget.normalized;

	private void SearchForNextIntersection()
	{
		if (DistanceToPlayer < _senseRadius)
		{
			Debug.Log("We are close to the player. Find out which direction");
		}
		_lastNode = _currentNode;
		_currentNode = _targetNode;

		var validOptions = new List<NodeAddress>();
		foreach(var neighbor in _currentNode.Neighbors)
		{
			if(neighbor.Equals( _lastNode.Address))
			{
				continue;
			}
			if(Maze.Paths.ContainsKey(new PathID(_currentNode.Address, neighbor)))
			{
				validOptions.Add(neighbor);
			}
		}

		if(validOptions.Count == 0)
		{
			validOptions.Add(_lastNode.Address);
		}

		var randomSelection = Random.Range(0, validOptions.Count);
		_targetNode = Maze.NodeMap[validOptions[randomSelection]];

	}

	private void ProcessState()
	{
		switch (_state)
		{
			case State.Uninitialized:
				throw new System.Exception("enemy not initialized");
			case State.Wandering:
				
				break;
			case State.Hunting:

				break;
			case State.Chasing:

				break;
		}
	}
}
