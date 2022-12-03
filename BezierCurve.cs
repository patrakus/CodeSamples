using MeshGeneration.Utils;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

// Source of knowledge about bezier curves:
// https://www.youtube.com/watch?v=aVwxzDHniEw&t=1207s

namespace BezierCurve
{
    public sealed class BezierCurve
    {
        #region MEMBERS

        private Vector3[] sampledPoints;
        private Vector3[] normals;
        private Vector3[] tangents;
        private CumulativeDistanceLookupTable LUT;
        
        // cached old data
        private Vector3 cachedStartPoint; 
        private Vector3 cachedControlStartPoint;
        private Vector3 cachedControlEndPoint; 
        private Vector3 cachedEndPoint;

        #endregion

        #region PROPERTIES

        public Vector3[] SampledPoints
        {
            get => sampledPoints;
            private set => sampledPoints = value;
        }

        public Vector3[] Normals
        {
            get => normals;
            private set => normals = value;
        }
        
        // shows curve direction
        public Vector3[] Tangents
        {
            get => tangents;
            private set => tangents = value;
        }

        public float Length
        {
            get;
            private set;
        }

        // When setting new curve resolution, it will cause to rebuild entire curve.
        public int Resolution
        {
            get;
            private set;
        }

        public Vector3 CachedStartPoint
        {
            get => cachedStartPoint;
            set => cachedStartPoint = value;
        }

        public Vector3 CachedControlStartPoint
        {
            get => cachedControlStartPoint;
            set => cachedControlStartPoint = value;
        }

        public Vector3 CachedControlEndPoint
        {
            get => cachedControlEndPoint;
            set => cachedControlEndPoint = value;
        }

        public Vector3 CachedEndPoint
        {
            get => cachedEndPoint;
            set => cachedEndPoint = value;
        }

        #endregion

        #region FUNCTIONS

        public BezierCurve(Vector3 startPoint, Vector3 controlStartPoint, Vector3 controlEndPoint, Vector3 endPoint, int curveResolution)
        {
            CacheData(startPoint, controlStartPoint, controlEndPoint, endPoint);
            SetResolution(curveResolution);
            RebuildCurve();
        }

        public void SetResolution(int curveResolution)
        {
            if (curveResolution <= 0)
            {
                Resolution = 1;
            }
            
            Resolution = curveResolution;
        }

        public void RebuildCurve()
        {
            //TODO: zrobić wersję z brakiem alokacji nowych zasobów
            SampledPoints = GetFourPointCurve(CachedStartPoint, CachedControlStartPoint, CachedControlEndPoint, CachedEndPoint, Resolution);
            Length = GetCurveLenght(SampledPoints);
            Tangents = GetTangents(CachedStartPoint, CachedControlStartPoint, CachedControlEndPoint, CachedEndPoint, Resolution);
            // var acc = CalculateAcceleration(CachedStartPoint, CachedControlStartPoint, CachedControlEndPoint, CachedEndPoint, Resolution);
            Normals = GetNormals(Tangents);
            LUT = new CumulativeDistanceLookupTable(SampledPoints, Resolution);
        }

        public float GetT(float distance)
        {
            return LUT.GetT(distance);
        }
        
        private void CacheData(Vector3 startPoint, Vector3 controlStartPoint, Vector3 controlEndPoint, Vector3 endPoint)
        {
            cachedStartPoint = startPoint;
            cachedControlStartPoint = controlStartPoint;
            cachedControlEndPoint = controlEndPoint;
            cachedEndPoint = endPoint;
        }

        private Vector3[] GetNormals(Vector3[] tangentsInCurve)
        {
            Vector3[] curveNormals = new Vector3[tangentsInCurve.Length];

            for (int i = 0; i < tangentsInCurve.Length; i++)
            {
                // var normal = new OrientedPoint(SampledPoints[i], tangentsInCurve[i]).LocalToWorld(Vector3.right);
                var normal = sampledPoints[i] + Quaternion.LookRotation(tangentsInCurve[i]) * Vector3.right;

                normal = normal - SampledPoints[i];

                if (Vector3.Dot(normal, tangentsInCurve[i]) == 0)
                {
                    Debug.Log(1);
                }
                
                curveNormals[i] = normal.normalized;
            }

            return curveNormals;
        }

        private Vector3[] GetTangents(Vector3 A, Vector3 B, Vector3 C, Vector3 D, int curveResolution)
        {
            if (curveResolution < 0)
            {
                curveResolution = 0;
            }
            
            Vector3[] curveTangents = new Vector3[curveResolution + 1];

            for (int i = 0; i < curveTangents.Length; i++)
            {
                float t = i / (curveResolution+1.0f);
                // Derivative of Cubic bezier
                Vector3 velocity = A * (-3 * t * t + 6 * t - 3) +
                                   B * (9 * t * t - 12 * t + 3) +
                                   C * (-9 * t * t + 6 * t) +
                                   D * (3 * t * t);

                // var a = Vector3.Lerp(A, B, t);
                // var b = Vector3.Lerp(B, C, t);
                // var c = Vector3.Lerp(C, D, t);
                //
                // var d = Vector3.Lerp(a, b, t);
                // var e = Vector3.Lerp(b, c, t);

                // this derivative describes velocity of a point
                curveTangents[i] = velocity.normalized;
                // curveTangents[i] = (e - d).normalized;
            }

            return curveTangents;
        }

