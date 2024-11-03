using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.Collections;

namespace ProceduralModeling
{
    public class ProceduralTree : ProceduralModelingBase
    {
        public TreeData Data { get { return data; } }
        internal List<TreeBranch> branches = new List<TreeBranch>();
        public Dictionary<int, Vector3> BranchPositions { get; private set; }
        private List<GameObject> leaves = new List<GameObject>();
        
        public event Action<TreeBranch> OnBranchCreated;

        [SerializeField] TreeData data;
        [SerializeField] private GameObject leafPrefab;
        [SerializeField] private float leafSize = 0.2f;
        [SerializeField] private float leafDensity = 1f;
        [SerializeField, Range(2, 8)] internal int generations = 3;
        [SerializeField, Range(0.5f, 5f)] protected float trunkLength = 1f;
        [SerializeField, Range(0.1f, 2f)] protected float trunkRadius = 0.12f;
        [SerializeField] private int fixedSeed = 12345;

        private HashSet<int> branchesWithLeaves = new HashSet<int>();
        private HashSet<int> deletedBranchIds = new HashSet<int>();

        private int branchCount = 0;
        private Coroutine leafGenerationCoroutine;
        private System.Random randomGenerator;

        private void Start()
        {
            SetupRandomGenerator();
            InitializeTree();
        }

        protected override Mesh Build()
        {
            return CreateMeshFromBranches();
        }

        private void InitializeTree()
        {
            Debug.Log("Initializing tree...");
            data.Setup(randomGenerator);
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

        private void SetupRandomGenerator()
        {
            randomGenerator = new System.Random(fixedSeed);
            UnityEngine.Random.InitState(fixedSeed); // Set Unity's random seed too
        }

        private float GetRandomRange(float min, float max)
        {
            float t = (float)(randomGenerator.NextDouble());
            return min + t * (max - min);
        }

        private TreeBranch GenerateTrunk()
        {
            Debug.Log("Generating trunk...");
            // Calculate the start and end points of the trunk
            Vector3 start = Vector3.zero;
            Vector3 end = Vector3.up * trunkLength * 1.5f;
            // Calculate the base and top radii of the trunk
            float baseRadius = trunkRadius;
            float topRadius = trunkRadius * 0.8f;
            // Create a new trunk branch
            TreeBranch trunk = new TreeBranch(generations, trunkLength * 1.5f, baseRadius, 1);
            trunk.SetStartPoint(start);
            trunk.SetEndPoint(end);
            trunk.SetTopRadius(topRadius);
            BranchPositions[trunk.BranchId] = end;
            return trunk;
        }

        private void SetBranchIndices(List<Vector3> vertices, List<Vector4> customData)
        {
            // Calculate the number of vertices per branch
            int verticesPerBranch = 20;
            for (int i = 0; i < vertices.Count; i++)
            {
                // Calculate the index of the branch
                int branchIndex = i / verticesPerBranch;
                // Add the branch index to the custom data
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
                    GenerateBranchesRecursive(newBranch, remainingGenerations - 2, 
                        randomGenerator.Next(2, 4)); 
                }
            }
        }

