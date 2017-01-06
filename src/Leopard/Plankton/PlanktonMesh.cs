using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plankton
{
    /// <summary>
    /// Description of PlanktonMesh.
    /// </summary>
    public class PlanktonMesh
    {
        private PlanktonVertexList _vertices;
        private PlanktonHalfEdgeList _halfedges;
        private PlanktonFaceList _faces;

        #region "constructors"
        public PlanktonMesh() //blank constructor
        {
        }

        public PlanktonMesh(PlanktonMesh source)
        {
            foreach (var v in source.Vertices)
            {
                this.Vertices.Add(new PlanktonVertex() {
                    OutgoingHalfedge = v.OutgoingHalfedge,
                    X = v.X,
                    Y = v.Y,
                    Z = v.Z
                });
            }
            foreach (var f in source.Faces)
            {
                this.Faces.Add(new PlanktonFace() { FirstHalfedge = f.FirstHalfedge });
            }
            foreach (var h in source.Halfedges)
            {
                this.Halfedges.Add(new PlanktonHalfedge() {
                    StartVertex = h.StartVertex,
                    AdjacentFace = h.AdjacentFace,
                    NextHalfedge = h.NextHalfedge,
                    PrevHalfedge = h.PrevHalfedge,
                });
            }
        }
        #endregion

        #region "properties"
        /// <summary>
        /// Gets access to the vertices collection in this mesh.
        /// </summary>
        public PlanktonVertexList Vertices
        {
            get { return _vertices ?? (_vertices = new PlanktonVertexList(this)); }
        }

        /// <summary>
        /// Gets access to the halfedges collection in this mesh.
        /// </summary>
        public PlanktonHalfEdgeList Halfedges
        {
            get { return _halfedges ?? (_halfedges = new PlanktonHalfEdgeList(this)); }
        }

        /// <summary>
        /// Gets access to the faces collection in this mesh.
        /// </summary>
        public PlanktonFaceList Faces
        {
            get { return _faces ?? (_faces = new PlanktonFaceList(this)); }
        }
        #endregion

        #region "general methods"

        /// <summary>
        /// Calculate the volume of the mesh
        /// </summary>
        public double Volume()
        {
            double VolumeSum = 0;
            for (int i = 0; i < this.Faces.Count; i++)
            {
                int[] FaceVerts = this.Faces.GetFaceVertices(i);
                int EdgeCount = FaceVerts.Length;
                if (EdgeCount == 3)
                {
                    PlanktonXYZ P = this.Vertices[FaceVerts[0]].ToXYZ();
                    PlanktonXYZ Q = this.Vertices[FaceVerts[1]].ToXYZ();
                    PlanktonXYZ R = this.Vertices[FaceVerts[2]].ToXYZ();
                    //get the signed volume of the tetrahedron formed by the triangle and the origin
                    VolumeSum += (1 / 6d) * (
                           P.X * Q.Y * R.Z +
                           P.Y * Q.Z * R.X +
                           P.Z * Q.X * R.Y -
                           P.X * Q.Z * R.Y -
                           P.Y * Q.X * R.Z -
                           P.Z * Q.Y * R.X);
                }
                else
                {
                    PlanktonXYZ P = this._faces.GetFaceCenter(i);
                    for (int j = 0; j < EdgeCount; j++)
                    {
                        PlanktonXYZ Q = this.Vertices[FaceVerts[j]].ToXYZ();
                        PlanktonXYZ R = this.Vertices[FaceVerts[(j + 1) % EdgeCount]].ToXYZ();
                        VolumeSum += (1 / 6d) * (
                            P.X * Q.Y * R.Z +
                            P.Y * Q.Z * R.X +
                            P.Z * Q.X * R.Y -
                            P.X * Q.Z * R.Y -
                            P.Y * Q.X * R.Z -
                            P.Z * Q.Y * R.X);
                    }
                }
            }
            return VolumeSum;
        }


        public PlanktonMesh CatmullClark(int iteration)
        {
            return CatmullClark(iteration, new List<int>(), new List<int>(), new List<int>());
        }

        public PlanktonMesh CatmullClark(int iteration, List<int> fixVertices, List<int> fixEdges, List<int> fixFaces)
        {
            // Gene added catmull clark subdivision function, function written for Leopard.
            // maybe change this function to static, so can be easier.   

            PlanktonMesh cMesh = new PlanktonMesh(this); //  this iteration 

            List<bool> isBoundaryVertices = new List<bool>();

            // assign boundary vertices
            for (int i = 0; i < cMesh.Vertices.Count; i++)
            {
                if (cMesh.Vertices.IsBoundary(i))
                    isBoundaryVertices.Add(true);
                else
                    isBoundaryVertices.Add(false);
            }

            // set vertices
            foreach (int v in fixVertices)
                isBoundaryVertices[v] = true;

            // set edges
            foreach (int e in fixEdges)
            {
                int id = e * 2;
                //int[] pEndsIndices = cMesh.Halfedges.GetVertices(edge);

                int pairIndex = cMesh.Halfedges.GetPairHalfedge(id);
                if (pairIndex < id) { int temp = id; id = pairIndex; pairIndex = temp; }
                int[] vts = cMesh.Halfedges.GetVertices(id);

                isBoundaryVertices[vts[0]] = true;
                isBoundaryVertices[vts[1]] = true;
            }

            // set face 
            foreach (int f in fixFaces)
                foreach (int v in cMesh.Faces.GetFaceVertices(f))
                    isBoundaryVertices[v] = true;
           

            for (int iter = 0; iter < iteration; iter++)
            {
                PlanktonMesh subdMesh = new PlanktonMesh(); // current mesh for each iteration

                // add the original vertices 
                for (int i = 0; i < cMesh.Vertices.Count; i++)
                    subdMesh.Vertices.Add(cMesh.Vertices[i].ToXYZ());

                // face centers
                #region face centers
                PlanktonXYZ[] faceCenter = new PlanktonXYZ[cMesh.Faces.Count];
                for (int i = 0; i < cMesh.Faces.Count; i++)
                {
                    faceCenter[i] = cMesh.Faces.GetFaceCenter(i);
                    isBoundaryVertices.Add(false);
                    subdMesh.Vertices.Add(cMesh.Faces.GetFaceCenter(i));
                }
                #endregion

                // new edge points
                #region new edge points
                PlanktonXYZ[] newEdgePoints = new PlanktonXYZ[cMesh.Halfedges.Count / 2]; // pair half edges belong to one. 
                for (int i = 0; i < cMesh.Halfedges.Count; i++)
                {
                    if (i % 2 == 1) continue;

                    int pairIndex = cMesh.Halfedges.GetPairHalfedge(i);

                    int[] pEndsIndices = cMesh.Halfedges.GetVertices(i);
                    int fA = cMesh.Halfedges[i].AdjacentFace;
                    int fB = cMesh.Halfedges[pairIndex].AdjacentFace;

                    PlanktonXYZ pStart, pEnd, fAC, fBC;

                    // not boudary condition 
                    if (pEndsIndices[0] >= 0 && pEndsIndices[1] >= 0 &&
                        fA != -1 && fB != -1)
                    {
                        pStart = cMesh.Vertices[pEndsIndices[0]].ToXYZ();
                        pEnd = cMesh.Vertices[pEndsIndices[1]].ToXYZ();

                        fAC = faceCenter[fA];
                        fBC = faceCenter[fB];

                        if (isBoundaryVertices[pEndsIndices[0]] && isBoundaryVertices[pEndsIndices[1]])
                        {
                            newEdgePoints[(int)i / 2] = (cMesh.Vertices[pEndsIndices[0]].ToXYZ() + cMesh.Vertices[pEndsIndices[1]].ToXYZ()) * 0.5f;
                            isBoundaryVertices.Add(true);
                            subdMesh.Vertices.Add(newEdgePoints[(int)i / 2]);
                        }
                        else if ((!isBoundaryVertices[pEndsIndices[0]] && isBoundaryVertices[pEndsIndices[1]]) ||
                            (isBoundaryVertices[pEndsIndices[0]] && !isBoundaryVertices[pEndsIndices[1]]))
                        {
                            newEdgePoints[(int)i / 2] =
                                (cMesh.Vertices[pEndsIndices[0]].ToXYZ() + cMesh.Vertices[pEndsIndices[1]].ToXYZ()) * 0.5f * 0.5f +
                                (pStart + pEnd + fAC + fBC) * 0.25f * 0.5f;
                            isBoundaryVertices.Add(false);
                            subdMesh.Vertices.Add(newEdgePoints[(int)i / 2]);
                        }
                        else
                        {
                            newEdgePoints[(int)i / 2] = (pStart + pEnd + fAC + fBC) * 0.25f;
                            isBoundaryVertices.Add(false);
                            subdMesh.Vertices.Add(newEdgePoints[(int)i / 2]);
                        }
                    }
                    // boudary condition
                    else
                    {
                        newEdgePoints[(int)i / 2] = (cMesh.Vertices[pEndsIndices[0]].ToXYZ() + cMesh.Vertices[pEndsIndices[1]].ToXYZ()) * 0.5f;
                        isBoundaryVertices.Add(true);
                        subdMesh.Vertices.Add(newEdgePoints[(int)i / 2]);
                    }

                }
                #endregion

                // new vertex points
                #region new vertex points
                PlanktonXYZ[] newVertexPoints = new PlanktonXYZ[cMesh.Vertices.Count];
                for (int i = 0; i < cMesh.Vertices.Count; i++)
                {
                    int n = cMesh.Vertices.GetValence(i);

                    // F 
                    int[] fIndices = cMesh.Vertices.GetVertexFaces(i);
                    PlanktonXYZ fn = new PlanktonXYZ();
                    foreach (int f in fIndices)
                        if (f != -1)
                            fn += faceCenter[f];
                    fn *= (float)(1.0f / (n * n));

                    // E
                    int[] vnIndices = cMesh.Vertices.GetVertexNeighbours(i);
                    PlanktonXYZ vn = new PlanktonXYZ();
                    foreach (int e in vnIndices)
                        if (e != -1)
                            vn += cMesh.Vertices[e].ToXYZ();
                    vn *= (float)(1.0f / (n * n));

                    // V
                    PlanktonXYZ v = cMesh.Vertices[i].ToXYZ() * (n - 2) * (1.0f / n);
                    if (!isBoundaryVertices[i])
                    {
                        newVertexPoints[i] = fn + vn + v;
                        subdMesh.Vertices.SetVertex(i, newVertexPoints[i].X, newVertexPoints[i].Y, newVertexPoints[i].Z);
                    }
                    else
                    {
                        newVertexPoints[i] = cMesh.Vertices[i].ToXYZ();
                        subdMesh.Vertices.SetVertex(i, newVertexPoints[i].X, newVertexPoints[i].Y, newVertexPoints[i].Z);
                    }
                }
                #endregion

                // add mesh face 
                #region construct mesh
                for (int i = 0; i < cMesh.Faces.Count; i++)
                {
                    int pMVC = cMesh.Vertices.Count;
                    int fNewID = i + pMVC;

                    int[] fVertices = cMesh.Faces.GetFaceVertices(i);
                    int[] fEdgePts = cMesh.Faces.GetHalfedges(i);

                    int fNum = cMesh.Faces.Count;
                    int eNum = cMesh.Halfedges.Count / 2;

                    if (fVertices.Length == 3)
                    {
                        subdMesh.Faces.AddFace(fNewID, fNum + fEdgePts[0] / 2 + pMVC, fVertices[1], fNum + fEdgePts[1] / 2 + pMVC);
                        subdMesh.Faces.AddFace(fNewID, fNum + fEdgePts[1] / 2 + pMVC, fVertices[2], fNum + fEdgePts[2] / 2 + pMVC);
                        subdMesh.Faces.AddFace(fNewID, fNum + fEdgePts[2] / 2 + pMVC, fVertices[0], fNum + fEdgePts[0] / 2 + pMVC);
                    }
                    else
                    {
                        subdMesh.Faces.AddFace(fNewID, fNum + fEdgePts[0] / 2 + pMVC, fVertices[1], fNum + fEdgePts[1] / 2 + pMVC);
                        subdMesh.Faces.AddFace(fNewID, fNum + fEdgePts[1] / 2 + pMVC, fVertices[2], fNum + fEdgePts[2] / 2 + pMVC);
                        subdMesh.Faces.AddFace(fNewID, fNum + fEdgePts[2] / 2 + pMVC, fVertices[3], fNum + fEdgePts[3] / 2 + pMVC);
                        subdMesh.Faces.AddFace(fNewID, fNum + fEdgePts[3] / 2 + pMVC, fVertices[0], fNum + fEdgePts[0] / 2 + pMVC);
                    }
                }
                #endregion

                cMesh = subdMesh;

            }

            return cMesh;
        }

        public PlanktonMesh Dual(int option)  // Gene hacked, 0: barycenter, 1: circumcenter 
        {

            // hack for open meshes
            // TODO: improve this ugly method
            if (this.IsClosed() == false)
            {
                var dual = new PlanktonMesh();

                // create vertices from face centers
                for (int i = 0; i < this.Faces.Count; i++)
                {
                    // Gene added options
                    if (option == 0)
                        dual.Vertices.Add(this.Faces.GetFaceCenter(i));
                    else if (option == 1)
                        dual.Vertices.Add(this.Faces.GetFaceCircumCenter(i));
                }

                // create faces from the adjacent face indices of non-boundary vertices
                for (int i = 0; i < this.Vertices.Count; i++)
                {
                    if (this.Vertices.IsBoundary(i))
                    {
                        continue;
                    }
                    dual.Faces.AddFace(this.Vertices.GetVertexFaces(i));
                }

                return dual;
            }

            // can later add options for other ways of defining face centres (barycenter/circumcenter etc)
            // won't work yet with naked boundaries

            PlanktonMesh P = this;
            PlanktonMesh D = new PlanktonMesh();

            //for every primal face, add the barycenter to the dual's vertex list
            //dual vertex outgoing HE is primal face's start HE
            //for every vertex of the primal, add a face to the dual
            //dual face's startHE is primal vertex's outgoing's pair

            for (int i = 0; i < P.Faces.Count; i++)
            {
                // Gene added circumcenter option
                PlanktonXYZ fc = new PlanktonXYZ(); // face center
                if (option == 0)
                    fc = P.Faces.GetFaceCenter(i);
                else if (option == 1)
                    fc = P.Faces.GetFaceCircumCenter(i);

                D.Vertices.Add(new PlanktonVertex(fc.X, fc.Y, fc.Z));

                //
                //D.Vertices.Add(new PlanktonVertex(fc.X, fc.Y, fc.Z)); // gene deleted
                int[] FaceHalfedges = P.Faces.GetHalfedges(i);
                for (int j = 0; j < FaceHalfedges.Length; j++)
                {
                    if (P.Halfedges[P.Halfedges.GetPairHalfedge(FaceHalfedges[j])].AdjacentFace != -1)
                    {
                        // D.Vertices[i].OutgoingHalfedge = FaceHalfedges[j];
                        D.Vertices[D.Vertices.Count - 1].OutgoingHalfedge = P.Halfedges.GetPairHalfedge(FaceHalfedges[j]);
                        break;
                    }
                }
            }

            for (int i = 0; i < P.Vertices.Count; i++)
            {
                if (P.Vertices.NakedEdgeCount(i) == 0)
                {
                    int df = D.Faces.Add(PlanktonFace.Unset);
                    // D.Faces[i].FirstHalfedge = P.PairHalfedge(P.Vertices[i].OutgoingHalfedge);
                    D.Faces[df].FirstHalfedge = P.Vertices[i].OutgoingHalfedge;
                }
            }

            // dual halfedge start V is primal AdjacentFace
            // dual halfedge AdjacentFace is primal end V
            // dual nextHE is primal's pair's prev
            // dual prevHE is primal's next's pair

            // halfedge pairs stay the same

            for (int i = 0; i < P.Halfedges.Count; i++)
            {
                if ((P.Halfedges[i].AdjacentFace != -1) & (P.Halfedges[P.Halfedges.GetPairHalfedge(i)].AdjacentFace != -1))
                {
                    PlanktonHalfedge DualHE = PlanktonHalfedge.Unset;
                    PlanktonHalfedge PrimalHE = P.Halfedges[i];
                    //DualHE.StartVertex = PrimalHE.AdjacentFace;
                    DualHE.StartVertex = P.Halfedges[P.Halfedges.GetPairHalfedge(i)].AdjacentFace;

                    if (P.Vertices.NakedEdgeCount(PrimalHE.StartVertex) == 0)
                    {
                        //DualHE.AdjacentFace = P.Halfedges[P.PairHalfedge(i)].StartVertex;
                        DualHE.AdjacentFace = PrimalHE.StartVertex;
                    }
                    else { DualHE.AdjacentFace = -1; }

                    //This will currently fail with open meshes...
                    //one option could be to build the dual with all halfedges, but mark some as dead
                    //if they connect to vertex -1
                    //mark the 'external' faces all as -1 (the ones that are dual to boundary verts)
                    //then go through and if any next or prevs are dead hes then replace them with the next one around
                    //this needs to be done repeatedly until no further change

                    //DualHE.NextHalfedge = P.Halfedges[P.PairHalfedge(i)].PrevHalfedge;
                    DualHE.NextHalfedge = P.Halfedges.GetPairHalfedge(PrimalHE.PrevHalfedge);

                    //DualHE.PrevHalfedge = P.PairHalfedge(PrimalHE.NextHalfedge);
                    DualHE.PrevHalfedge = P.Halfedges[P.Halfedges.GetPairHalfedge(i)].NextHalfedge;

                    D.Halfedges.Add(DualHE);
                }
            }
            return D;
        }

        public bool IsClosed()
        {
            for (int i = 0; i < this.Halfedges.Count; i++)
            {
                if (this.Halfedges[i].AdjacentFace < 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Truncates the vertices of a mesh.
        /// </summary>
        /// <param name="t">Optional parameter for the normalised distance along each edge to control the amount of truncation.</param>
        /// <returns>A new mesh, the result of the truncation.</returns>
        public PlanktonMesh TruncateVertices(float t = 1f / 3)
        {
            // TODO: handle special cases (t = 0.0, t = 0.5, t > 0.5)
            var tMesh = new PlanktonMesh(this);

            var vxyz = tMesh.Vertices.Select(v => v.ToXYZ()).ToArray();
            PlanktonXYZ v0, v1, v2;
            int[] oh;
            for (int i = 0; i < this.Vertices.Count; i++)
            {
                oh = this.Vertices.GetHalfedges(i);
                tMesh.Vertices.TruncateVertex(i);
                foreach (var h in oh)
                {
                    v0 = vxyz[this.Halfedges[h].StartVertex];
                    v1 = vxyz[this.Halfedges.EndVertex(h)];
                    v2 = v0 + (v1 - v0) * t;
                    tMesh.Vertices.SetVertex(tMesh.Halfedges[h].StartVertex, v2.X, v2.Y, v2.Z);
                }
            }

            return tMesh;
        }

        /* Hide for the time being to avoid confusion...
        public void RefreshVertexNormals()
        {
        }
        public void RefreshFaceNormals()
        {
        }
        public void RefreshEdgeNormals()
        {
        }
        */

        /// <summary>
        /// Removes any unreferenced objects from arrays, reindexes as needed and shrinks arrays to minimum required size.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if halfedge count is odd after compaction.
        /// Most likely caused by only marking one of the halfedges in a pair for deletion.</exception>
        public void Compact()
        {
            // Compact vertices, faces and halfedges
            this.Vertices.CompactHelper();
            this.Faces.CompactHelper();
            this.Halfedges.CompactHelper();
        }

        //dihedral angle for an edge
        //

        //skeletonize - build a new mesh with 4 faces for each original edge

        #endregion
    
    }
}
