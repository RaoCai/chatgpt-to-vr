using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.UI;
using UnityEngine;
//using UnityEngine.XR.Interaction.Toolkit;
using Oculus.Interaction;
using System.Threading.Tasks;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using Unity.VisualScripting;
using static ProceduralModeling.ProceduralTree;

namespace ProceduralModeling
{

    public class ProceduralTree : ProceduralModelingBase
    {
        
        private TreeBranch latestBranch;
        public TreeData Data { get { return data; } set { data = value; } }
        private TreeBranch root;
        public List<TreeBranch> activeBranches = new List<TreeBranch>();
        private bool meshNeedsUpdate = true;

        [SerializeField] TreeData data;
        [SerializeField, Range(2, 8)] protected int generations = 5;
        [SerializeField, Range(0.5f, 5f)] protected float length = 1f;
        [SerializeField, Range(0.1f, 2f)] protected float radius = 0.15f;
        private List<InteractiveBranch> interactiveBranches = new List<InteractiveBranch>();

        const float PI2 = Mathf.PI * 2f;

        private void Start()
        {
            if (data == null)
            {
                Debug.LogError("TreeData is not assigned!");
                return;
            }
            data.Setup();
        }

        private Mesh BuildTrunk(){
            data.Setup();
            var toRadius = radius * 0.1f;
            root = new TreeBranch(
                generations,
                length,
                radius,
                Vector3.zero,
                Vector3.up,
                data
            );
            Vector3 initialBranchDirection = GetRandomGrowthDirection(root);
            var initialBranch = new TreeBranch(
                generations - 1,
                length * data.lengthAttenuation,
                radius * data.radiusAttenuation,
                root.To,
                initialBranchDirection,
                data
            );

            root.Children.Add(initialBranch);
            activeBranches.Add(root);
            activeBranches.Add(initialBranch);
            return BuildMeshFromBranches(new List<TreeBranch>() { root, initialBranch });
        } 

        protected override Mesh Build()
        {
            return BuildTrunk();
        }

        private Mesh BuildMeshFromBranches(List<TreeBranch> branches)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            foreach (var branch in branches)
            {
                int baseIndex = vertices.Count;
                for (int i = 0; i < branch.Segments.Count; i++)
                {
                    var segment = branch.Segments[i];
                    Vector3[] segmentVertices = CreateCircle(segment.Position, segment.Frame.Normal, segment.Frame.Binormal, segment.Radius, data.radialSegments);
                    vertices.AddRange(segmentVertices);

                    // Calculate smooth normals
                    Vector3 segmentDirection = i < branch.Segments.Count - 1 ? 
                        (branch.Segments[i + 1].Position - segment.Position).normalized : 
                        (branch.To - segment.Position).normalized;

                    for (int j = 0; j < segmentVertices.Length; j++)
                    {
                        Vector3 normal = (segmentVertices[j] - segment.Position).normalized;
                        normal = Vector3.Slerp(normal, segmentDirection, 0.5f).normalized;
                        normals.Add(normal);
                    }

                    if (i > 0)
                    {
                        // Connect this segment to the previous one
                        for (int j = 0; j < data.radialSegments; j++)
                        {
                            int nextJ = (j + 1) % data.radialSegments;
                            int current = baseIndex + i * data.radialSegments + j;
                            int next = baseIndex + i * data.radialSegments + nextJ;
                            int previous = current - data.radialSegments;
                            int previousNext = next - data.radialSegments;

                            triangles.AddRange(new int[] { previous, current, next });
                            triangles.AddRange(new int[] { previous, next, previousNext });
                        }
                    }
                }

                // Add cap at the end of the branch
                AddBranchCap(vertices, triangles, normals, branch.Segments.Last(), data.radialSegments);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void AddBranchCap(List<Vector3> vertices, List<int> triangles, List<Vector3> normals, TreeSegment lastSegment, int radialSegments)
        {
            int centerIndex = vertices.Count;
            vertices.Add(lastSegment.Position);
            normals.Add(lastSegment.Frame.Normal); // Assuming Frame has a Normal property

            for (int i = 0; i < radialSegments; i++)
            {
                int current = centerIndex - radialSegments + i;
                int next = centerIndex - radialSegments + (i + 1) % radialSegments;
                triangles.AddRange(new int[] { centerIndex, next, current });
            }
        }

        private Vector3[] CreateCircle(Vector3 center, Vector3 normal, Vector3 binormal, float radius, int segments)
        {
            Vector3[] vertices = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[i] = center + (normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle)) * radius;
            }
            return vertices;
        }


