using CommNet.Network;
using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using static System.Collections.Specialized.BitVector32;
using static Targeting;

namespace VertexBasedProceduralParts
{
    public class VBProceduralParts : PartModule, IPartCostModifier, IPartMassModifier
    {
        static List<Vector3> crossSectionClipboard;

        private GameObject meshObject;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        bool updatingFromSymmetry = false;

        public Part GetEldestParent(Part p) => (p.parent is null) ? p : GetEldestParent(p.parent);

        [KSPField(isPersistant = true)]
        public string bottomVerticesData = "";

        [KSPField(isPersistant = true)]
        public string topVerticesData = "";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Length")]
        [UI_FloatRange(minValue = 0.01f, maxValue = 10f, stepIncrement = 0.01f)]
        public float l = 1.5f;

        [KSPField(guiActiveEditor = true, guiName = "Selected Ring")]
        [UI_FloatRange(minValue = 0, maxValue = 1, stepIncrement = 1)]
        public float selectedRing = 0;

        [KSPField(guiActiveEditor = true, guiName = "Selected Vertex")]
        [UI_FloatRange(minValue = 0, maxValue = 23, stepIncrement = 1)]
        public float selectedVertex;

        [KSPField(guiActiveEditor = true, guiName = "Vertex X")]
        [UI_FloatRange(minValue = -2f, maxValue = 5f, stepIncrement = 0.001f)]
        public float vertexX;

        [KSPField(guiActiveEditor = true, guiName = "Vertex Y")]
        [UI_FloatRange(minValue = -2f, maxValue = 5f, stepIncrement = 0.001f)]
        public float vertexY;

        [KSPField(guiActiveEditor = true, guiName = "Vertex Z")]
        [UI_FloatRange(minValue = -2f, maxValue = 5f, stepIncrement = 0.001f)]
        public float vertexZ;

        [KSPField(guiActiveEditor = true, guiName = "Mirror Symmetry")]
        [UI_Toggle(disabledText = "False", enabledText = "True")]
        bool isSymmetry = false;

        [KSPField(guiActiveEditor = true, isPersistant = true)]
        bool isFlipppedNormals = false;

        [KSPField(guiActiveEditor = true, isPersistant = true)]
        bool isFlatShading = false;

        [KSPField(guiActive = false, guiActiveEditor = true, guiFormat = "F2", guiName = "Dry Mass")]
        private float dryMass;

        [KSPField(guiActive = false, guiActiveEditor = true, guiFormat = "F2", guiName = "Cost")]
        private float dryCost;

