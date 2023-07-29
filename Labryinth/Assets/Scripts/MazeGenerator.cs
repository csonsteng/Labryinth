using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class MazeGenerator : Singleton<MazeGenerator>
{

	[SerializeField] private int _size = 4;
	[SerializeField] private float _scale = 10f;

	//[SerializeField] private GameObject _nodeObjectTemplate;
	[SerializeField] private GameObject _pathObjectTemplate;

	//[SerializeField] private Material _startMaterial;
	//[SerializeField] private Material _endMaterial;


	public int Size => _size;
	public float Scale => _scale;

	private Dictionary<NodeAddress, Node> NodeMap => Maze.NodeMap;
	private Dictionary<PathID, Path> Paths => Maze.Paths;

	[ContextMenu("Redraw")]
	public void Redraw()
	{
		Maze.Instance.Scale = _scale;
		Clear();
		Generate();
		Maze.Instance.CreatePathfinder();
	}

	private void Clear()
	{
		foreach (var spawnedObject in NodeMap.Values)
		{
			Destroy(spawnedObject.GameObject);
		}

		NodeMap.Clear();
		foreach (var spawnedPath in Paths.Values)
		{
			Destroy(spawnedPath.GameObject);
		}
		Paths.Clear();
	}


	private void FillNodeMap(int size)
	{
		for (var r = 1; r <= size; r++)
		{
			var layerDivisions = Mathf.Pow(2, (2 + Mathf.FloorToInt(r / 2f)));
			var step = 360f / layerDivisions;
			for (var theta = 0f; theta < 360f; theta += step)
			{
				var node = new Node(r, theta);
				NodeMap.Add(node.Address, node);


				node.SetWorldPosition(_scale);

				if (NodeMap.TryGetValue(new NodeAddress(r - 1, theta), out var neighbor))
				{
					node.AllNeighbors.Add(neighbor.Address);
					neighbor.AllNeighbors.Add(node.Address);
				}

				var lastNodeTheta = theta - step;
				if (lastNodeTheta < 0f)
				{
					lastNodeTheta += 360f;
				}
				node.AllNeighbors.Add(new NodeAddress(r, lastNodeTheta));

				var nextNodeTheta = theta + step;
				if (nextNodeTheta >= 360f)
				{
					nextNodeTheta -= 360f;
				}
				node.AllNeighbors.Add(new NodeAddress(r, nextNodeTheta));
			}
		}

	}

	private bool TryAddPath(NodeAddress address1, NodeAddress address2)
	{
		var pathID = new PathID(address1, address2);
		if (Paths.ContainsKey(pathID))
		{
			return false;
		}
		var path = new Path(pathID);
		CreatePathObject(path);

		NodeMap[address1].AccessibleNeighbors.Add(address2);
		NodeMap[address2].AccessibleNeighbors.Add(address1);

		Paths.Add(pathID, path);

		return true;
	}

	/* Test Only
	private void GenerateAngledConnection()
	{
		FillNodeMap(1);
		Maze.StartNode = new NodeAddress(1, 0);
		var branchAddress = new NodeAddress(1, 90);
		Maze.EndNode = new NodeAddress(1, 180);

		TryAddPath(Maze.StartNode, branchAddress);
		TryAddPath(branchAddress, Maze.EndNode);
	}

	private void GenerateTriConnection()
	{
		FillNodeMap(2);
		Maze.StartNode = new NodeAddress(2, 45);
		var branchAddress = new NodeAddress(1, 90);
		var branchAddress3 = new NodeAddress(1, 0);
		var branchAddress4 = new NodeAddress(1, 180);
		var branchAddress2 = new NodeAddress(2, 90);
		Maze.EndNode = new NodeAddress(2, 135);

		TryAddPath(Maze.StartNode, branchAddress2);
		TryAddPath(branchAddress, branchAddress2);
		//TryAddPath(branchAddress, branchAddress3);
		//TryAddPath(branchAddress, branchAddress4);
		TryAddPath(branchAddress2, Maze.EndNode);
	}
	*/

	private void Generate()
	{
		FillNodeMap(_size);

		Maze.StartNodeAddress = Maze.RandomNodeAddress();
		Maze.EndNodeAddress = Maze.RandomNodeAddress();
		while (Maze.EndNodeAddress.Equals(Maze.StartNodeAddress))
		{
			Maze.EndNodeAddress = Maze.RandomNodeAddress();
		}

		var visitedList = new List<NodeAddress>()
		{
			Maze.StartNodeAddress
		};

		var currentStep = NodeMap[Maze.StartNodeAddress];
		var attempts = 0;
		var maxAttempts = 1000;



		// connect the start to the end
		while (!currentStep.Address.Equals(Maze.EndNodeAddress) && attempts < maxAttempts)
		{
			attempts++;
			if (!TryGetRandomNeighbor(currentStep, out var randomNeighbor, visitedList))
			{
				var randomVisitedNode = Random.Range(0, visitedList.Count);
				currentStep = NodeMap[visitedList[randomVisitedNode]];
				Logger.Log($"Attempt {attempts} -- no remaining neighbors on {currentStep}");
			}
			if (!NodeMap.TryGetValue(randomNeighbor, out var randomNode))
			{
				Logger.Log($"Attempt {attempts} -- {randomNeighbor} does not exist");
				continue;
			}
			if (visitedList.Contains(randomNeighbor))
			{

				Logger.Log($"Attempt {attempts} -- {randomNeighbor} has been visited");
				continue;
			}


			TryAddPath(currentStep.Address, randomNeighbor);

			currentStep = randomNode;
			visitedList.Add(currentStep.Address);
		}

		if (attempts + 1 >= maxAttempts)
		{
			Redraw();
		}
		// ensure neither the start or end nodes are dead ends
		foreach(var neighbor in Maze.StartNode.AllNeighbors)
		{
			if(!Paths.ContainsKey(new PathID(Maze.StartNodeAddress, neighbor)))
			{
				TryAddPath(Maze.StartNodeAddress, neighbor);
				visitedList.Add(neighbor);
				break;
			}
		}
		foreach (var neighbor in Maze.EndNode.AllNeighbors)
		{
			if (!Paths.ContainsKey(new PathID(Maze.EndNodeAddress, neighbor)))
			{
				TryAddPath(Maze.EndNodeAddress, neighbor);
				visitedList.Add(neighbor);
				break;
			}
		}



		// add some other random unconnected nodes to ensure a decent size
		while (visitedList.Count < 3 * NodeMap.Keys.Count / 4)
		{
			var randomNodeAddress = Random.Range(0, visitedList.Count);
			var randomNode = NodeMap[visitedList[randomNodeAddress]];
			if (TryGetRandomNeighbor(randomNode, out var node, visitedList))
			{
				TryAddPath(randomNode.Address, node);
				visitedList.Add(node);
			}
		}

		// add some random additional paths whether to existing nodes or not
		var randomAdditionalConnections = 0.1f * NodeMap.Keys.Count;

		for (var i = 0; i < randomAdditionalConnections; i++)
		{
			var randomNodeAddress = Random.Range(0, visitedList.Count);
			var randomNode = NodeMap[visitedList[randomNodeAddress]];
			foreach (var neighbor in randomNode.AllNeighbors)
			{
				var pathID = new PathID(randomNode.Address, neighbor);
				if (!TryAddPath(randomNode.Address, neighbor))
				{
					continue;
				}

				visitedList.Add(neighbor);
			}
		}

		Logger.Log($"Start: {Maze.StartNodeAddress}, End: {Maze.EndNodeAddress}");
	}

	public bool TryGetRandomNeighbor(Node node, out NodeAddress address, List<NodeAddress> exclusionList = null)
	{
		var neighbors = new List<NodeAddress>();
		foreach (var neighbor in node.AllNeighbors)
		{
			if (exclusionList == null || !exclusionList.Contains(neighbor))
			{
				neighbors.Add(neighbor);
			}
		}
		if (neighbors.Count == 0)
		{
			address = new NodeAddress(0, 0f);
			return false;
		}
		var index = Random.Range(0, neighbors.Count);
		address = neighbors[index];
		return true;
	}

	/*
	private void CreateNodeMarkers()
	{
		_nodeObjectTemplate.SetActive(false);
		_pathObjectTemplate.SetActive(false);

		foreach(var node in NodeMap.Values)
		{
			if(node.AccessibleNeighbors.Count > 0) 
			{ 
				MakeNodeGameObject(node);
			}
		}
		
		SetObjectMaterial(MakeNodeGameObject(NodeMap[Maze.StartNodeAddress]), _startMaterial);
		SetObjectMaterial(MakeNodeGameObject(NodeMap[Maze.EndNodeAddress]), _endMaterial);

	}

	private GameObject MakeNodeGameObject(Node node)
	{
		var nodeObject = Instantiate(_nodeObjectTemplate, _nodeObjectTemplate.transform.parent);
		nodeObject.name = node.Address.ToString();
		node.GameObject = nodeObject;
		nodeObject.transform.position = node.Position;
		nodeObject.transform.localScale = PathRenderer.Instance.WicketWidth * Vector3.one;
		nodeObject.SetActive(true);
		return nodeObject;
	}*/

	private void CreatePathObject(Path path)
	{
		var pathObject = Instantiate(_pathObjectTemplate, _pathObjectTemplate.transform.parent);
		pathObject.transform.localPosition = Vector3.zero;
		var lineRenderer = pathObject.GetComponent<LineRenderer>();
		lineRenderer.SetPosition(0, NodeMap[path.PathID.Address1].Position);
		lineRenderer.SetPosition(1, NodeMap[path.PathID.Address2].Position);

		pathObject.SetActive(true);
		path.GameObject = pathObject;
	}

	private void SetObjectMaterial(GameObject gameObject, Material material)
	{
		var meshRenderer = gameObject.GetComponent<MeshRenderer>();
		if(meshRenderer == null)
		{
			return;
		}
		meshRenderer.material = material;
	}


}