        private TreeBranch CreateBranch(TreeBranch parent)
        {
            // Calculate the direction of the parent branch
            Vector3 parentDirection = (parent.EndPoint - parent.StartPoint).normalized;
            // Calculate the angle of rotation
            float angle = GetRandomRange(30f, 60f);
            // Calculate the rotation angle
            float rotationAngle = GetRandomRange(0f, 360f);
            // Calculate the rotation quaternion
            Quaternion rotation = Quaternion.AngleAxis(rotationAngle, parentDirection) * Quaternion.AngleAxis(angle, Vector3.right);
            // Calculate the new direction
            Vector3 newDirection = rotation * parentDirection;

            // Calculate the length and radius of the new branch    
            float branchLength = parent.Length * GetRandomRange(0.6f, 0.8f);
            float branchRadius = parent.Radius * GetRandomRange(0.6f, 0.8f);

            // Calculate the start and end points of the new branch
            Vector3 branchStart = parent.EndPoint;
            Vector3 branchEnd = branchStart + newDirection * branchLength;

            if (BranchPositions.Values.Any(ep => Vector3.Distance(ep, branchEnd) < 0.1f))
            {
                return null;
            }

            // Create a new branch
            int branchId = branches.Count + 1;
            TreeBranch newBranch = new TreeBranch(parent.Generation + 2, branchLength, branchRadius, branchId);
            newBranch.SetStartPoint(branchStart);
            newBranch.SetEndPoint(branchEnd);
            newBranch.SetTopRadius(branchRadius * 0.8f);
            
            // Add the new branch to the list of branches
            branches.Add(newBranch);
            // Add the new branch's end point to the dictionary
            BranchPositions[newBranch.BranchId] = branchEnd;

            if (parent.BranchId != 1)
            {
                AddColliderToBranch(newBranch);

                // Remove leaves from parent branch since it's no longer an end branch
                if (branchesWithLeaves.Contains(parent.BranchId))
                {
                    RemoveLeavesFromBranch(parent.BranchId);
                    branchesWithLeaves.Remove(parent.BranchId);
                }

                // Add leaves to new branch if it's an end branch
                if (!branches.Any(b => Vector3.Distance(b.StartPoint, newBranch.EndPoint) < 0.1f))
                {
                    if (leafGenerationCoroutine != null)
                    {
                        // Stop the existing coroutine if it exists
                        StopCoroutine(leafGenerationCoroutine); 
                    }
                    // Start the new coroutine to generate leaves
                    leafGenerationCoroutine = StartCoroutine(DelayedLeafGeneration(newBranch));
                }
            }
            // Invoke the event to notify listeners about the new branch
            OnBranchCreated?.Invoke(newBranch);
            return newBranch;
        }

        private void AddLeavesToBranch(TreeBranch branch)
        {
            // Check if the leaf prefab is set
            if (leafPrefab == null) return;
            // Calculate the number of leaves to add
            int leafCount = Mathf.RoundToInt(branch.Length * leafDensity * 10);
            // Calculate the direction of the branch
            Vector3 branchDirection = (branch.EndPoint - branch.StartPoint).normalized;

            // Calculate a consistent right vector for the branch
            Vector3 rightDirection = Vector3.Cross(branchDirection, Vector3.forward).normalized;
            if (rightDirection.magnitude < 0.001f)
            {   
                // If the cross product is too small, use the right vector
                rightDirection = Vector3.Cross(branchDirection, Vector3.right).normalized;
            }
            // Calculate forward direction
            Vector3 forwardDirection = Vector3.Cross(rightDirection, branchDirection);

            for (int i = 0; i < leafCount; i++)
            {
                // Calculate base position on the branch
                float t = GetRandomRange(0.3f, 1.0f);
                Vector3 basePosition = Vector3.Lerp(branch.StartPoint, branch.EndPoint, t);

                // Calculate position on branch surface
                float angle = GetRandomRange(0f, 360f) * Mathf.Deg2Rad;
                Vector3 surfaceOffset = (Mathf.Cos(angle) * rightDirection + Mathf.Sin(angle) * forwardDirection) * branch.Radius;
                Vector3 position = basePosition + surfaceOffset;

                // Calculate outward direction from branch center
                Vector3 outwardDirection = (position - basePosition).normalized;
                
                // Create rotation that points slightly upward and outward
                Quaternion baseRotation = Quaternion.LookRotation(
                    Vector3.Lerp(outwardDirection, branchDirection, 0.3f), // Blend between outward and up
                    Vector3.Lerp(branchDirection, outwardDirection, 0.7f)  // Blend between up and outward
                );

                // Add slight random variation
                Quaternion randomRotation = Quaternion.Euler(
                    GetRandomRange(-15f, 15f),
                    GetRandomRange(-30f, 30f),
                    GetRandomRange(-15f, 15f)
                );
                // Apply rotation to the leaf   
                Quaternion finalRotation = baseRotation * randomRotation;
                // Instantiate the leaf
                GameObject leaf = Instantiate(leafPrefab, position, finalRotation, transform);
                leaf.name = $"Leaf_Branch_{branch.BranchId}_{i}";
                leaf.transform.localScale = Vector3.one * leafSize;
                leaves.Add(leaf);
            }
            // Add the branch to the set of branches with leaves
            branchesWithLeaves.Add(branch.BranchId);
        }