        static float TraverseMaxLength(TreeBranch branch)
        {
            float max = 0f;
            branch.Children.ForEach(c =>
            {
                max = Mathf.Max(max, TraverseMaxLength(c));
            });
            return branch.Length + max;
        }

        static void Traverse(TreeBranch from, Action<TreeBranch> action)
        {
            if (from.Children.Count > 0)
            {
                from.Children.ForEach(child =>
                {
                    Traverse(child, action);
                });
            }
            action(from);
        }

        private void CreateInteractiveBranches(TreeBranch branch, string result)
        {
            if (branch == null)
            {
                Debug.LogError("Attempted to create interactive branch with null TreeBranch");
                return;
            }

            GameObject branchObject = new GameObject($"Branch_{interactiveBranches.Count} {result}");
            if (branchObject == null)
            {
                Debug.LogError("Failed to create GameObject for interactive branch");
                return;
            }

            branchObject.transform.SetParent(transform);
            branchObject.transform.position = branch.From;

            InteractiveBranch interactiveBranch = branchObject.AddComponent<InteractiveBranch>();
            if (interactiveBranch == null)
            {
                Debug.LogError("Failed to add InteractiveBranch component");
                return;
            }

            interactiveBranch.Initialize(branch, result);

            OVRGrabbable grabbable = branchObject.AddComponent<OVRGrabbable>();
            if (grabbable == null)
            {
                Debug.LogError("Failed to add OVRGrabbable component");
                return;
            }

            SphereCollider sphereCollider = branchObject.AddComponent<SphereCollider>();
            if (sphereCollider == null)
            {
                Debug.LogError("Failed to add SphereCollider component");
                return;
            }

            sphereCollider.radius = 0.05f;

            var grabPointsField = typeof(OVRGrabbable).GetField("m_grabPoints", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (grabPointsField == null)
            {
                Debug.LogError("Failed to find m_grabPoints field in OVRGrabbable");
                return;
            }

            grabPointsField.SetValue(grabbable, new Collider[] { sphereCollider });

            interactiveBranches.Add(interactiveBranch);
        }

        private string GenerateRandomResult()
        {
            return UnityEngine.Random.Range(1, 101).ToString();
        }

        public async Task<TreeBranch> GrowBranchFromSearch(string searchQuery)
        {
            Debug.Log($"GrowBranchFromSearch called with query: {searchQuery}");
            string result = await FetchResultFromInternet(searchQuery);

            if (!string.IsNullOrEmpty(result))
            {
                Debug.Log($"Search result: {result}");
                
                TreeBranch newBranch = AddNewBranch();
                
                if (newBranch != null)
                {
                    CreateInteractiveBranches(newBranch, result);
                    
                    Debug.Log($"New branch added. Total branches: {activeBranches.Count}");
                    UpdateMesh();

                    return newBranch;
                }
                else
                {
                    Debug.LogWarning("Failed to add new branch");
                }
            }
            else
            {
                Debug.LogWarning("Search result was empty or null");
            }

            return null;
        }

        private void UpdateInteractiveBranches()
        {
            // Update or create interactive branches for all active branches
            for (int i = 0; i < activeBranches.Count; i++)
            {
                if (i < interactiveBranches.Count)
                {
                    interactiveBranches[i].UpdatePosition(activeBranches[i].To);
                    interactiveBranches[i].UpdateScale(1f);
                }
                else
                {
                    CreateInteractiveBranches(activeBranches[i], GenerateRandomResult());
                }
            }
        }

        public void DeleteBranch(InteractiveBranch branch)
        {
            int index = interactiveBranches.IndexOf(branch);
            if (index != -1)
            {
                for (int i = interactiveBranches.Count - 1; i >= index; i--)
                {
                    Destroy(interactiveBranches[i].gameObject);
                    interactiveBranches.RemoveAt(i);
                    activeBranches.RemoveAt(i);
                }

                if (index == 0)
                {
                    root.Children.Clear();
                }
                else
                {
                    activeBranches[index - 1].Children.Clear();
                }

                UpdateMesh();
            }
        }

        public void ResetTree()
        {
            foreach (var branch in interactiveBranches)
            {
                Destroy(branch.gameObject);
            }
            interactiveBranches.Clear();
            activeBranches.Clear(); 
            root.Children.Clear();
            UpdateMesh();
        }

        

        private async Task<string> FetchResultFromInternet(string searchQuery)
        {
            //this is just a simulate need to implement the real fetch result from the internet
            await Task.Delay(1000);
            return $"Result: {searchQuery}";
        }

        private TreeBranch FindClosestBranch(Vector3 position)
        {
            TreeBranch closestBranch = null;
            float closestDistance = float.MaxValue;

            foreach (var branch in activeBranches)
            {
                float distance = Vector3.Distance(position, branch.To);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBranch = branch;
                }
            }

            return closestBranch ?? root;
        }

