using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ScrollGeneratorEffect
{
    public class Polygon
    {
        public class FlEdge
        {
            public PointF A { get; }
            public PointF B { get; }
            public float CrossProd { get; } = 0;
            public float PartialX { get; } = 0;
            public float PartialY { get; } = 0;

            public FlEdge() { }


            public FlEdge(PointF A, PointF B)
            {
                this.A = A;
                this.B = B;
                float crossP = EdgeCrossProduct(A, B);
                CrossProd = crossP;
                PartialX = EdgePartialX(crossP, A, B);
                PartialY = EdgePartialY(crossP, A, B);
            }
            //for shoelace formula implementation for finding the centroid of a polygon (and, coincidentally, its area)
            private static float EdgePartialX(float crossProd, PointF p1, PointF p2) { return crossProd * (p1.X + p2.X); }
            private static float EdgePartialY(float crossProd, PointF p1, PointF p2) { return crossProd * (p1.Y + p2.Y); }
            private static float EdgeCrossProduct(PointF p1, PointF p2) { return (p1.X * p2.Y) - (p2.X * p1.Y); }

            //wrapper for minDistance that can be called "on" a given edge
            public virtual Tuple<float, PointF> DistanceToEdge(PointF leafCtr)
            {
                return MinDistance(A, B, leafCtr);
            }

            //wrapper for Polygon.crossEdge that can be called "on" a given edge
            public virtual bool CrossEdge(PointF prntCtr, PointF leafCtr)
            {
                return Polygon.CrossEdge(A, B, prntCtr, leafCtr);
            }
        }


        public class CircularEdge : FlEdge
        {
            public PointF Ctr { get; }
            public float Rad { get; } = 0;

            public CircularEdge(PointF pt, float dist)
            {
                Ctr = pt;
                Rad = dist;
            }


            public override Tuple<float, PointF> DistanceToEdge(PointF leafCtr)
            {
                return DistToCircumference(Ctr, Rad, leafCtr);
            }


            //wrapper for minDistance that can be called "on" a given edge
            public override bool CrossEdge(PointF prntCtr, PointF leafCtr)
            {
                return CrossCircularEdge(Ctr, Rad, prntCtr, leafCtr);
            }
        }


        public List<PointF> Vertices { get; set; } = new List<PointF>();
        public List<FlEdge> Edges { get; set; } = new List<FlEdge>();
        public PointF Centroid { get; } = new PointF(0, 0);
        public float Area { get; } = 0f;
        public float radius = 0f;

        public Polygon() { }


        public Polygon(float[][] coords, bool useScale = true)
            : this(coords.Select(x => new PointF(x[0], x[1])).ToList(), useScale) { }


        public Polygon(List<PointF> vertexLst, bool useScale = true)
        {
            if (useScale) { vertexLst = ScaleVertices(vertexLst); }
            if (vertexLst.Count > 2)
            {
                float partialX = 0; //sum is shoelace formula for centroid (w/area-derived coeficcient)
                float partialY = 0; //sum is shoelace formula for centroid (w/area-derived coeficcient)
                Area = 0f;      //sum is shoelace formula for area

                PointF lastVrtx = vertexLst[^1];
                foreach (PointF vrtx in vertexLst)
                {
                    Vertices.Add(vrtx);

                    FlEdge newEdge = new(lastVrtx, vrtx);
                    Area += newEdge.CrossProd;
                    partialX += newEdge.PartialX;
                    partialY += newEdge.PartialY;
                    Edges.Add(newEdge);

                    lastVrtx = vrtx;
                }
                Area = 0.5f * Area; //finishing shoelace formula for area
                float areaRatio = 1f / (6f * Area); //coeficcient for shoelace formula for centroid
                Centroid = new PointF(areaRatio * partialX, areaRatio * partialY); //finishing shoelace formula for centroid

                //too many edge checks will slow down tree building considerably
                //convert to a circle around centroid
                if (Edges.Count > 8)
                { BecomeCircular(); }
            }
        }


        private static List<PointF> ScaleVertices(List<PointF> vertexLst)
        {
            List<PointF> scaledList = new();

            foreach (PointF vrtx in vertexLst)
            {
                scaledList.Add(new PointF(vrtx.X * Configs.scale, vrtx.Y * Configs.scale));
            }
            return scaledList;
        }


        public void BecomeCircular()
        {
            //find vertex closest to centroid - distance will be our radius
            List<float> ctrDists = new();

            foreach (PointF vrtx in Vertices) { ctrDists.Add(Trig.PointDist(Centroid, vrtx)); }
            radius = ctrDists.Min();

            //remove all "normal" edges, substitute "circular" one
            Edges.Clear();
            CircularEdge oneEdge = new(Centroid, radius);
            Edges.Add(oneEdge);
        }


        public string PrintPolygon()
        {
            string polygonPrint = "";
            for (int idx = 0; idx < Vertices.Count; idx++)
            {
                polygonPrint += $"vertex #{idx}: ({Vertices[idx].X:F0}, {Vertices[idx].Y:F0})\n";
            }
            for (int idx = 0; idx < Edges.Count; idx++)
            {
                polygonPrint += $"edge #{idx}: ({Edges[idx].A.X:F0}, {Edges[idx].A.Y:F0}), ({Edges[idx].B.X:F0}, {Edges[idx].B.Y:F0})\n";
            }
            if (radius != 0)
            {
                polygonPrint += $"This polygon was converted to a circle with center " +
                                $"({Centroid.X:F0}, {Centroid.Y:F0}) and radius " +
                                $"{radius:F0} to conserve computational resources.\n";
            }

            return polygonPrint;
        }


        private class FPointVectr
        {
            private float X { get; }
            private float Y { get; }


            private FPointVectr(PointF A, PointF B)
            {
                X = B.X - A.X;
                Y = B.Y - A.Y;
            }


            public static float VectrDotProduct(PointF v1strt, PointF v1end, PointF v2strt, PointF v2end)
            {
                FPointVectr v1 = new(v1strt, v1end);
                FPointVectr v2 = new(v2strt, v2end);

                return (v1.X * v2.X) + (v1.Y * v2.Y);
            }


            public static float VectrCrossProduct(PointF v1strt, PointF v1end, PointF v2strt, PointF v2end)
            {
                FPointVectr v1 = new(v1strt, v1end);
                FPointVectr v2 = new(v2strt, v2end);

                return (v1.X * v2.Y) - (v2.X * v1.Y);
            }
        }


        public static Tuple<float, PointF> DistToCircumference(PointF ctr, float rad, PointF leafCtr)
        {
            PointF circumpoint = GetCircumpoint(ctr, rad, leafCtr);
            float distToPt = Trig.PointDist(leafCtr, circumpoint);
            return Tuple.Create(distToPt, circumpoint);
        }


        private static PointF GetCircumpoint(PointF ctr, float rad, PointF nodeCtr)
        {
            float angleFromCtr = Trig.AngleToPoint(ctr, nodeCtr);

            float circumpointX = ctr.X + ((float)Math.Cos(angleFromCtr) * rad);
            float circumpointY = ctr.Y + ((float)Math.Sin(angleFromCtr) * rad);
            return new PointF(circumpointX, circumpointY);
        }


        public static bool CrossCircularEdge(PointF polyCtr, float rad, PointF Ctr1, PointF Ctr2)
        {
            float distTo1 = Trig.PointDist(polyCtr, Ctr1);
            float distTo2 = Trig.PointDist(polyCtr, Ctr2);
            return distTo1 >= rad || distTo2 >= rad;
        }


        //adapted from https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/
        // See https://www.geeksforgeeks.org/orientation-3-ordered-points/ for details of orientation formula.      
        //Checks if line segments 'E1-E2' and 'Ctr1-Ctr2' intersect
        public static bool CrossEdge(PointF E1, PointF E2, PointF Ctr1, PointF Ctr2)
        {
            //find the four orientations needed for general case
            int o1 = orientation(E1, E2, Ctr1);
            int o2 = orientation(E1, E2, Ctr2);
            int o3 = orientation(Ctr1, Ctr2, E1);
            int o4 = orientation(Ctr1, Ctr2, E2);

            //find orientation of ordered triplet (P, Q, R).
            static int orientation(PointF P, PointF Q, PointF R)
            {
                float orVal = ((Q.Y - P.Y) * (R.X - Q.X)) - ((Q.X - P.X) * (R.Y - Q.Y));

                if (orVal == 0)
                {
                    return 0; // collinear
                }

                return orVal > 0 ? 1 : -1; // clock or counterclock wise
            }

            //general case - these two segments WILL NOT be collinear
            return o1 != o2 && o3 != o4;
        }


        public static Tuple<float, PointF> MinDistance(PointF A, PointF B, PointF E)
        {
            float dotProdAB_BE = FPointVectr.VectrDotProduct(A, B, B, E);
            float dotProdAB_AE = FPointVectr.VectrDotProduct(A, B, A, E);
            PointF closestPt;


            float minDist;
            // Case 1: The nearest point from the point E on the line segment AB is point B itself if the dot product of vector AB(A to B) and vector BE(B to E) is positive where E is the given point. 
            if (dotProdAB_BE > 0)
            {
                // Finding the magnitude
                minDist = Trig.PointDist(E, B);
                closestPt = B;
            }

            // Case 2: The nearest point from the point E on the line segment AB is point A itself if the dot product of vector AB(A to B) and vector AE(A to E) is negative where E is the given point.
            else if (dotProdAB_AE < 0)
            {
                minDist = Trig.PointDist(E, A);
                closestPt = A;
            }

            // Case 3: Otherwise, if the dot product is 0, then the point E is perpendicular to the line segment AB and the perpendicular distance to the given point E from the line segment AB is the shortest distance. If some arbitrary point F is the point on the line segment which is perpendicular to E, then the perpendicular distance can be calculated as |EF| = |(AB X AE)/|AB|| 
            else
            {
                // Finding the perpendicular distance
                float ABdist = Trig.PointDist(A, B);
                minDist = FPointVectr.VectrCrossProduct(A, B, A, E) / ABdist;
                closestPt = FindPoint(A, B, E, minDist);
            }

            return Tuple.Create(minDist, closestPt);
        }


        public static PointF FindPoint(PointF A, PointF B, PointF E, float distEtoF)
        {
            // xF = (1 - t) * xA + t * xB
            // yF = (1 - t) * yA + t * yB
            // the distance from (xA,yA) to (xF,yF) is t times the distance from (xA,yA) to (xB,yB).

            //triangle: hypotenuse = distEtoA, adjacent = distEtoF, opposite = distAtoF
            //angle = Math.Acos(adjacent/hypotenuse)
            //angle = Math.Asin(opposite/hypotenuse)  ->  opposite/hypotenuse = Math.Sin(angle)  ->  
            //opposite = hypotenuse * Math.Sin(angle)

            float distAtoB = Trig.PointDist(A.X, A.Y, B.X, B.Y);
            float distEtoA = Trig.PointDist(A.X, A.Y, E.X, E.Y);
            float anglAEF = (float)Math.Acos(distEtoF / distEtoA);
            float distAtoF = distEtoA * (float)Math.Sin(anglAEF);
            float xF = ((1 - (distAtoF / distAtoB)) * A.X) + (distAtoF / distAtoB * B.X);
            float yF = ((1 - (distAtoF / distAtoB)) * A.Y) + (distAtoF / distAtoB * B.Y);
            return new PointF(xF, yF);
        }
    }
}