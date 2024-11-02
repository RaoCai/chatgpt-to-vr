using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralModeling
{
    public class ProceduralTree : ProceduralModelingBase
    {
        public TreeData Data { get { return data; } }
        internal List<TreeBranch> branches = new List<TreeBranch>();
        private int branchCount = 0;

        public event Action<TreeBranch> OnBranchCreated;

        [SerializeField] TreeData data;
        [SerializeField, Range(2, 8)] internal int generations = 3;
        [SerializeField, Range(0.5f, 5f)] protected float trunkLength = 1f;
        [SerializeField, Range(0.1f, 2f)] protected float trunkRadius = 0.12f;
        private HashSet<int> deletedBranchIds = new HashSet<int>();

        public Dictionary<int, Vector3> BranchPositions { get; private set; }

        private void Start()
        {
            InitializeTree();
        }

        protected override Mesh Build()
        {
            return CreateMeshFromBranches();
        }

        private void InitializeTree()
        {
            Debug.Log("Initializing tree...");
            data.Setup();
            branches.Clear();
            BranchPositions = new Dictionary<int, Vector3>();
            deletedBranchIds.Clear();

            // Generate trunk
            TreeBranch trunk = GenerateTrunk();
            branches.Add(trunk);

            // Generate branches
            GenerateBranchesRecursive(trunk, generations, 3);

            RegenerateMesh();
        }

        private TreeBranch GenerateTrunk()
        {
            Debug.Log("Generating trunk...");
            Vector3 start = Vector3.zero;
            Vector3 end = Vector3.up * trunkLength * 1.5f;
            float baseRadius = trunkRadius;
            float topRadius = trunkRadius * 0.8f;
            TreeBranch trunk = new TreeBranch(generations, trunkLength * 1.5f, baseRadius, 1);
            trunk.SetStartPoint(start);
            trunk.SetEndPoint(end);
            trunk.SetTopRadius(topRadius);
            BranchPositions[trunk.BranchId] = end;
            return trunk;
        }

        private void SetBranchIndices(List<Vector3> vertices, List<Vector4> customData)
        {
            int verticesPerBranch = 20;
            for (int i = 0; i < vertices.Count; i++)
            {
                int branchIndex = i / verticesPerBranch;
                Vector4 vertexWithBranchIndex = vertices[i];
                vertexWithBranchIndex.w = branchIndex;
                customData.Add(vertexWithBranchIndex);
            }
        }

        private void GenerateBranchesRecursive(TreeBranch parent, int remainingGenerations, int branchCount)
        {
            if (remainingGenerations <= 0) return;

            for (int i = 0; i < branchCount; i++)
            {
                TreeBranch newBranch = CreateBranch(parent);
                if (newBranch != null)
                {
                    GenerateBranchesRecursive(newBranch, remainingGenerations - 2, UnityEngine.Random.Range(2, 4));
                }
            }
        }

        private TreeBranch CreateBranch(TreeBranch parent)
        {
            Vector3 parentDirection = (parent.EndPoint - parent.StartPoint).normalized;
            float angle = UnityEngine.Random.Range(30f, 60f);
            float rotationAngle = UnityEngine.Random.Range(0f, 360f);
            Quaternion rotation = Quaternion.AngleAxis(rotationAngle, parentDirection) * Quaternion.AngleAxis(angle, Vector3.right);
            Vector3 newDirection = rotation * parentDirection;

            float branchLength = parent.Length * UnityEngine.Random.Range(0.6f, 0.8f);
            float branchRadius = parent.Radius * UnityEngine.Random.Range(0.6f, 0.8f);

            Vector3 branchStart = parent.EndPoint;
            Vector3 branchEnd = branchStart + newDirection * branchLength;

            if (BranchPositions.Values.Any(ep => Vector3.Distance(ep, branchEnd) < 0.1f))
            {
                return null;
            }

            int branchId = branches.Count + 1;
            TreeBranch newBranch = new TreeBranch(parent.Generation + 2, branchLength, branchRadius, branchId);
            newBranch.SetStartPoint(branchStart);
            newBranch.SetEndPoint(branchEnd);
            newBranch.SetTopRadius(branchRadius * 0.8f);

            // Add branch only once
            branches.Add(newBranch);
            BranchPositions[newBranch.BranchId] = branchEnd;

            if (parent.BranchId != 1)
            {
                AddColliderToBranch(newBranch);
            }

            // Duplicate lines
            // branches.Add(newBranch);
            // BranchPositions[newBranch.BranchId] = branchEnd;
            
            OnBranchCreated?.Invoke(newBranch);

            return newBranch;
        }

        public int GrowBranchesFromInitial(int numberOfBranches, Dictionary<int, float> branchGrowth)
        {
            Debug.Log($"Growing up to {numberOfBranches} new branches from existing branches.");

            int branchesGrown = 0;
            List<TreeBranch> parentBranches = branches.Where(b => b.Generation > 1).ToList();

            while (branchesGrown < numberOfBranches && parentBranches.Count > 0)
            {
                TreeBranch parentBranch = parentBranches[UnityEngine.Random.Range(0, parentBranches.Count)];
                TreeBranch newBranch = CreateBranch(parentBranch);
                
                if (newBranch != null)
                {
                    branchesGrown++;
                    branchGrowth[newBranch.BranchId] = 0f;
                }

                if (branchesGrown >= numberOfBranches) break;
            }

            RegenerateMesh();
            return branchesGrown;
        }

        private void AddColliderToBranch(TreeBranch branch)
        {
            GameObject branchObject = new GameObject($"Branch_{branch.BranchId}");
            branchObject.transform.position = Vector3.Lerp(branch.StartPoint, branch.EndPoint, 0.5f);

            // Calculate direction and set rotation
            Vector3 direction = (branch.EndPoint - branch.StartPoint).normalized;
            branchObject.transform.rotation = Quaternion.LookRotation(direction);

            // Add a BoxCollider
            BoxCollider collider = branchObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(branch.Radius * 2, branch.Length, branch.Radius * 2);
            collider.center = Vector3.zero;

            // Attach the branch object to the tree
            branchObject.transform.parent = this.transform;
        }

        private Mesh CreateMeshFromBranches()
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector4> customData = new List<Vector4>();

            foreach (TreeBranch branch in branches)
            {
                AddBranchToMesh(branch, vertices, triangles, normals, uvs);
            }

            SetBranchIndices(vertices, customData);

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.SetUVs(3, customData);

            mesh.RecalculateBounds();

            return mesh;
        }

        private void AddBranchToMesh(TreeBranch branch, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs)
        {
            int segments = 8;
            int vertexOffset = vertices.Count;

            Vector3 direction = (branch.EndPoint - branch.StartPoint).normalized;
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right.magnitude < 0.001f)
            {
                right = Vector3.Cross(direction, Vector3.forward).normalized;
            }
            Vector3 forward = Vector3.Cross(right, direction);

            float uvStep = 1.0f / segments;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * (2 * Mathf.PI / segments);

                Vector3 startOffset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * forward) * branch.Radius;
                Vector3 endOffset = startOffset * (branch.TopRadius / branch.Radius);

                vertices.Add(branch.StartPoint + startOffset);
                vertices.Add(branch.EndPoint + endOffset);
                normals.Add(startOffset.normalized);
                normals.Add(endOffset.normalized);

                uvs.Add(new Vector2(i * uvStep, 0));
                uvs.Add(new Vector2(i * uvStep, 1));

                if (i < segments)
                {
                    int baseIndex = vertexOffset + i * 2;
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 3);
                    triangles.Add(baseIndex + 2);
                }
            }
        }

        public void DeleteBranch(int branchId)
        {
            Debug.Log($"ProceduralTree.DeleteBranch called for branch {branchId}");
            
            // Find the branch to delete
            TreeBranch branchToDelete = branches.Find(b => b.BranchId == branchId);
            if (branchToDelete == null)
            {
                Debug.LogError($"Branch {branchId} not found");
                return;
            }

            // Don't delete the trunk (branch 1)
            if (branchId == 1)
            {
                Debug.Log("Cannot delete trunk (branch 1)");
                return;
            }

            // Get all child branches
            var childBranches = branches.Where(b => 
                Vector3.Distance(b.StartPoint, branchToDelete.EndPoint) < 0.1f).ToList();

            Debug.Log($"Found {childBranches.Count} child branches to delete");

            // Remove the branch and its children
            branches.Remove(branchToDelete);
            BranchPositions.Remove(branchId);
            
            // Remove children
            foreach (var child in childBranches)
            {
                DeleteBranch(child.BranchId);
            }

            // Clean up any branch GameObjects
            GameObject branchObject = GameObject.Find($"Branch_{branchId}");
            if (branchObject != null)
            {
                Destroy(branchObject);
            }

            RegenerateMesh();
            Debug.Log($"Branch {branchId} and its children deleted successfully");
        }

        private void AddBranchAndChildrenToDeletedSet(int branchId)
        {
            deletedBranchIds.Add(branchId);
            var childBranches = branches.Where(b => b.BranchId.ToString().StartsWith(branchId.ToString()) && b.BranchId != branchId).ToList();
            foreach (var childBranch in childBranches)
            {
                AddBranchAndChildrenToDeletedSet(childBranch.BranchId);
            }
        }

        internal void RebuildTree()
        {
            Debug.Log("Rebuilding tree...");
            List<TreeBranch> newBranches = new List<TreeBranch>();
            Dictionary<int, Vector3> newBranchPositions = new Dictionary<int, Vector3>();

            foreach (TreeBranch branch in branches)
            {
                if (!deletedBranchIds.Contains(branch.BranchId))
                {
                    newBranches.Add(branch);
                    newBranchPositions[branch.BranchId] = branch.EndPoint;
                }
            }

            branches = newBranches;
            BranchPositions = newBranchPositions;

            RegenerateMesh();
        }

        internal void RegenerateMesh()
        {
            Mesh mesh = CreateMeshFromBranches();
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (meshRenderer.sharedMaterial == null)
            {
                meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            }
        }

        public void TagBranch(int branchId, string tag)
        {
            TreeBranch branch = branches.Find(b => b.BranchId == branchId);
            if (branch != null)
            {
                branch.AddTag(tag);
                RegenerateMesh();
            }
        }

        public void RemoveTagFromBranch(int branchId, string tag)
        {
            TreeBranch branch = branches.Find(b => b.BranchId == branchId);
            if (branch != null)
            {
                branch.RemoveTag(tag);
                RegenerateMesh();
            }
        }

        public void SetBranchCategory(int branchId, string category)
        {
            TreeBranch branch = branches.Find(b => b.BranchId == branchId);
            if (branch != null)
            {
                branch.SetCategory(category);
                RegenerateMesh();
            }
        }

        public List<TreeBranch> GetBranchesByCategory(string category)
        {
            return branches.Where(b => b.Category == category).ToList();
        }

        public List<TreeBranch> GetBranchesByTag(string tag)
        {
            return branches.Where(b => b.Tags.Contains(tag)).ToList();
        }

        public string SerializeTreeData()
        {
            var treeData = new
            {
                Branches = branches.Select(b => new
                {
                    b.BranchId,
                    StartPoint = b.StartPoint.ToString(),
                    EndPoint = b.EndPoint.ToString(),
                    Tags = b.Tags,
                    Category = b.Category
                }).ToList()
            };

            return JsonUtility.ToJson(treeData, true);
        }
    }

    [System.Serializable]
    public class TreeData
    {
        public int randomSeed = 0;
        [Range(0.25f, 0.95f)] public float lengthAttenuation = 0.8f, radiusAttenuation = 0.5f;
        [Range(1, 3)] public int branchesMin = 1, branchesMax = 3;
        [Range(-45f, 0f)] public float growthAngleMin = -15f;
        [Range(0f, 45f)] public float growthAngleMax = 15f;
        [Range(1f, 10f)] public float growthAngleScale = 4f;
        [Range(0f, 45f)] public float branchingAngle = 15f;
        [Range(4, 20)] public int heightSegments = 10, radialSegments = 8;
        [Range(0.0f, 0.35f)] public float bendDegree = 0.1f;

        private System.Random rnd;

        public void Setup()
        {
            rnd = new System.Random(randomSeed);
        }

        public int GetRandomBranches()
        {
            return rnd.Next(branchesMin, branchesMax + 1);
        }

        public float GetRandomGrowthAngle()
        {
            return UnityEngine.Random.Range(growthAngleMin, growthAngleMax);
        }

        public float GetRandomBendDegree()
        {
            return UnityEngine.Random.Range(-bendDegree, bendDegree);
        }
    }

    public class TreeBranch
    {
        public List<string> Tags { get; private set; } = new List<string>();
        public string Category { get; private set; }
        public int Generation { get; private set; }
        public float Length { get; private set; }
        public float Radius { get; private set; }
        public int BranchId { get; private set; }
        public Vector3 StartPoint { get; private set; }
        public Vector3 EndPoint { get; private set; }
        public float TopRadius { get; private set; }

        public TreeBranch(int generation, float length, float radius, int branchId)
        {
            Generation = generation;
            Length = length;
            Radius = radius;
            BranchId = branchId;
        }

        public void SetTopRadius(float topRadius)
        {
            TopRadius = topRadius;
        }

        public void SetStartPoint(Vector3 startPoint)
        {
            StartPoint = startPoint;
        }

        public void SetEndPoint(Vector3 endPoint)
        {
            EndPoint = endPoint;
        }

        public void AddTag(string tag)
        {
            Tags.Add(tag);
        }

        public void RemoveTag(string tag)
        {
            Tags.Remove(tag);
        }

        public void SetCategory(string category)
        {
            Category = category;
        }
    }
}