        public void GrowBranch(float growthProgress)
        {
             if (data == null)
            {
                Debug.LogError("TreeData is null in GrowBranch");
                return;
            }

            foreach (var branch in activeBranches)
            {
                branch.UpdateGrowth(growthProgress);
            }

            UpdateMesh();
            UpdateInteractiveBranches();
        }

        public TreeBranch AddNewBranch()
        {
            TreeBranch newBranch = null;

            if (activeBranches.Count == 0)
            {
                // Create the initial trunk
                root = new TreeBranch(generations, length, radius, Vector3.zero, Vector3.up, data);
                activeBranches.Add(root);
                newBranch = root;
            }
            else
            {
                TreeBranch parentBranch;
                Vector3 growthDirection;
                float newLength;
                float newRadius;

                if (activeBranches.Count <= data.trunkSegments)
                {
                    // Continue the trunk growth, but shorter
                    parentBranch = activeBranches[activeBranches.Count - 1];
                    growthDirection = Vector3.up + (UnityEngine.Random.insideUnitSphere * 0.1f); // Slight randomness
                    newLength = length * 0.8f; // Shorter segments
                    newRadius = radius * 0.9f; // Slight taper
                }
                else
                {
                    // Create a new branch
                    parentBranch = FindSuitableParentBranch();
                    growthDirection = GetRandomGrowthDirection(parentBranch);
                    newLength = length * data.lengthAttenuation;
                    newRadius = radius * data.radiusAttenuation;
                }

                if (parentBranch != null)
                {
                    newBranch = new TreeBranch(
                        parentBranch.Generation - 1,
                        newLength,
                        newRadius,
                        parentBranch.To,
                        growthDirection,
                        data
                    );
                    
                    parentBranch.Children.Add(newBranch);
                    activeBranches.Add(newBranch);
                }
                else
                {
                    Debug.LogError("Failed to find a parent branch. This should never happen.");
                    return null;
                }
            }

            meshNeedsUpdate = true;
            UpdateMesh();
            return newBranch;
        }

        private TreeBranch FindSuitableParentBranch()
        {
            if (activeBranches.Count == 0) return null;

            List<TreeBranch> suitableBranches = new List<TreeBranch>();
            
            foreach (var branch in activeBranches)
            {
                if (branch.Children.Count < data.branchesMax && branch.Generation > 0)
                {
                    suitableBranches.Add(branch);
                }
            }

            if (suitableBranches.Count == 0)
            {
                return activeBranches[UnityEngine.Random.Range(0, activeBranches.Count)];
            }

            return suitableBranches[UnityEngine.Random.Range(0, suitableBranches.Count)];
        }

        private Vector3 GetRandomGrowthDirection(TreeBranch parentBranch)
        {
            Vector3 parentDirection = (parentBranch.To - parentBranch.From).normalized;
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
            randomDirection.y = Mathf.Abs(randomDirection.y); // Ensure upward growth
            return Vector3.Slerp(parentDirection, randomDirection, 0.5f).normalized;
        }

