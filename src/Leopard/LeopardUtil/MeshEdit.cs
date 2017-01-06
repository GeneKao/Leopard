using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Plankton;
using PlanktonGh;

namespace Leopard
{
    public static class MeshEdit
    {

        public static Polyline getFacePolyline(PlanktonMesh source, int i)
        {
            int[] vertexFace = source.Faces.GetFaceVertices(i);
            Polyline facePoly = new Polyline();
            for (int j = 0; j <= vertexFace.Length; j++)
            {
                var v = source.Vertices[vertexFace[j % vertexFace.Length]];
                facePoly.Add(v.X, v.Y, v.Z);
            }
            return facePoly;
        }
    }
}