        public int GrowBranchesFromInitial(int numberOfBranches, Dictionary<int, float> branchGrowth)
        {
            Debug.Log($"Growing up to {numberOfBranches} new branches from existing branches.");
            // Initialize the number of branches grown  
            int branchesGrown = 0;
            // Get all branches that are not the trunk
            List<TreeBranch> parentBranches = branches.Where(b => b.Generation > 1).ToList();

            while (branchesGrown < numberOfBranches && parentBranches.Count > 0)
            {   
                // Select a random parent branch
                TreeBranch parentBranch = parentBranches[UnityEngine.Random.Range(0, parentBranches.Count)];
                // Create a new branch from the selected parent
                TreeBranch newBranch = CreateBranch(parentBranch);
                // If the new branch was created successfully
                if (newBranch != null)
                {
                    // Increment the number of branches grown
                    branchesGrown++;
                    // Add the new branch to the dictionary with a growth value of 0
                    branchGrowth[newBranch.BranchId] = 0f;
                }
                // If the number of branches grown is greater than or equal to the number of branches to grow
                if (branchesGrown >= numberOfBranches) break;
            }
            // Update the mesh  
            RegenerateMesh();
            // Return the number of branches grown
            return branchesGrown;
        }

        private void AddColliderToBranch(TreeBranch branch)
        {
            // Create a new game object for the branch
            GameObject branchObject = new GameObject($"Branch_{branch.BranchId}");
            // Set the position of the branch object to the midpoint of the branch
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
            // Create a new mesh
            Mesh mesh = new Mesh();
            // Initialize lists for vertices, triangles, normals, uvs, and custom data
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector4> customData = new List<Vector4>();

            // Add each branch to the mesh
            foreach (TreeBranch branch in branches)
            {
                AddBranchToMesh(branch, vertices, triangles, normals, uvs);
            }
            // Add the branch indices to the custom data
            SetBranchIndices(vertices, customData);
            // Set the vertices, triangles, normals, uvs, and custom data of the mesh
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.SetUVs(3, customData);
            // Recalculate the bounds of the mesh
            mesh.RecalculateBounds();
            // Return the mesh
            return mesh;
        }

        private void AddBranchToMesh(TreeBranch branch, List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs)
            {
            // Calculate the number of segments for the branch
            int segments = 8;
            // Calculate the offset of the vertices
            int vertexOffset = vertices.Count;
            // Calculate the direction of the branch    
            Vector3 direction = (branch.EndPoint - branch.StartPoint).normalized;
            // Calculate the right direction of the branch
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right.magnitude < 0.001f)
            {
                right = Vector3.Cross(direction, Vector3.forward).normalized;
                }
            // Calculate the forward direction of the branch
            Vector3 forward = Vector3.Cross(right, direction);
            // Calculate the UV step
            float uvStep = 1.0f / segments;

            // Loop through each segment of the branch
            for (int i = 0; i <= segments; i++)
            {   
                // Calculate the angle of the segment
                float angle = i * (2 * Mathf.PI / segments);
                // Calculate the start offset of the segment
                Vector3 startOffset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * forward) * branch.Radius;
                // Calculate the end offset of the segment
                Vector3 endOffset = startOffset * (branch.TopRadius / branch.Radius);
                // Add the start and end points of the segment to the vertices list 
                vertices.Add(branch.StartPoint + startOffset);
                vertices.Add(branch.EndPoint + endOffset);
                // Add the normal of the segment to the normals list
                normals.Add(startOffset.normalized);
                normals.Add(endOffset.normalized);
                // Add the UVs of the segment to the uvs list   
                uvs.Add(new Vector2(i * uvStep, 0));
                uvs.Add(new Vector2(i * uvStep, 1));
                // If the segment is not the last segment   
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

        private IEnumerator DelayedLeafGeneration(TreeBranch branch)
        {
            // Wait for 3 seconds
            yield return new WaitForSeconds(3f);    
            // If the branch is still an end branch
            if (!branches.Any(b => Vector3.Distance(b.StartPoint, branch.EndPoint) < 0.1f))
            {
                AddLeavesToBranch(branch);
            }
        }

