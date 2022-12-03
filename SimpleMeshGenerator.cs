using System;
using UnityEngine;

namespace MeshGeneration.Utils
{
    public static class SimpleMeshGenerator
    {
        #region FUNCTIONS

        public static Mesh GetTriangle()
        {
            Mesh simpleTriangle = new Mesh(1);

            simpleTriangle.Vertices[0] = GetPositionInUnitCircle(90);
            simpleTriangle.Vertices[1] = GetPositionInUnitCircle(0);
            simpleTriangle.Vertices[2] = GetPositionInUnitCircle(180);

            // Set indices for triangle
            for (int i = 0; i < simpleTriangle.Vertices.Length; i++)
            {
                simpleTriangle.Triangles[i] = i;
            }
            
            simpleTriangle.RegenerateNormals();
            
            //Center triangle
            simpleTriangle.Translate(new Vector3(0,0, -0.5f));

            return simpleTriangle;
        }

        public static Mesh GetRightAngleTriangle()
        {
            Mesh rightAngleTriangle = new Mesh(1);
            
            rightAngleTriangle.Vertices[0] = GetPositionInUnitCircle(90);
            rightAngleTriangle.Vertices[1] = GetPositionInUnitCircle(0);
            rightAngleTriangle.Vertices[2] = new Vector3(0, 0, 0);

            // Set indices for triangle
            for (int i = 0; i < rightAngleTriangle.Vertices.Length; i++)
            {
                rightAngleTriangle.Triangles[i] = i;
            }
            
            rightAngleTriangle.RegenerateNormals();

            return rightAngleTriangle;
        }

        public static Mesh GetPlane(bool optimized = true)
        {
            Mesh firstTriangle = GetRightAngleTriangle();
            Mesh secondTriangle = GetRightAngleTriangle();
            secondTriangle.Mirror(Mesh.MirrorAxis.X);
            secondTriangle.Mirror(Mesh.MirrorAxis.Z);
            secondTriangle.Translate(new Vector3(1,0,1));

            Mesh plane = firstTriangle.Join(secondTriangle); 

            if (optimized == true)
            {
                plane = plane.GetOptimizedMesh();;
            }

            return plane;
        }
        
        public static Mesh GetGrid(int width, int height, float cellSize, bool optimized = true)
        {
            Mesh grid = new Mesh(0);
            
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Mesh plane = GetPlane();
                    plane.Scale(cellSize);
                    plane.Translate(new Vector3(j*cellSize, 0, i*cellSize));
                    grid = grid.Join(plane);
                }
            }

            if (optimized == true)
            {
                grid = grid.GetOptimizedMesh();
            }

            return grid;
        }

        public static Mesh GetCube(bool optimized = true)
        {
            Mesh up = GetPlane(false);
            up.Translate(new Vector3(0,1,0));
            
            Mesh down = GetPlane(false);
            down.Rotate(new Vector3(0, 0, 180));
            down.Translate(new Vector3(1,0,0));

            Mesh forward = GetPlane(false);
            forward.Rotate(new Vector3(-90,0,0));

            Mesh back = GetPlane(false);
            back.Rotate(new Vector3(90,0,0));
            back.Translate(Vector3.forward + Vector3.up);

            Mesh left = GetPlane(false);
            left.Rotate(new Vector3(0,0,90));
            
            Mesh right = GetPlane(false);
            right.Rotate(new Vector3(0,0,-90));
            right.Translate(Vector3.right + Vector3.up);

            Mesh cube = up.Join(down).Join(forward).Join(back).Join(left).Join(right);
            if (optimized == true)
            {
                cube = cube.GetOptimizedMesh();
            }

            return cube;
        }

        private static Vector3 GetPositionInUnitCircle(float angle)
        {
            Vector3 position = Vector3.zero;
            
            float angleInRadians = angle * Mathf.Deg2Rad;

            position.x = Mathf.Cos(angleInRadians);
            position.z = Mathf.Sin(angleInRadians);

            return position;
        }

        #endregion
    }
}