        public void UpdateMesh()
        {
            if (!meshNeedsUpdate) return;

            Mesh updatedMesh = BuildMeshFromBranches(GetAllBranches());
            GetComponent<MeshFilter>().mesh = updatedMesh;
            
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = updatedMesh;
            }
            else
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = updatedMesh;
            }

            meshNeedsUpdate = false;
        }

        private List<TreeBranch> GetAllBranches()
        {
            List<TreeBranch> allBranches = new List<TreeBranch>();
            void TraverseBranches(TreeBranch branch)
            {
                allBranches.Add(branch);
                foreach (var child in branch.Children)
                {
                    TraverseBranches(child);
                }
            }
            if (root != null) TraverseBranches(root);
            return allBranches;
        }



        public class InteractiveBranch : OVRGrabbable
        {
            public TreeBranch TreeBranch { get; private set; }
            public string Result { get; set; }
            private GameObject popupUI;

            public void Initialize(TreeBranch treeBranch, string result)
            {
                TreeBranch = treeBranch;
                Result = result;

                CreatePopupUI();
            }

            protected override void Start()
            {
                base.Start();
            }

            public void UpdatePosition(Vector3 position)
            {
                transform.position = position;
            }

            public void UpdateScale(float scale)
            {
                transform.localScale = Vector3.one * scale;
            }

            private void CreatePopupUI()
            {
                // Create a UI canvas for the branch
                popupUI = new GameObject("BranchPopup");
                GameObject buttonObj = new GameObject("DeleteButton");

                popupUI.transform.SetParent(transform);
                buttonObj.transform.SetParent(popupUI.transform);

                popupUI.transform.localPosition = Vector3.up * 0.2f;

                Canvas canvas = popupUI.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;

                Button deleteButton = buttonObj.AddComponent<Button>();
                Image buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = Color.red;

                RectTransform rectTransform = canvas.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(0.2f, 0.1f);

                Image background = popupUI.AddComponent<Image>();
                background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                GameObject textObj = new GameObject("ResultText");
                textObj.transform.SetParent(popupUI.transform);

                // Delete button
                Text buttonText = new GameObject("ButtonText").AddComponent<Text>();
                buttonText.transform.SetParent(deleteButton.transform);
                buttonText.text = "Delete Branch";
                buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.color = Color.white;

                RectTransform buttonRectTransform = deleteButton.GetComponent<RectTransform>();
                buttonRectTransform.anchorMin = new Vector2(0.5f, 0);
                buttonRectTransform.anchorMax = new Vector2(0.5f, 0);
                buttonRectTransform.anchoredPosition = new Vector2(0, 10);
                buttonRectTransform.sizeDelta = new Vector2(80, 30);

                // Result text
                Text resultText = textObj.AddComponent<Text>();
                resultText.text = $"Result: {Result}";
                resultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                resultText.alignment = TextAnchor.MiddleCenter;
                resultText.color = Color.white;

                RectTransform textRectTransform = resultText.GetComponent<RectTransform>();
                textRectTransform.anchorMin = Vector2.zero;
                textRectTransform.anchorMax = Vector2.one;
                textRectTransform.offsetMin = Vector2.zero;

                deleteButton.onClick.AddListener(DeleteBranch);
                popupUI.SetActive(false);
            }

            public override void GrabBegin(OVRGrabber hand, Collider grabPoint)
            {
                base.GrabBegin(hand, grabPoint);
                Debug.Log($"Branch Selected. Result: {Result}");
                popupUI.SetActive(true);
            }

            public override void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
            {
                base.GrabEnd(linearVelocity, angularVelocity);
                Debug.Log("Branch Released");
                popupUI.SetActive(false);
            }

            public void DeleteBranch()
            {
                var tree = GetComponentInParent<ProceduralTree>();
                if (tree != null)
                {
                    tree.DeleteBranch(this);
                }
            }

            public void Activate()
            {
                DeleteBranch();
            }
        }

        [System.Serializable]
        public class TreeData
        {
            public int randomSeed = 0;
            [Range(0.25f, 0.95f)] public float lengthAttenuation = 0.8f, radiusAttenuation = 0.5f;
            [Range(1, 3)] public int branchesMin = 1, branchesMax = 2;
            [Range(-45f, 0f)] public float growthAngleMin = -15f;
            [Range(0f, 45f)] public float growthAngleMax = 15f;
            [Range(1f, 10f)] public float growthAngleScale = 4f;
            [Range(4, 10)] public int heightSegments = 6, radialSegments = 6;
            [Range(0.0f, 0.35f)] public float bendDegree = 0.1f;

            public float trunkLengthMultiplier = 1.5f; // Adjust to make the trunk longer
            public int trunkSegments = 2;

            Rand rnd;

            public void Setup()
            {
                rnd = new Rand(randomSeed);
            }

            public int Range(int a, int b)
            {
                return rnd.Range(a, b);
            }

            public float Range(float a, float b)
            {
                return rnd.Range(a, b);
            }

            public int GetRandomBranches()
            {
                return rnd.Range(branchesMin, branchesMax + 1);
            }

            public float GetRandomGrowthAngle()
            {
                return rnd.Range(growthAngleMin, growthAngleMax);
            }

            public float GetRandomBendDegree()
            {
                return rnd.Range(-bendDegree, bendDegree);
            }
        }

        public class TreeBranch
        {
            public int Generation { get; set; }
            public List<TreeSegment> Segments { get; set; }
            public List<TreeBranch> Children { get; set; }
            public Vector3 From { get; set; }
            public Vector3 To { get; set; }
            public float Length { get; set; }
            public float Offset { get { return offset; } }

            public List<Vector3> IntermediatePoints { get; private set; }
        public List<float> IntermediateRadii { get; private set; }

            private TreeData data;

            int generation;

            List<TreeSegment> segments;
            List<TreeBranch> children;

            Vector3 from, to;
            float fromRadius, toRadius;
            float length;
            float offset;

            public TreeBranch(int generation, float length, float radius, Vector3 startPoint, Vector3 direction, TreeData data)
            {
                this.Generation = generation;
                this.Length = length;
                this.fromRadius = radius;
                this.toRadius = radius * data.radiusAttenuation;
                this.From = startPoint;
                this.To = startPoint + direction * length;
                this.data = data;

                Children = new List<TreeBranch>();
                Segments = BuildSegments();
            }

            public TreeBranch(Vector3 from, Vector3 to, float fromRadius, float toRadius, int segments)
            {
                From = from;
                To = to;
                fromRadius = fromRadius;
                toRadius = toRadius;
                IntermediatePoints = new List<Vector3>();
                IntermediateRadii = new List<float>();

                for (int i = 1; i < segments; i++)
                {
                    float t = i / (float)segments;
                    IntermediatePoints.Add(Vector3.Lerp(from, to, t));
                    IntermediateRadii.Add(Mathf.Lerp(fromRadius, toRadius, t));
                }
            }

            private List<TreeSegment> BuildSegments()
            {
                var segments = new List<TreeSegment>();
                var points = new List<Vector3>();

                Vector3 direction = (To - From).normalized;
                Vector3 normal = Vector3.Cross(direction, Vector3.up).normalized;
                if (normal == Vector3.zero)
                {
                    normal = Vector3.Cross(direction, Vector3.right).normalized;
                }
                Vector3 binormal = Vector3.Cross(direction, normal).normalized;

                // Add slight curve to the branch
                float bendFactor = data.GetRandomBendDegree();
                Vector3 bendVector = (normal * UnityEngine.Random.Range(-1f, 1f) + binormal * UnityEngine.Random.Range(-1f, 1f)).normalized * bendFactor;

                points.Add(From);
                points.Add(Vector3.Lerp(From, To, 0.25f) + bendVector * 0.25f);
                points.Add(Vector3.Lerp(From, To, 0.75f) + bendVector * 0.25f);
                points.Add(To);

                var curve = new CatmullRomCurve(points);
                var frames = curve.ComputeFrenetFrames(data.heightSegments, normal, binormal, false);

                for (int i = 0; i < frames.Count; i++)
                {
                    float t = i / (float)(frames.Count - 1);
                    float radius = Mathf.Lerp(fromRadius, toRadius, t);
                    Vector3 position = curve.GetPointAt(t);

                    segments.Add(new TreeSegment(frames[i], position, radius));
                }

                return segments;
            }

            public void UpdateGrowth(float growthProgress)
            {
                Vector3 growthVector = (To - From) * growthProgress;
                Vector3 currentEndPoint = From + growthVector;
                toRadius = Mathf.Lerp(fromRadius, toRadius, growthProgress);
                Segments = BuildSegments();

                foreach (var child in Children)
                {
                    child.From = currentEndPoint;
                    child.UpdateGrowth(growthProgress);
                }
            }
        }

        public class TreeSegment
        {
            public FrenetFrame Frame { get { return frame; } }
            public Vector3 Position { get { return position; } }
            public float Radius { get { return radius; } }

            FrenetFrame frame;
            Vector3 position;
            float radius;

            public TreeSegment(FrenetFrame frame, Vector3 position, float radius)
            {
                this.frame = frame;
                this.position = position;
                this.radius = radius;
            }
        }

        public class Rand
        {
            System.Random rnd;

            public float value
            {
                get
                {
                    return (float)rnd.NextDouble();
                }
            }

            public Rand(int seed)
            {
                rnd = new System.Random(seed);
            }

            public int Range(int a, int b)
            {
                var v = value;
                return Mathf.FloorToInt(Mathf.Lerp(a, b, v));
            }

            public float Range(float a, float b)
            {
                var v = value;
                return Mathf.Lerp(a, b, v);
            }
        }

    }
}