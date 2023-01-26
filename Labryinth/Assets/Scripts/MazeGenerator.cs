using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
	public PathRenderer PathRenderer;

	public int Size = 4;
	public float Scale = 10f;

	public GameObject NodeObjectTemplate;
	public GameObject PathObjectTemplate;

	public Material StartMaterial;
	public Material EndMaterial;

	

	public readonly Dictionary<NodeAddress, Node> NodeMap = new();
	public readonly Dictionary<PathID, Path> Paths = new();

	public NodeAddress StartNode => _startNodeAddress;
	public NodeAddress EndNode => _endNodeAddress;

	private NodeAddress _startNodeAddress;
	private NodeAddress _endNodeAddress;

	private void Start()
	{
		Logger.Disable();
		Redraw();
	}

	[ContextMenu("Redraw")]
	public void Redraw()
	{
		Clear();
		Generate();
		//GenerateAngledConnection();
		//GenerateTriConnection();
		Draw();
		StartGame();
		PathRenderer.Generate(this);
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

				if (NodeMap.TryGetValue(new NodeAddress(r - 1, theta), out var neighbor))
				{
					node.Neighbors.Add(neighbor.Address);
					neighbor.Neighbors.Add(node.Address);
				}

				var lastNodeTheta = theta - step;
				if (lastNodeTheta < 0f)
				{
					lastNodeTheta += 360f;
				}
				node.Neighbors.Add(new NodeAddress(r, lastNodeTheta));

				var nextNodeTheta = theta + step;
				if (nextNodeTheta >= 360f)
				{
					nextNodeTheta -= 360f;
				}
				node.Neighbors.Add(new NodeAddress(r, nextNodeTheta));
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
		Paths.Add(pathID, path);

		return true;
	}

	private void GenerateAngledConnection()
	{
		FillNodeMap(1);
		_startNodeAddress = new NodeAddress(1, 0);
		var branchAddress = new NodeAddress(1, 90);
		_endNodeAddress = new NodeAddress(1, 180);

		TryAddPath(_startNodeAddress, branchAddress);
		TryAddPath(branchAddress, _endNodeAddress);
	}

	private void GenerateTriConnection()
	{
		FillNodeMap(2);
		_startNodeAddress = new NodeAddress(2, 45);
		var branchAddress = new NodeAddress(1, 90);
		var branchAddress3 = new NodeAddress(1, 0);
		var branchAddress4 = new NodeAddress(1, 180);
		var branchAddress2 = new NodeAddress(2, 90);
		_endNodeAddress = new NodeAddress(2, 135);

		TryAddPath(_startNodeAddress, branchAddress2);
		TryAddPath(branchAddress, branchAddress2);
		//TryAddPath(branchAddress, branchAddress3);
		//TryAddPath(branchAddress, branchAddress4);
		TryAddPath(branchAddress2, _endNodeAddress);
	}

	private void Generate()
	{

		FillNodeMap(Size);

		_startNodeAddress = RandomNodeAddress();
		_endNodeAddress = RandomNodeAddress();
		while (_endNodeAddress.Equals(_startNodeAddress))
		{
			_endNodeAddress = RandomNodeAddress();
		}

		var visitedList = new List<NodeAddress>()
		{
			_startNodeAddress
		};

		var currentStep = NodeMap[_startNodeAddress];
		var attempts = 0;
		var maxAttempts = 1000;

		// connect the start to the end
		while (!currentStep.Address.Equals(_endNodeAddress) && attempts < maxAttempts)
		{
			attempts++;
			if (!currentStep.TryGetRandomNeighbor(visitedList, out var randomNeighbor))
			{
				var randomVisitedNode = Random.Range(0, visitedList.Count);
				currentStep = NodeMap[visitedList[randomVisitedNode]];
				Logger.Log($"Attempt {attempts} -- no remaining neighbors on {currentStep}");
			}
			if(!NodeMap.TryGetValue(randomNeighbor, out var randomNode))
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

		if(attempts + 1 >= maxAttempts)
		{
			Redraw();
		}

		// add some other random unconnected nodes to ensure a decent size
		while (visitedList.Count < 3 * NodeMap.Keys.Count / 4)
		{
			var randomNodeAddress = Random.Range(0, visitedList.Count);
			var randomNode = NodeMap[visitedList[randomNodeAddress]];
			if (randomNode.TryGetRandomNeighbor(visitedList, out var node))
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
			foreach (var neighbor in randomNode.Neighbors)
			{
				var pathID = new PathID(randomNode.Address, neighbor);
				if (!TryAddPath(randomNode.Address, neighbor))
				{
					continue;
				}

				visitedList.Add(neighbor);
			}
		}

		Logger.Log($"Start: {_startNodeAddress}, End: {_endNodeAddress}");
	}

	private void Draw()
	{
		NodeObjectTemplate.SetActive(false);
		PathObjectTemplate.SetActive(false);

		foreach (var node in NodeMap.Values)
		{
			var nodeObject = Instantiate(NodeObjectTemplate, NodeObjectTemplate.transform.parent);
			nodeObject.name = node.Address.ToString();
			nodeObject.transform.localPosition = node.Position(Scale);
			nodeObject.SetActive(true);
			node.GameObject = nodeObject;

			if(node.Address.Equals(_startNodeAddress))
			{
				SetObjectMaterial(nodeObject, StartMaterial);
				nodeObject.transform.localScale = 5f * Vector3.one;
			}
			if (node.Address.Equals(_endNodeAddress))
			{
				SetObjectMaterial(nodeObject, EndMaterial);
				nodeObject.transform.localScale = 5f * Vector3.one;
			}
		}
	}

	private void StartGame()
	{
		FirstPersonController.Instance.Initialize(NodeMap[_startNodeAddress].GameObject.transform.position);
	}
	private void CreatePathObject(Path path)
	{
		var pathObject = Instantiate(PathObjectTemplate, PathObjectTemplate.transform.parent);
		pathObject.transform.localPosition = Vector3.zero;
		var lineRenderer = pathObject.GetComponent<LineRenderer>();
		lineRenderer.SetPosition(0, NodeMap[path.PathID.Address1].Position(Scale));
		lineRenderer.SetPosition(1, NodeMap[path.PathID.Address2].Position(Scale));

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

	public NodeAddress RandomNodeAddress()
	{
		var allNodes = NodeMap.Keys.ToArray();
		var index = Random.Range(0, allNodes.Length);
		return allNodes[index];
	}
}