        private IEnumerator DelayedLeafGenerationForAll()
        {
            yield return new WaitForSeconds(3f); // Adjust delay time as needed
            
            foreach (var branch in branches)
            {
                // If the branch is still an end branch 
                if (!branches.Any(b => Vector3.Distance(b.StartPoint, branch.EndPoint) < 0.1f))
                {
                    AddLeavesToBranch(branch);
                }
            }
        }

        public void DeleteBranch(int branchId)
        {
            // If the branch is the trunk
            if (branchId == 1)
            {
                Debug.Log("Cannot delete trunk (branch 1)");
                return;
            }

            // Get all branches that will be deleted
            HashSet<int> branchesToDelete = new HashSet<int>();
            CollectBranchAndChildren(branchId, branchesToDelete);

            // Remove leaves first
            foreach (int id in branchesToDelete)
            {
                RemoveLeavesFromBranch(id);
                branchesWithLeaves.Remove(id);
            }

            // Remove branches
            branches.RemoveAll(b => branchesToDelete.Contains(b.BranchId));
            foreach (int id in branchesToDelete)
            {
                BranchPositions.Remove(id);
            }
            // Update the mesh
            RegenerateMesh();
        }

        private void CollectBranchAndChildren(int branchId, HashSet<int> collectedBranches)
        {
            // Add the branch to the collected branches
            collectedBranches.Add(branchId);
            // Find all child branches
            var childBranches = branches.Where(b => 
                Vector3.Distance(b.StartPoint, 
                branches.First(parent => parent.BranchId == branchId).EndPoint) < 0.1f);
            // Loop through each child branch
            foreach (var child in childBranches)
            {
                // If the child branch is not already collected
                if (!collectedBranches.Contains(child.BranchId))
                {
                    // Recursively collect the child branch and its children
                    CollectBranchAndChildren(child.BranchId, collectedBranches);
                }
            }
        }

        private void RemoveLeavesFromBranch(int branchId)
        {
            // Find all leaves under this transform that belong to the branch
            var branchLeaves = transform.GetComponentsInChildren<Transform>()
                .Where(t => t != null && 
                            t.gameObject != null && 
                            t.name.StartsWith($"Leaf_Branch_{branchId}_"))
                .Select(t => t.gameObject)  
                .ToList();
                
            // Destroy all leaves for this branch
            foreach (var leaf in branchLeaves)
            {
                if (leaf != null)
                {
                    DestroyImmediate(leaf);
                }
            }

            // Clean up our leaves list
            leaves.RemoveAll(leaf => leaf == null || branchLeaves.Contains(leaf));
        }

        private void ClearLeaves()
        {
            foreach (var leaf in leaves)
            {
                // If the leaf is not null
                if (leaf != null)
                {
                    DestroyImmediate(leaf);
                }   
            }
            // Clear the leaves list
            leaves.Clear();
        }

        private void AddBranchAndChildrenToDeletedSet(int branchId)
        {
            // Add the branch to the deleted branches set
            deletedBranchIds.Add(branchId);
            // Find all child branches
            var childBranches = branches.Where(b => b.BranchId.ToString().StartsWith(branchId.ToString()) && b.BranchId != branchId).ToList();
            // Loop through each child branch
            foreach (var childBranch in childBranches)
            {
                AddBranchAndChildrenToDeletedSet(childBranch.BranchId);
            }
        }

        internal void RebuildTree()
        {
            Debug.Log("Rebuilding tree...");
            // Create a new list for the branches
            List<TreeBranch> newBranches = new List<TreeBranch>();
            // Create a new dictionary for the branch positions
            Dictionary<int, Vector3> newBranchPositions = new Dictionary<int, Vector3>();

            // Loop through each branch
            foreach (TreeBranch branch in branches)
            {
                // If the branch is not in the deleted branches set
                if (!deletedBranchIds.Contains(branch.BranchId))
                {
                    newBranches.Add(branch);
                    newBranchPositions[branch.BranchId] = branch.EndPoint;
                }
            }
            // Update the branches and branch positions
            branches = newBranches;
            BranchPositions = newBranchPositions;

            RegenerateMesh();
        }