        [KSPEvent(guiActiveEditor = true, guiName = "Reset Bottom")]
        public void ResetBottomToCylinder()
        {
            ResetToCylinder(bottomSection, 1.25f);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Reset Top")]
        public void ResetTopToCylinder()
        {
            ResetToCylinder(topSection, 1.25f);
        }
        [KSPEvent(guiActiveEditor = true, guiName = "Cap Bottom")]
        public void CapBottom()
        {
            MakeCap(bottomSection);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Cap Top")]
        public void CapTop()
        {
            MakeCap(topSection);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Copy Cross-Section to Clipboard")]
        public void CopyCrossSection()
        {
            CrossSection section = GetActiveSection();

            CopyToClipboard(section);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Paste Cross-Section from Clipboard")]
        public void PasteCrossSection()
        {
            CrossSection section = GetActiveSection();

            PasteFromClipboard(section);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Flip Mesh Normals")]
        void FlipMesh()
        {
            isFlipppedNormals = !isFlipppedNormals;
            GenerateMesh();
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Toggle Flat Shading")]
        void ToggleFlatShading()
        {
            isFlatShading = !isFlatShading;
            GenerateMesh();
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Toggle 2D Editor")]
        void Toggle2DEditor()
        {
            isWindowOpen = !isWindowOpen;
        }

        int lastSelected = -1;
        int lastSelectedRing = -1;
        Vector3 lastSliderValue;

        Vector3 newSlider;

        Vector2 lastV0;

        public CrossSection bottomSection;
        public CrossSection topSection;
        RingCreator ringCreator;
        Lofter lofter;
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            bottomSection = new CrossSection(24, 1.25f);
            topSection = new CrossSection(24, 1.25f);

            AttachSlider("l");
            AttachSlider("vertexX");
            AttachSlider("vertexY");
            AttachSlider("vertexZ");
            if (!string.IsNullOrEmpty(bottomVerticesData))
                bottomSection.vertices = DeserializeVertices(bottomVerticesData);
            if (!string.IsNullOrEmpty(topVerticesData))
                topSection.vertices = DeserializeVertices(topVerticesData);

            ringCreator = new RingCreator();
            lofter = new Lofter();
            CreateObject();
            GenerateMesh();
        }
        public void Start()
        {
            UpdateAttachmentNodes(1.5f, 1.5f);
        }
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return dryMass;
        }
        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return dryCost;
        }
        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }
        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }
        public void UpdateMass()
        {
            dryMass = (float)(Math.PI * 0.625f * 0.625f * l * 0.1f);
            dryCost = (float)dryMass * 1000f;
        }
        public void CopyToClipboard(CrossSection section)
        {
            crossSectionClipboard = new List<Vector3>(section.vertices);
        }
        public void PasteFromClipboard(CrossSection section)
        {
            if (crossSectionClipboard == null) return;

            section.vertices = new List<Vector3>(crossSectionClipboard);

            GenerateMesh();
        }
        void ResetToCylinder(CrossSection section, float d)
        {
            int n = section.vertices.Count;

            Quaternion rotation = Quaternion.AngleAxis(360f / n, Vector3.up);

            Vector3 v = Vector3.right * d / 2f;

            for (int i = 0; i < n; i++)
            {
                section.vertices[i] = new Vector2(v.x, v.z);
                v = rotation * v;
            }

            GenerateMesh();
        }
        void MakeCap(CrossSection section)
        {
            int n = section.vertices.Count;

            for (int i = 0; i < n; i++)
            {
                section.vertices[i] = new Vector2(0, 0);
            }

            GenerateMesh();
        }
        void AttachSlider(string fieldName)
        {
            var slider = Fields[fieldName].uiControlEditor as UI_FloatRange;

            if (slider != null)
                slider.onFieldChanged += OnParameterChanged;
        }
        void OnParameterChanged(BaseField field, object obj)
        {
            GenerateMesh();
            if (field.name == nameof(l) && obj is float oldLength)
            {
                UpdateAttachmentNodes(l, oldLength);
            }
        }
        int GetMirroredIndex(int i, int count)
        {
            bool IsSelfMirror(int i, int count)
            {
                return i == GetMirroredIndex(i, count);
            }
            return (count / 2 - i + count) % count;
        }
        Vector3 MirrorVertex(Vector3 v)
        {
            return new Vector3(-v.x, v.y, v.z);
        }
        CrossSection GetActiveSection()
        {
                return (selectedRing< 0.5f) ? bottomSection : topSection;
        }
        void Update()
        {
            MeshFilter originalFilter = part.FindModelComponent<MeshFilter>();
            MeshRenderer originalRenderer = originalFilter.GetComponent<MeshRenderer>();
            if (!HighLogic.LoadedSceneIsEditor) return;

            if (isDragging) return;

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                isShiftDown = true;
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                isShiftDown = false;
            }

            CrossSection section = GetActiveSection();

            int ringIndex = (selectedRing < 0.5f) ? 0 : 1;

            int i = Mathf.Clamp(
                    Mathf.RoundToInt(selectedVertex),
                    0,
                    section.vertices.Count - 1
                );

            if (i != lastSelected || ringIndex != lastSelectedRing)
            {
                Vector3 v = (Vector3)section.vertices[i];

                vertexX = v.x;
                vertexY = v.y;
                vertexZ = v.z;

                lastSelected = i;
                lastSelectedRing = ringIndex;
            }

            newSlider = new Vector3(vertexX, vertexY, vertexZ);

            if (newSlider != lastSliderValue)
            {
                section.vertices[i] = newSlider;

                lastSliderValue = newSlider;

                if (isSymmetry)
                {
                    int j = GetMirroredIndex(i, section.vertices.Count);

                    section.vertices[j] = MirrorVertex(newSlider);
                }

                GenerateMesh();
                PropagateSymmetry();
            }

            Material mat = originalRenderer.sharedMaterial;
            meshRenderer.sharedMaterial = mat;

        }
        public void ApplySymmetry(VBProceduralParts source)
        {
            bottomVerticesData = source.bottomVerticesData;
            topVerticesData = source.topVerticesData;

            GenerateMesh();
        }
        public void PropagateSymmetry()
        {
            if (updatingFromSymmetry) return;
            foreach (Part counterpart in part.symmetryCounterparts)
            {
                VBProceduralParts module = counterpart.GetComponent<VBProceduralParts>();

                if (module != null)
                {
                    module.updatingFromSymmetry = true;
                    module.ApplySymmetry(this);
                    module.updatingFromSymmetry = false;
                }
            }
        }
        string SerializeVertices(List<Vector3> verts)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                sb.Append(v.x).Append(",").Append(v.y).Append(",").Append(v.z);

                if (i < verts.Count - 1)
                    sb.Append(";");
            }

