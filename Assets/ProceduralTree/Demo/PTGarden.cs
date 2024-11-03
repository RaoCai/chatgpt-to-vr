using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralModeling
{

    public class PTGarden : MonoBehaviour
    {

        [SerializeField] Camera cam;
        [SerializeField] List<GameObject> prefabs;
        [SerializeField] Vector2 scaleRange = new Vector2(1f, 1.2f);

        const string SHADER_PATH = "Hidden/Internal-Colored";

        Material lineMaterial = null;
        MeshCollider col = null;
        Vector3[] vertices;
        int[] triangles;

        bool hit;
        Vector3 point;
        Vector3 normal;
        Quaternion rotation;

        void Update()
        {
        }

        const int resolution = 16;
        const float pi2 = Mathf.PI * 2f;
        const float radius = 0.5f;
        Color color = new Color(0.6f, 0.75f, 1f);

        void OnRenderObject()
        {
            if (!hit) return;

            CheckInit();

            lineMaterial.SetPass(0);
            lineMaterial.SetInt("_ZTest", (int)CompareFunction.Always);

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int i = 0; i < resolution; i++)
            {
                var cur = (float)i / resolution * pi2;
                var next = (float)(i + 1) / resolution * pi2;
                var p1 = rotation * new Vector3(Mathf.Cos(cur), Mathf.Sin(cur), 0f);
                var p2 = rotation * new Vector3(Mathf.Cos(next), Mathf.Sin(next), 0f);
                GL.Vertex(point + p1 * radius);
                GL.Vertex(point + p2 * radius);
            }

            GL.End();
            GL.PopMatrix();
        }

        void OnEnable()
        {
            col = GetComponent<MeshCollider>();
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            vertices = mesh.vertices;
            triangles = mesh.triangles;
        }

        void CheckInit()
        {
            if (lineMaterial == null)
            {
                Shader shader = Shader.Find(SHADER_PATH);
                if (shader == null) return;
                lineMaterial = new Material(shader);
                lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }

    }

}