        public void RegenerateMesh()
        {
            // Create a new set for the branches with leaves
            HashSet<int> newBranchesWithLeaves = new HashSet<int>();
            // Loop through each branch 
            foreach (var branch in branches)
            {
                // If the branch is an end branch   
                if (!branches.Any(b => Vector3.Distance(b.StartPoint, branch.EndPoint) < 0.1f))
                {
                    newBranchesWithLeaves.Add(branch.BranchId);
                }
            }

            // Remove leaves from branches that are no longer end branches
            foreach (int branchId in branchesWithLeaves.Except(newBranchesWithLeaves))
            {
                RemoveLeavesFromBranch(branchId);
            }

            // Add leaves to new end branches
            foreach (int branchId in newBranchesWithLeaves.Except(branchesWithLeaves))
            {
                // Find the branch
                var branch = branches.Find(b => b.BranchId == branchId);
                if (branch != null)
                {
                    // If the leaf generation coroutine is not null
                    if (leafGenerationCoroutine != null)
                    {
                        StopCoroutine(leafGenerationCoroutine);
                    }
                    // Start the leaf generation coroutine
                    leafGenerationCoroutine = StartCoroutine(DelayedLeafGenerationForAll());
                }
            }
            // Update the branches with leaves
            branchesWithLeaves = newBranchesWithLeaves;
            // Update mesh
            Mesh mesh = CreateMeshFromBranches();
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                // Add a mesh filter component to the game object
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            // Update the mesh
            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                // Add a mesh renderer component to the game object
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            // If the mesh renderer's material is null
            if (meshRenderer.sharedMaterial == null)
            {
                // Add a material to the mesh renderer
                meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            }

            
        }

        public void ExportToPDF()
        {
            // Get the path to the documents folder
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // Create a file name for the PDF
            string fileName = $"ProceduralTree_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            // Combine the documents path and file name to create the destination path
            string destinationPath = Path.Combine(documentsPath, fileName);
            // Create a new document
            iTextSharp.text.Document pdfdoc = new iTextSharp.text.Document();
            // Set the page size to A3 and rotate it
            pdfdoc.SetPageSize(iTextSharp.text.PageSize.A3.Rotate());
            // If the file already exists
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
            // Create a new PDF writer
            var pdfwriter = iTextSharp.text.pdf.PdfWriter.GetInstance(pdfdoc, new FileStream(destinationPath, FileMode.CreateNew));
            // Open the document
            pdfdoc.Open();

            // Create base font once
            iTextSharp.text.pdf.BaseFont baseFont = iTextSharp.text.pdf.BaseFont.CreateFont(
                iTextSharp.text.pdf.BaseFont.HELVETICA,
                iTextSharp.text.pdf.BaseFont.CP1252,
                iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);
            // Create a new PDF content byte
            var cb = pdfwriter.DirectContent;
            // First page - Overview
            WritePDFPage(cb, baseFont, pdfdoc, true);

            // Second page - Branch details
            pdfdoc.NewPage();
            WritePDFPage(cb, baseFont, pdfdoc, false);

            pdfdoc.Close();
            Debug.Log($"PDF exported to: {destinationPath}");
        }

