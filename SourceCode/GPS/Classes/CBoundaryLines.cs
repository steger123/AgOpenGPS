﻿using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace AgOpenGPS
{
    public class CBoundaryLines
    {
        //constructor
        public CBoundaryLines()
        {
            area = 0;
            isDriveAround = false;
            isDriveThru = false;
            isOwnField = false;
            OuterField = -1;
        }

        //list of coordinates of boundary line
        public List<vec3> bndLine = new List<vec3>();

        //the list of constants and multiples of the boundary
        public List<vec2> calcList = new List<vec2>();

        public double Northingmin, Northingmax, Eastingmin, Eastingmax;

        //area variable
        public double area;

        //boundary variables
        public int OuterField;
        public bool isOwnField, isDriveAround, isDriveThru;

        public void CalculateBoundaryHeadings()
        {
            //to calc heading based on next and previous points to give an average heading.
            int cnt = bndLine.Count;
            vec3[] arr = new vec3[cnt];
            cnt--;
            bndLine.CopyTo(arr);
            bndLine.Clear();

            //first point needs last, first, second points
            vec3 pt3 = arr[0];
            pt3.heading = Math.Atan2(arr[1].easting - arr[cnt].easting, arr[1].northing - arr[cnt].northing);
            if (pt3.heading < 0) pt3.heading += glm.twoPI;
            bndLine.Add(pt3);

            //middle points
            for (int i = 1; i < cnt; i++)
            {
                pt3 = arr[i];
                pt3.heading = Math.Atan2(arr[i + 1].easting - arr[i - 1].easting, arr[i + 1].northing - arr[i - 1].northing);
                if (pt3.heading < 0) pt3.heading += glm.twoPI;
                bndLine.Add(pt3);
            }

            //last and first point
            pt3 = arr[cnt];
            pt3.heading = Math.Atan2(arr[0].easting - arr[cnt - 1].easting, arr[0].northing - arr[cnt - 1].northing);
            if (pt3.heading < 0) pt3.heading += glm.twoPI;
            bndLine.Add(pt3);
        }

        public void FixBoundaryLine(int bndNum, double spacing)
        {
            //boundary point spacing based on eq width
            spacing *= 0.25;

            if (spacing < 1) spacing = 1;
            if (spacing > 3) spacing = 3;

            //first find out which side is inside the boundary
            vec3 point = new vec3(bndLine[2].easting - (Math.Sin(glm.PIBy2 + bndLine[2].heading) * 2.0),
            bndLine[2].northing - (Math.Cos(glm.PIBy2 + bndLine[2].heading) * 2.0), 0.0);

            //make sure boundaries are wound correctly
            if (IsPointInsideBoundary(point)) ReverseWinding();

            //make sure distance isn't too small between points on headland
            int bndCount = bndLine.Count;
            double distance;
            for (int i = 0; i < bndCount - 1; i++)
            {
                distance = glm.Distance(bndLine[i], bndLine[i + 1]);
                if (distance < spacing)
                {
                    bndLine.RemoveAt(i + 1);
                    bndCount = bndLine.Count;
                    i--;
                }
            }

            //make sure distance isn't too big between points on boundary
            bndCount = bndLine.Count;
            spacing *= 1.33;

            for (int i = 0; i < bndCount; i++)
            {
                int j = i + 1;

                if (j == bndCount) j = 0;
                distance = glm.Distance(bndLine[i], bndLine[j]);
                if (distance > spacing)
                {
                    vec3 pointB = new vec3((bndLine[i].easting + bndLine[j].easting) / 2.0,
                        (bndLine[i].northing + bndLine[j].northing) / 2.0, bndLine[i].heading);

                    bndLine.Insert(j, pointB);
                    bndCount = bndLine.Count;
                    i--;
                }
            }

            //make sure distance isn't too big between points on boundary
            bndCount = bndLine.Count;
            spacing *= 1.33;

            for (int i = 0; i < bndCount; i++)
            {
                int j = i + 1;

                if (j == bndCount) j = 0;
                distance = glm.Distance(bndLine[i], bndLine[j]);
                if (distance > spacing)
                {
                    vec3 pointB = new vec3((bndLine[i].easting + bndLine[j].easting) / 2.0,
                        (bndLine[i].northing + bndLine[j].northing) / 2.0, bndLine[i].heading);

                    bndLine.Insert(j, pointB);
                    bndCount = bndLine.Count;
                    i--;
                }
            }

            //make sure headings are correct for calculated points
            CalculateBoundaryHeadings();
        }

        private void ReverseWinding()
        {
            //reverse the boundary
            int cnt = bndLine.Count;
            vec3[] arr = new vec3[cnt];
            cnt--;
            bndLine.CopyTo(arr);
            bndLine.Clear();
            for (int i = cnt; i >= 0; i--)
            {
                arr[i].heading -= Math.PI;
                if (arr[i].heading < 0) arr[i].heading += glm.twoPI;
                bndLine.Add(arr[i]);
            }
        }

        public void PreCalcBoundaryLines()
        {
            int j = bndLine.Count - 1;
            //clear the list, constant is easting, multiple is northing
            calcList.Clear();
            vec2 constantMultiple = new vec2(0, 0);

            Northingmin = Northingmax = bndLine[0].northing;
            Eastingmin = Eastingmax = bndLine[0].easting;

            for (int i = 0; i < bndLine.Count; j = i++)
            {
                if (Northingmin > bndLine[i].northing) Northingmin = bndLine[i].northing;
                if (Northingmax < bndLine[i].northing) Northingmax = bndLine[i].northing;
                if (Eastingmin > bndLine[i].easting) Eastingmin = bndLine[i].easting;
                if (Eastingmax < bndLine[i].easting) Eastingmax = bndLine[i].easting;

                //check for divide by zero
                if (Math.Abs(bndLine[i].northing - bndLine[j].northing) < 0.00000000001)
                {
                    constantMultiple.easting = bndLine[i].easting;
                    constantMultiple.northing = 0;
                    calcList.Add(constantMultiple);
                }
                else
                {
                    //determine constant and multiple and add to list
                    constantMultiple.easting = bndLine[i].easting - ((bndLine[i].northing * bndLine[j].easting)
                                    / (bndLine[j].northing - bndLine[i].northing)) + ((bndLine[i].northing * bndLine[i].easting)
                                        / (bndLine[j].northing - bndLine[i].northing));
                    constantMultiple.northing = (bndLine[j].easting - bndLine[i].easting) / (bndLine[j].northing - bndLine[i].northing);
                    calcList.Add(constantMultiple);
                }
            }
        }

        public bool IsPointInsideBoundary(vec3 testPointv3)
        {
            if (calcList.Count < 3) return false;
            int j = bndLine.Count - 1;
            bool oddNodes = false;

            if (testPointv3.northing > Northingmin || testPointv3.northing < Northingmax || testPointv3.easting > Eastingmin || testPointv3.easting < Eastingmax)
            {
                //test against the constant and multiples list the test point
                for (int i = 0; i < bndLine.Count; j = i++)
                {
                    if ((bndLine[i].northing < testPointv3.northing && bndLine[j].northing >= testPointv3.northing)
                    || (bndLine[j].northing < testPointv3.northing && bndLine[i].northing >= testPointv3.northing))
                    {
                        oddNodes ^= ((testPointv3.northing * calcList[i].northing) + calcList[i].easting < testPointv3.easting);
                    }
                }
            }
            return oddNodes; //true means inside.
        }

        public bool IsPointInsideBoundary(vec2 testPointv2)
        {
            if (calcList.Count < 3) return false;
            int j = bndLine.Count - 1;
            bool oddNodes = false;

            if (testPointv2.northing > Northingmin || testPointv2.northing < Northingmax || testPointv2.easting > Eastingmin || testPointv2.easting < Eastingmax)
            {
                //test against the constant and multiples list the test point
                for (int i = 0; i < bndLine.Count; j = i++)
                {
                    if ((bndLine[i].northing < testPointv2.northing && bndLine[j].northing >= testPointv2.northing)
                    || (bndLine[j].northing < testPointv2.northing && bndLine[i].northing >= testPointv2.northing))
                    {
                        oddNodes ^= ((testPointv2.northing * calcList[i].northing) + calcList[i].easting < testPointv2.easting);
                    }
                }
            }
            return oddNodes; //true means inside.
        }

        public void DrawBoundaryLine()
        {
            ////draw the perimeter line so far
            if (bndLine.Count < 1) return;
            //GL.PointSize(2);
            GL.LineWidth(2);
            int ptCount = bndLine.Count;
            //if (isDriveThru) GL.Color3(0.25f, 0.752f, 0.860f);
            //else
            GL.Begin(PrimitiveType.Lines);
            for (int h = 0; h < ptCount; h++) GL.Vertex3(bndLine[h].easting, bndLine[h].northing, 0);
            //GL.Color3(0.95f, 0.972f, 0.90f);
            //GL.Vertex3(bndLine[0].easting, bndLine[0].northing, 0);
            GL.End();

            //ptCount = bdList.Count;
            //if (ptCount < 1) return;
            //gl.PointSize(4);
            //gl.Color(0.19f, 0.932f, 0.70f);
            //gl.Begin(OpenGL.GL_POINTS);
            ////gl.Vertex(closestBoundaryPt.easting, closestBoundaryPt.northing, 0);
            //gl.End();
        }

        //obvious
        public void CalculateBoundaryArea()
        {
            int ptCount = bndLine.Count;
            if (ptCount < 1) return;

            area = 0;         // Accumulates area in the loop
            int j = ptCount - 1;  // The last vertex is the 'previous' one to the first

            for (int i = 0; i < ptCount; j = i++)
            {
                area += (bndLine[j].easting + bndLine[i].easting) * (bndLine[j].northing - bndLine[i].northing);
            }
            area = Math.Abs(area / 2);
        }
    }
}