        private float GetCurveLenght(Vector3[] pointInCurve)
        {
            float totalLength = 0;

            for (int i = 0; i < pointInCurve.Length - 1; i++)
            {
                totalLength += pointInCurve[i].GetDirectionTo(pointInCurve[i + 1]).magnitude;
            }

            return totalLength;
        }

        // Quadratic bezier
        // Returns sampled points defined in curveResolution
        private Vector3[] GetThreePointCurve(Vector3 startPoint, Vector3 controlPoint, Vector3 endPoint, int curveResolution)
        {
            if (curveResolution < 0)
            {
                curveResolution = 0;
            }
            
            Vector3[] sampledVectorPoints = new Vector3[curveResolution + 1];
            float curvePointDivider = sampledVectorPoints.Length - 1;

            for (int i = 0; i < sampledVectorPoints.Length; i++)
            {
                Vector3 interpolatedStartAndControlPoint = Vector3.Lerp(startPoint, controlPoint, i / curvePointDivider);
                Vector3 interpolatedControlAndEndPoint = Vector3.Lerp(controlPoint, endPoint, i / curvePointDivider);
                sampledVectorPoints[i] = Vector3.Lerp(interpolatedStartAndControlPoint, interpolatedControlAndEndPoint, i / curvePointDivider);
            }

            return sampledVectorPoints;
        }

        // Cubic bezier
        // Returns sampled points defined in curveResolution
        private Vector3[] GetFourPointCurve(Vector3 startPoint, Vector3 startControlPoint,
            Vector3 endControlPoint, Vector3 endPoint, int curveResolution)
        {
            if (curveResolution < 0)
            {
                curveResolution = 0;
            }
            
            Vector3[] sampledVectorPoints = new Vector3[curveResolution + 1];
            float curvePointDivider = sampledVectorPoints.Length - 1;

            for (int i = 0; i < sampledVectorPoints.Length; i++)
            {
                // first pass
                Vector3 interpolatedStartAndStartControlPoint = Vector3.Lerp(startPoint, startControlPoint, i / curvePointDivider);
                Vector3 interpolatedStartControlPointAndEndControlPoint = Vector3.Lerp(startControlPoint, endControlPoint, i / curvePointDivider);
                Vector3 interpolatedEndControlPointAndEndPoint = Vector3.Lerp(endControlPoint, endPoint, i / curvePointDivider);
                
                // second pass
                Vector3 dPoint = Vector3.Lerp(interpolatedStartAndStartControlPoint, interpolatedStartControlPointAndEndControlPoint, i / curvePointDivider);
                Vector3 ePoint = Vector3.Lerp(interpolatedStartControlPointAndEndControlPoint, interpolatedEndControlPointAndEndPoint, i / curvePointDivider);
                
                // Final sampled point in curve
                sampledVectorPoints[i] = Vector3.Lerp(dPoint, ePoint, i / curvePointDivider);
            }

            return sampledVectorPoints;
        }

        #endregion

        #region CLASS_ENUM

        private struct OrientedPoint
        {
            private Quaternion rot;
            private Vector3 pos;

            public OrientedPoint(Vector3 pos, Quaternion rot)
            {
                this.pos = pos;
                this.rot = rot;
            }
            
            public OrientedPoint(Vector3 pos, Vector3 forward)
            {
                this.pos = pos;
                this.rot = Quaternion.LookRotation(forward);
            }

            public Vector3 LocalToWorld(Vector3 localSpacePos)
            {
                return pos + rot * localSpacePos;
            }
        }
        
        // Lookup table for getting uniform animation
        private class CumulativeDistanceLookupTable
        {
            #region MEMBERS

            private float[] sampledDistances;
            private float[] sampledTValues;
            private float arcLength;

            #endregion

            #region FUNCTIONS

            public CumulativeDistanceLookupTable(Vector3[] sampledPoints, float bezierLength)
            {
                GenerateLookupTable(sampledPoints, bezierLength);
            }

            public void Regenerate(Vector3[] sampledPoints, float bezierLength)
            {
                GenerateLookupTable(sampledPoints, bezierLength);
            }

            public float GetT(float distance)
            {
                if (distance.IsBetween(0, arcLength) == false)
                {
                    return distance / arcLength; // fi distance is outside the length of the curve, then extrapolate values outside
                }

                float finalT = 0;
                for (int i = 0; i < sampledDistances.Length - 1; i++)
                {
                    if (distance.IsBetween(sampledDistances[i], sampledDistances[i + 1]) == false)
                    {
                        continue;
                    }

                    float tBetweenSampledDistances = distance.Remap(sampledDistances[i], sampledDistances[i + 1], 0, 1);
                    finalT = Mathf.Lerp(sampledTValues[i], sampledTValues[i + 1], tBetweenSampledDistances);
                }
                
                return finalT;
            }

            private void GenerateLookupTable(Vector3[] sampledPoints, float bezierLength)
            {
                sampledDistances = new float[sampledPoints.Length];
                sampledTValues = new float[sampledPoints.Length];
                arcLength = bezierLength;

                // fill obvious values
                sampledDistances[0] = 0;
                sampledTValues[0] = 0;
                sampledDistances[sampledPoints.Length -1] = bezierLength;
                sampledTValues[sampledPoints.Length -1] = 1;

                float sampledDist = 0;

                for (int i = 1; i < sampledPoints.Length-1; i++)
                {
                    sampledDist += sampledPoints[i].GetDirectionTo(sampledPoints[i - 1]).magnitude;

                    sampledDistances[i] = sampledDist;
                    sampledTValues[i] = sampledDist.Remap(0, bezierLength, 0, 1);
                }
            }

            #endregion
        }

        #endregion
    }
}