        private void WritePDFPage(PdfContentByte cb, BaseFont font, Document pdfdoc, bool isFirstPage)
        {
            // Set the y position to the bottom of the page minus 50
            float yPosition = pdfdoc.PageSize.Height - 50;

            if (!isFirstPage)
            {
                // Branch details page header
                cb.SetFontAndSize(font, 20);
                WriteText(cb, "Branch Details", 50, yPosition);
                yPosition -= 40;

                cb.SetFontAndSize(font, 12);
                int branchesPerPage = 6; // Reduced to fit more details
                int currentBranch = 0;

                foreach (var branch in branches)
                {
                    if (currentBranch > 0 && currentBranch % branchesPerPage == 0)
                    {
                        // Create a new page
                        pdfdoc.NewPage();
                        // Set the y position to the bottom of the page minus 50
                        yPosition = pdfdoc.PageSize.Height - 50;
                        // Set the font size to 12
                        cb.SetFontAndSize(font, 12);
                    }

                    // Branch header with brown color
                    cb.SetColorStroke(new BaseColor(101, 67, 33));
                    WriteText(cb, $"Branch {branch.BranchId}:", 50, yPosition);
                    yPosition -= 25;

                    // Basic information in columns
                    cb.SetColorStroke(new BaseColor(0, 0, 0));
                    WriteText(cb, $"  Generation: {branch.Generation}", 70, yPosition);
                    WriteText(cb, $"  Length: {branch.Length:F2}", 300, yPosition);
                    WriteText(cb, $"  Radius: {branch.Radius:F2}", 500, yPosition);
                    yPosition -= 25;

                    // Connected branches
                    var childBranches = branches.Where(b => 
                        Vector3.Distance(b.StartPoint, branch.EndPoint) < 0.1f).ToList();
                    var parentBranch = branches.FirstOrDefault(b => 
                        Vector3.Distance(b.EndPoint, branch.StartPoint) < 0.1f);
                    // Write the parent branch ID   
                    WriteText(cb, $"  Parent Branch: {(parentBranch != null ? parentBranch.BranchId.ToString() : "None")}", 
                        70, yPosition);
                    yPosition -= 20;
                    
                    WriteText(cb, $"  Child Branches: {(childBranches.Any() ? string.Join(", ", childBranches.Select(b => b.BranchId)) : "None")}", 
                        70, yPosition);
                    yPosition -= 25;

                    // Metadata
                    if (branch.Tags.Any())
                    {
                        WriteText(cb, $"  Tags: {string.Join(", ", branch.Tags)}", 70, yPosition);
                        yPosition -= 20;
                    }
                    // Write the category if it is not null or empty
                    if (!string.IsNullOrEmpty(branch.Category))
                    {
                        WriteText(cb, $"  Category: {branch.Category}", 70, yPosition);
                        yPosition -= 20;
                    }

                    // Add spacing between branches
                    yPosition -= 30;
                    // Increment the current branch
                    currentBranch++;
                }
            }
            else
            {
                // Title and overview section
                cb.SetFontAndSize(font, 24);
                WriteText(cb, "Procedural Tree Export", 50, yPosition);
                yPosition -= 50;

                // Tree statistics
                cb.SetFontAndSize(font, 12);
                WriteText(cb, $"Total Branches: {branches.Count}", 50, yPosition);
                yPosition -= 20;
                WriteText(cb, $"Generations: {generations}", 50, yPosition);
                yPosition -= 20;
                WriteText(cb, $"Trunk Length: {trunkLength:F2}", 50, yPosition);
                yPosition -= 20;
                WriteText(cb, $"Trunk Radius: {trunkRadius:F2}", 50, yPosition);
                yPosition -= 40;

                // Draw tree visualization
                DrawTreeDiagram(cb, yPosition);
            }
        }

        private void WriteText(iTextSharp.text.pdf.PdfContentByte cb, string text, float x, float y)
        {
            cb.BeginText();
            cb.SetTextMatrix(x, y);
            cb.ShowText(text);
            cb.EndText();
        }

