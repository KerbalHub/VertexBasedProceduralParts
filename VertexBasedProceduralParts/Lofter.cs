using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace VertexBasedProceduralParts
{
    public class Lofter
    {
        public Mesh GenerateLoft(List<Vector3> bottomRing, List<Vector3> topRing) // Inputs two rings, outputs a mesh
        {
            Mesh mesh = new Mesh();
            mesh.name = "Loft";

            // create array from top and bottom ring
            List<Vector3> vertices = new List<Vector3>();
            vertices.AddRange(bottomRing);
            vertices.AddRange(topRing);

            vertices.AddRange(bottomRing);
            vertices.AddRange(topRing);

            // create tris
            int n = bottomRing.Count;
            List<int> tris = new List<int>();


            for (int i = 0; i < n; i++)
            {
                // bottom ring indices
                int bl = i;
                int br = (i + 1) % n;

                // top ring indices
                int tl = i + n;
                int tr = (i + 1) % n + n;

                if (i % 2 == 0)
                {
                    tris.Add(bl);
                    tris.Add(br);
                    tris.Add(tl);

                    tris.Add(br);
                    tris.Add(tr);
                    tris.Add(tl);
                }
                else
                {
                    tris.Add(bl);
                    tris.Add(br);
                    tris.Add(tr);

                    tris.Add(bl);
                    tris.Add(tr);
                    tris.Add(tl);
                }
            }
            int offset = n * 2;
            for (int i = 0; i < n; i++)
            {
                // bottom ring indices
                int bl = i + offset;
                int br = (i + 1) % n + offset;

                // top ring indices
                int tl = i + n + offset;
                int tr = (i + 1) % n + n + offset;

                if (i % 2 == 0)
                {
                    tris.Add(bl);
                    tris.Add(tl);
                    tris.Add(br);

                    tris.Add(br);
                    tris.Add(tl);
                    tris.Add(tr);
                }
                else
                {
                    tris.Add(bl);
                    tris.Add(tr);
                    tris.Add(br);

                    tris.Add(bl);
                    tris.Add(tl);
                    tris.Add(tr);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(tris, 0);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
