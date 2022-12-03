using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeshGeneration.Utils
{
    public class Mesh
    {
        #region MEMBERS

        private const int VertexCountPerTriangle = 3;

        #endregion

        #region PROPERTIES

        public int[] Triangles
        {
            get;
            private set;
        }
        
        public Vector3[] Vertices
        {
            get;
            private set;
        }
        
        public Vector3[] Normals
        {
            get;
            private set;
        }
        
        public Vector2[] UVs
        {
            get;
            private set;
        }

        public bool IsOptimized
        {
            get;
            private set;
        }

        public int TriangleCount
        {
            get;
            private set;
        }

        #endregion

        #region FUNCTIONS

        public Mesh(int triangleCount)
        {
            if (triangleCount < 0)
            {
                triangleCount = 0;
            }
            
            int arraySize = triangleCount * VertexCountPerTriangle;
            
            Triangles = new int[arraySize];
            Vertices = new Vector3[arraySize];
            Normals = new Vector3[arraySize];
            UVs = new Vector2[arraySize];
            TriangleCount = triangleCount;

            // fill array with vertices index
            for (int i = 0; i < Triangles.Length; i++)
            {
                Triangles[i] = i;
            }
        }

        public Mesh(Mesh source)
        {
            Triangles = new int[source.Triangles.Length];
            Array.Copy(source.Vertices, Triangles, source.Triangles.Length);
            
            Vertices = new Vector3[source.Vertices.Length];
            Array.Copy(source.Vertices, Vertices, source.Vertices.Length);
            
            Normals = new Vector3[source.Normals.Length];
            Array.Copy(source.Normals, Normals, source.Normals.Length);
            
            UVs = new Vector2[source.UVs.Length];
            Array.Copy(source.UVs, UVs, source.UVs.Length);

        }

        public Mesh(int[] triangles, Vector3[] vertices, Vector3[] normals, Vector2[] uvs)
        {
            Triangles = triangles;
            Vertices = vertices;
            Normals = normals;
            UVs = uvs;
            TriangleCount = triangles.Length / VertexCountPerTriangle;
        }

        public void Translate(Vector3 offset)
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i] += offset;
            }
        }

        public void Rotate(Vector3 euler)
        {
            Quaternion eulerToQuaternion = Quaternion.Euler(euler);

            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i] = eulerToQuaternion * Vertices[i];
                Normals[i] = eulerToQuaternion * Normals[i];
            }
        }
        
        public void Scale(float scalar)
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i] *= scalar;
            }
        }

        // Mirror center is (0,0,0)
        public void Mirror(MirrorAxis axis)
        {
            switch (axis)
            {
                case MirrorAxis.X:
                    FlipX();
                    break;
                case MirrorAxis.Y:
                    FlipY();
                    break;
                case MirrorAxis.Z:
                    FlipZ();
                    break;
            }

            for (int i = 0; i < Triangles.Length / VertexCountPerTriangle; i++)
            {
                ReverseTriangle(i);
            }
        }

        public Mesh Join(Mesh target)
        {
            Mesh newMesh = new Mesh(this.TriangleCount + target.TriangleCount);

            CopyDataFromTwoMeshes(this, target, newMesh);

            return newMesh;
        }

        // based on: 
        // https://computergraphics.stackexchange.com/questions/4031/programmatically-generating-vertex-normals
        public void RegenerateNormals()
        {
            // Flat shading
            for (int i = 0; i < Triangles.Length; i += VertexCountPerTriangle)
            {
                Vector3 normal = Vector3.Cross(Vertices[Triangles[i + 1]] - Vertices[Triangles[i]],
                    Vertices[Triangles[i + 2]] - Vertices[Triangles[i]]).normalized;

                Normals[Triangles[i]] = normal;
                Normals[Triangles[i + 1]] = normal;
                Normals[Triangles[i + 2]] = normal;
            }
        }

        public Mesh GetOptimizedMesh()
        {
            
#if UNITY_EDITOR || DEBUG
            if (Triangles.Length == 0)
            {
                Debug.LogWarning("You are trying to optimize empty mesh. You will receive new empty one.");
            }
#endif
            
            Mesh optimizedMesh = Veld();
            optimizedMesh.IsOptimized = true;

            return optimizedMesh;
        }

        public void ValidateTriangles()
        {
            for (int i = 0; i < Triangles.Length / VertexCountPerTriangle; i++)
            {
                Vector3 faceNormal = GetFaceNormal(i * VertexCountPerTriangle);
                Vector3 currentFaceNormal = CalculateCurrentFaceNormal(i * VertexCountPerTriangle);

                if (Vector3.Dot(faceNormal, currentFaceNormal) == -1)
                {
                    ReverseTriangle(i);
                }
            }
        }
        
        private void CopyDataFromTwoMeshes(Mesh first, Mesh second, Mesh target)
        {
            // Copy triangles
            Array.Copy(first.Triangles, 0, target.Triangles, 0, first.Triangles.Length);
            CopyAndOffsetSecondTriangleArray(first, second, target);

            // Copy vertices
            Array.Copy(first.Vertices, 0, target.Vertices, 0, first.Vertices.Length);
            Array.Copy(second.Vertices, 0, target.Vertices, first.Vertices.Length, second.Vertices.Length);
            
            // Copy normals
            Array.Copy(first.Normals, 0, target.Normals, 0, first.Normals.Length);
            Array.Copy(second.Normals, 0, target.Normals, first.Normals.Length, second.Normals.Length);
            
            // Copy uvs
            Array.Copy(first.UVs, 0, target.UVs, 0, first.UVs.Length);
            Array.Copy(second.UVs, 0, target.UVs, first.UVs.Length, second.UVs.Length);
        }
        
        private void CopyAndOffsetSecondTriangleArray(Mesh first, Mesh second, Mesh target)
        {
            for (int i = first.Triangles.Length; i < target.Triangles.Length; i++)
            {
                target.Triangles[i] = second.Triangles[i - first.Triangles.Length] + first.Triangles.Length;
            }
        }

        private void FlipX()
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].x = -Vertices[i].x;
            }
        }

        private void FlipY()
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].y = -Vertices[i].y;
            }
        }
        
        private void FlipZ()
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].z = -Vertices[i].z;
            }
        }
        
        // Velds two meshes to new one
        private Mesh Veld(float proximity = 0.01f)
        {
            //https://answers.unity.com/questions/228841/dynamically-combine-verticies-that-share-the-same.html
            
            if (Triangles.Length == 0)
            {
                return new Mesh(0);
            }
            
            List<int> vertexIndex = FindAndRemoveDuplicatedVertices(proximity);

            List<Vector3> optimizedVertices = new List<Vector3>(Vertices.Length);
            List<Vector3> optimizedNormals = new List<Vector3>(Vertices.Length);
            List<Vector2> optimizedUVs = new List<Vector2>(Vertices.Length);
            
            // copy optimized vertices
            Dictionary<int, int> vertexIndexLookupTable = CopyAndReorderVertices(Vertices, vertexIndex, optimizedVertices);
            // copy optimized normals
            CopyByVertexIndex(Normals, vertexIndex, optimizedNormals);
            // copy optimized UVs
            CopyByVertexIndex(UVs,vertexIndex, optimizedUVs);
            
            UpdateTriangleIndices(vertexIndex, vertexIndexLookupTable);

            return new Mesh(vertexIndex.ToArray(), optimizedVertices.ToArray(), optimizedNormals.ToArray(),
                optimizedUVs.ToArray());
        }

        private List<int> FindAndRemoveDuplicatedVertices(float proximity)
        {
            List<int> vertexIndex = new List<int>(Triangles);

            // this can be multithreaded

            // replace indexes for duplicated vertices
            for (int i = 0; i < Vertices.Length; i++)
            {
                for (int j = 0; j < Vertices.Length; j++)
                {
                    if (i == j || vertexIndex[i] == vertexIndex[j])
                    {
                        continue;
                    }

                    float distanceToVertex = Vertices[vertexIndex[i]].GetDirectionTo(Vertices[vertexIndex[j]]).magnitude;

                    if (distanceToVertex > proximity)
                    {
                        continue;
                    }

                    distanceToVertex = UVs[vertexIndex[i]].GetDirectionTo(UVs[vertexIndex[j]]).magnitude;
                    if (distanceToVertex > proximity)
                    {
                        continue;
                    }

                    distanceToVertex = Normals[vertexIndex[i]].GetDirectionTo(Normals[vertexIndex[j]]).magnitude;
                    if (distanceToVertex > proximity)
                    {
                        continue;
                    }

                    vertexIndex[j] = vertexIndex[i];
                }
            }

            return vertexIndex;
        }

        private void UpdateTriangleIndices(List<int> vertexIndex, Dictionary<int, int> vertexIndexLookupTable)
        {
            for (int i = 0; i < vertexIndex.Count; i++)
            {
                vertexIndex[i] = vertexIndexLookupTable[vertexIndex[i]];
            }
        }

        private void ReverseTriangle(int triangleStartIndex)
        {
            int[] newOrder = {Triangles[triangleStartIndex + 2], Triangles[triangleStartIndex + 1], Triangles[triangleStartIndex]};

            Triangles[triangleStartIndex] = newOrder[0];
            Triangles[triangleStartIndex + 1] = newOrder[1];
            Triangles[triangleStartIndex + 2] = newOrder[2];
        }

        private Vector3 CalculateCurrentFaceNormal(int triangleStartIndex)
        {
            return Vector3.Cross(Vertices[triangleStartIndex + 1] - Vertices[triangleStartIndex], Vertices[triangleStartIndex + 2] - Vertices[triangleStartIndex]).normalized;
        }

        private Vector3 GetFaceNormal(int triangleStartIndex)
        {
            return Normals[triangleStartIndex];
        }
        
        // Copy only unique data from vertexIndex
        private void CopyByVertexIndex(IList source, List<int> vertexIndex, IList targetList, bool onlyUnique = true)
        {
            List<int> indices;
            
            if (onlyUnique == true)
            {
                indices = vertexIndex.Distinct().ToList();
            }
            else
            {
                indices = vertexIndex.ToList();
            }

            foreach (int index in indices)
            {
                targetList.Add(source[index]);
            }
        }
        
        // Returns mapped vertex index to new position
        private Dictionary<int, int> CopyAndReorderVertices(IList source, List<int> vertexIndex, IList targetList)
        {
            List<int> uniqueIndices = vertexIndex.Distinct().ToList();
            Dictionary<int, int> newVerticesIndices = new Dictionary<int, int>();

            for (var i = 0; i < uniqueIndices.Count; i++)
            {
                int index = uniqueIndices[i];
                targetList.Add(source[index]);
                newVerticesIndices.Add(index, i);
            }
            
            return newVerticesIndices;
        }

        #endregion

        #region CLASS_ENUM

        public enum MirrorAxis
        {
            X,
            Y,
            Z
        }

        #endregion
    }
}