            return sb.ToString();
        }

        List<Vector3> DeserializeVertices(string data)
        {
            List<Vector3> v = new List<Vector3>();

            if (string.IsNullOrEmpty(data))
                return v;

            string[] pairs = data.Split(';');

            foreach (string p in pairs)
            {
                string[] xyz = p.Split(',');

                float x = float.Parse(xyz[0]);
                float y = float.Parse(xyz[1]);
                float z = float.Parse(xyz[2]);

                v.Add(new Vector3(x, y, z));
            }

            return v;
        }
        void CreateObject()
        {
            MeshFilter originalFilter = part.FindModelComponent<MeshFilter>();
            MeshRenderer originalRenderer = originalFilter.GetComponent<MeshRenderer>();
            Mesh EmptyMesh = new Mesh();
            meshObject = part.transform.Find("ProceduralMesh")?.gameObject;
            if (meshObject == null)
            {
                meshObject = new GameObject("ProceduralMesh");
            }
            meshObject.transform.parent = part.transform;
            meshObject.transform.localPosition = Vector3.zero;
            meshObject.transform.localRotation = Quaternion.identity;

            meshFilter = meshObject.GetComponent<MeshFilter>();
            meshRenderer = meshObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null && meshFilter == null)
            {
                meshFilter = meshObject.AddComponent<MeshFilter>();
                meshRenderer = meshObject.AddComponent<MeshRenderer>();
            }

            originalRenderer.enabled = false;

            meshRenderer.enabled = true;

            Material mat = originalRenderer.sharedMaterial;
            mat.SetFloat("_Scale", 1f);
            meshRenderer.sharedMaterial = mat;
            

