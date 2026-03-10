using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace VertexBasedProceduralParts
{
    public class RingCreator
    {
        // variables
        public int n = 24;

        // ring vectors
        public List<Vector3> bottomRing;
        public List<Vector3> topRing;

        public void VertexRingFormula(CrossSection bottom, CrossSection top, float l)
        {
            bottomRing = new List<Vector3>(n);
            topRing = new List<Vector3>(n);

            // bottom ring
            foreach (Vector3 v in bottom.vertices)
            {
                Vector3 p = new Vector3(v.x, v.z, v.y);

                bottomRing.Add(p + new Vector3(0, -l / 2f, 0));
            }

            // top ring
            foreach (Vector3 v in top.vertices)
            {
                Vector3 p = new Vector3(v.x, v.z, v.y);

                topRing.Add(p + new Vector3(0, l / 2f, 0));
            }
        }
    }
}