        private void DrawTreeDiagram(PdfContentByte cb, float startY)
        {
            float scale = 50f; // Increased scale
            float centerX = cb.PdfDocument.PageSize.Width / 2;
            float maxY = startY;
            float minY = 50;
            float availableHeight = maxY - minY;
            
            // Calculate bounds of the tree
            float minTreeX = float.MaxValue;
            float maxTreeX = float.MinValue;
            float minTreeY = float.MaxValue;
            float maxTreeY = float.MinValue;

            // Loop through each branch
            foreach (var branch in branches)
            {
                minTreeX = Mathf.Min(minTreeX, branch.StartPoint.x, branch.EndPoint.x);
                maxTreeX = Mathf.Max(maxTreeX, branch.StartPoint.x, branch.EndPoint.x);
                minTreeY = Mathf.Min(minTreeY, branch.StartPoint.y, branch.EndPoint.y);
                maxTreeY = Mathf.Max(maxTreeY, branch.StartPoint.y, branch.EndPoint.y);
            }
            
            // Calculate scaling factor to fit the tree
            float treeWidth = maxTreeX - minTreeX;
            float treeHeight = maxTreeY - minTreeY;
            float scaleX = (cb.PdfDocument.PageSize.Width * 0.8f) / treeWidth; // Increased width usage
            float scaleY = availableHeight / treeHeight;
            scale = Mathf.Min(scaleX, scaleY) * 0.9f; // Increased scale factor
            
            // Draw branches from thickest to thinnest
            foreach (var branch in branches.OrderByDescending(b => b.Radius))
            {
                // Calculate positions
                float x1 = centerX + (branch.StartPoint.x - minTreeX - treeWidth/2) * scale;
                float y1 = startY - availableHeight + (branch.StartPoint.y - minTreeY) * scale;
                float x2 = centerX + (branch.EndPoint.x - minTreeX - treeWidth/2) * scale;
                float y2 = startY - availableHeight + (branch.EndPoint.y - minTreeY) * scale;
                
                // Draw branch with increased thickness
                cb.SetLineWidth(branch.Radius * scale * 0.4f); // Doubled thickness
                cb.SetColorStroke(new BaseColor(101, 67, 33)); // Brown color
                cb.MoveTo(x1, y1);
                cb.LineTo(x2, y2);
                cb.Stroke();
                
                // Add larger circle at branch end
                float circleRadius = branch.Radius * scale * 0.2f; // Doubled circle size
                cb.Circle(x2, y2, circleRadius);
                cb.Stroke();
                
                // Add branch ID text with larger font and offset
                cb.SetColorStroke(new BaseColor(0, 0, 0));
                cb.BeginText();
                cb.SetFontAndSize(BaseFont.CreateFont(), 12); // Increased font size
                cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, 
                    branch.BranchId.ToString(), 
                    x2 + circleRadius + 10, // Increased text offset
                    y2, 
                    0);
                cb.EndText();
            }
        }

        public void TagBranch(int branchId, string tag)
        {
            // Find the branch  
            TreeBranch branch = branches.Find(b => b.BranchId == branchId);
            if (branch != null)
            {
                branch.AddTag(tag);
                RegenerateMesh();
            }
        }

        public void RemoveTagFromBranch(int branchId, string tag)
        {
            // Find the branch  
            TreeBranch branch = branches.Find(b => b.BranchId == branchId);
            if (branch != null)
            {
                branch.RemoveTag(tag);
                RegenerateMesh();
            }
        }

        public void SetBranchCategory(int branchId, string category)
        {
            // Find the branch
            TreeBranch branch = branches.Find(b => b.BranchId == branchId);
            if (branch != null)
            {
                branch.SetCategory(category);
                RegenerateMesh();
            }
        }

        public List<TreeBranch> GetBranchesByCategory(string category)
        {
            // Return the branches that have the specified category 
            return branches.Where(b => b.Category == category).ToList();
        }

        public List<TreeBranch> GetBranchesByTag(string tag)
        {
            // Return the branches that have the specified tag  
            return branches.Where(b => b.Tags.Contains(tag)).ToList();
        }

        public string SerializeTreeData()
        {
            // Create a new anonymous object with the branches data
            var treeData = new
            {
                Branches = branches.Select(b => new
                {
                    BranchId = b.BranchId,
                    StartPoint = b.StartPoint.ToString(),
                    EndPoint = b.EndPoint.ToString(),
                    Tags = b.Tags,
                    Category = b.Category
                }).ToList()
            };

            // Serialize the tree data to JSON
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

        public void Setup(System.Random randomGenerator)
        {
            rnd = randomGenerator;
        }

        public int GetRandomBranches()
        {
            return rnd.Next(branchesMin, branchesMax + 1);
        }

        public float GetRandomGrowthAngle(System.Random randomGenerator)
        {
            return growthAngleMin + (float)(randomGenerator.NextDouble()) * (growthAngleMax - growthAngleMin);
        }

        public float GetRandomBendDegree(System.Random randomGenerator)
        {
            return -bendDegree + (float)(randomGenerator.NextDouble()) * (bendDegree * 2);
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