            meshObject.transform.localScale = Vector3.one;
        }
        void GenerateMesh()
        {
            bottomVerticesData = SerializeVertices(bottomSection.vertices);
            topVerticesData = SerializeVertices(topSection.vertices);

            ringCreator.VertexRingFormula(bottomSection, topSection, l);

            Lofter lofter = new Lofter();

            Mesh mesh = lofter.GenerateLoft(
                ringCreator.bottomRing,
                ringCreator.topRing
            );

            if (meshFilter.mesh != null)
            {
                Destroy(meshFilter.mesh);
            }

            meshFilter.mesh = mesh;

            MeshCollider collider = meshObject.GetComponent<MeshCollider>();

            if (collider == null)
            {
                collider = meshObject.AddComponent<MeshCollider>();
            }

            //if (isFlipppedNormals)
            //{
            //    Flip(meshFilter.sharedMesh);
            //}

            if (isFlatShading)
            {
                FlatShading(meshFilter.sharedMesh);
            }

            MapToCylinder();

            collider.sharedMesh = null;
            collider.sharedMesh = meshFilter.mesh;
            collider.convex = true;

            UpdateMass();
        }
        public static void Flip(Mesh mesh)
        {
            mesh.triangles = mesh.triangles.Reverse().ToArray();
            Vector3[] normals = mesh.normals;

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = -normals[i];
            }
            mesh.normals = normals;
        }
        void UpdateAttachmentNodes(float length, float oldLength)
        {
            AttachNode nodeStackTop = part.FindAttachNode("top");
            AttachNode nodeStackBottom = part.FindAttachNode("bottom");
            nodeStackTop.position = new Vector3(0f, l / 2f, 0f);
            nodeStackTop.orientation = Vector3.up;
            nodeStackBottom.position = new Vector3(0f, -l / 2f, 0f);
            nodeStackBottom.orientation = Vector3.down;

            part.UpdateAttachNodes();

            float trans = length - oldLength;
            HandleLengthChange(trans);
        }
        public void HandleLengthChange(float trans)
        {
            foreach (AttachNode node in part.attachNodes)
            {
                float direction = (node.position.y > 0) ? 1 : -1;

                Vector3 translation = direction * (trans / 2) * Vector3.up;
                if (node.nodeType == AttachNode.NodeType.Stack)
                {
                    if (node.attachedPart is Part pushTarget)
                    {
                        TranslatePart(pushTarget, translation);
                    }
                }
            }
        }
        public void TranslatePart(Part pushTarget, Vector3 translation)
        {
            if (pushTarget == this.part.parent)
            {
                this.part.transform.Translate(-translation, Space.Self);
                pushTarget = GetEldestParent(this.part);
            }

            Vector3 worldSpaceTranslation = part.transform.TransformVector(translation);
            pushTarget.transform.Translate(worldSpaceTranslation, Space.World);
        }
        void MapToCylinder()
        {
            Mesh mesh = meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];

            for (int i = 0;i < vertices.Length;i++)
            {
                Vector3 vertex = vertices[i];

                float angle = Mathf.Atan2(vertex.x, vertex.z) * Mathf.Rad2Deg;
                float u = (angle + 180f) / 360f;

                float v = (vertex.y + l);

                uvs[i] = new Vector2(u, v);
            }

            mesh.uv = uvs;
        }
        void FlatShading(Mesh mesh)
        {
            Vector3[] oldVertices = mesh.vertices;
            int[] tris = mesh.triangles;

            Vector3[] newVertices = new Vector3[tris.Length];
            int[] newTris = new int[tris.Length];

            for (int i = 0; i < tris.Length; i++)
            {
                newVertices[i] = oldVertices[tris[i]];
                newTris[i] = i;
            }

            mesh.vertices = newVertices;
            mesh.triangles = newTris;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        // Draw Window

        private Rect windowEditor = new Rect(200, 200, 800, 800);
        private bool isWindowOpen = false;
        private bool isDragging = false;
        private Vector2 boxSize = new Vector2(16, 16);
        private Vector2 offset;
        private int draggedVertex = -1;
        private float interval = 0.001f;
        public bool isShiftDown = true;
        void OnGUI()
        {
            if (isWindowOpen)
            {
                windowEditor = GUI.Window(0, windowEditor, DrawWindowContent, "Cross-Section Editor");
            }
        }

        void DrawWindowContent(int windowID)
        {
            CrossSection section = GetActiveSection();
            Vector2 center = new Vector2(400, 400);
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> flatVertices;
            Vector2 newVertexPosition;

            Event e = Event.current;


            Rect vertexRect = new Rect (350 + center.x, 350 + center.y, boxSize.x, boxSize.y);

            vertices = section.vertices;

            int ringIndex = (selectedRing < 0.5f) ? 0 : 1;

            flatVertices = vertices.Select(v3 => (Vector2)v3).ToList();

            if (isShiftDown)
            {
                interval = 0.05f;
            }
            else
            {
                interval = 0.001f;
            }

            for (int i = 0; i < flatVertices.Count; i++)
            {
                Vector2 pos = flatVertices[i];
                vertexRect = new Rect(pos.x * 350 + center.x, pos.y * 350 + center.y, boxSize.x, boxSize.y);;
                if (i == (int)selectedVertex)
                {
                    GUI.Box(vertexRect, "o");
                }
                else
                {
                    GUI.Box(vertexRect, ".");
                }

                if (e.type == EventType.MouseDown && vertexRect.Contains(e.mousePosition))
                {
                    if (i != (int)selectedVertex)
                    {
                        selectedVertex = i;
                    }
                    draggedVertex = i;
                    isDragging = true;

                    offset = e.mousePosition - new Vector2(vertexRect.x, vertexRect.y);

                    e.Use();
                }
            }
            if (isDragging && draggedVertex != -1 && e.type == EventType.MouseDrag)
            {
                vertexX = (float)Math.Round(((e.mousePosition.x - center.x - offset.x) / 350) / interval) * interval;
                vertexY = (float)Math.Round(((e.mousePosition.y - center.y - offset.y) / 350) / interval) * interval;
                newSlider = new Vector3(vertexX, vertexY, 0);
                section.vertices[(int)selectedVertex] = new Vector3(vertexX, vertexY, 0);

                if (isSymmetry)
                {
                    int j = GetMirroredIndex(draggedVertex, section.vertices.Count);

                    section.vertices[j] = MirrorVertex(newSlider);
                }

                GenerateMesh();
                e.Use();
            }
            else if (e.type == EventType.MouseUp && isDragging)
            {
                isDragging = false;
                Vector3 v = section.vertices[draggedVertex];

                vertexX = v.x;
                vertexY = v.y;
                vertexZ = v.z;

                lastSliderValue = v;

                draggedVertex = -1;
                e.Use();
            }
            if (GUI.Button(new Rect(10, 780, 490, 15), "Close Window"))
            {
                isWindowOpen = !isWindowOpen;
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}