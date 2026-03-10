using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static EdyCommonTools.RotationController;

namespace VertexBasedProceduralParts
{
    public class CrossSection
    {
        public List<Vector3> vertices = new List<Vector3>();

        public CrossSection(int n, float d)
        {
            vertices = new List<Vector3>(n);

            // 360/24 angle with the axis being one Vector3 unit in the positive Y direction
            Quaternion rotation = Quaternion.AngleAxis(360f / n, Vector3.up);

            Vector3 v = Vector3.right * d / 2f;

            for (int i = 0; i < n; i++)
            {
                vertices.Add( new Vector2(v.x, v.z));
                v = rotation * v;
            }
        }
    }
}
