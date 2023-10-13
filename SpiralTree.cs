using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ScrollGeneratorEffect
{
    public class SpiralTree
    {
        private List<SpiralNode> Nodes { get; set; } = new List<SpiralNode>();
        private readonly Polygon _container;
        private readonly Random _rnd;
        private string _treeLog = "";

        public SpiralNode this[int i]
        {
            get => Nodes[i];
            set => Nodes[i] = value;
        }

        public int Count => Nodes.Count;


        public SpiralTree()
        {
            _rnd = new Random();
        }

        public SpiralTree(Polygon container, float initRad = 0)
        {
            _rnd = new Random();
            Configs.InitAll(_rnd);
            _container = container;

            initRad = initRad == 0f ? Configs.initRadius : initRad;

            if (Configs.RANDANG) { Configs.rootAngle = Configs.Random(0f, (float)Math.Tau, _rnd); }
            float initAngl = Configs.rootAngle;

            if (Configs.TWIN)
            {
                GrowTwins(_container.Centroid, initRad, initAngl, Configs.twinRatio);
            }
            else
            {
                SpiralNode root = new(_container.Centroid, initRad, initAngl);
                Nodes.Add(root);
                _ = CheckEdgeFit(root); //initialize edge data

                float angl = 2f * (float)Math.Tau;
                float x = root.SprlCtr.X + (root.Rad * 0.099f * angl * (float)Math.Cos(angl + root.StrtAngl));
                float y = root.SprlCtr.Y + (root.Rad * 0.099f * angl * (float)Math.Sin(angl + root.StrtAngl));
                root.StrtPt = new PointF(x, y);
            }
        }


        public void CreateLog()
        {
            if (!Configs.LOG) { return; }

            string dName = AppDirectoryPath();
            if (dName == "") { return; }

            string fName = "treeLog.txt";
            string fPath = Path.Combine(dName, fName);

            if (!File.Exists(fPath))
            {
                // Create a file to write to.
                using StreamWriter sw = File.CreateText(fPath);
                sw.Write(Configs.LogProperties());
                sw.Write(_container.PrintPolygon());
            }
            else
            {
                using StreamWriter sw = new(fPath, true);
                sw.Write(Configs.LogProperties());
                sw.Write(_container.PrintPolygon());
            }
        }


        public void AddToLog(string lgString)
        {
            _treeLog += lgString;
        }


        public void WriteLog()
        {
            if (!Configs.LOG) { return; }

            string dName = AppDirectoryPath();
            if (dName == "") { return; }

            string fName = "treeLog.txt";
            string fPath = Path.Combine(dName, fName);

            if (!File.Exists(fPath))
            {
                // Create a file to write to.
                using StreamWriter sw = File.CreateText(fPath);
                sw.Write(_treeLog);
            }
            else
            {
                using StreamWriter sw = new(fPath, true);
                sw.Write(_treeLog);
            }
        }


        public List<PointF[]> PlotSpiralTreePoints()
        {
            List<PointF[]> spiralFlPtsLst = new();
            foreach (SpiralNode node in Nodes)
            {
                PointF[] spiralFlPts = node.PlotSpiralPoints();
                spiralFlPtsLst.Add(spiralFlPts);
            }

            return spiralFlPtsLst;
        }


        //produces complement Hue and Value, at the same Saturation
        public List<int[]> GetRGBRandomComplement()
        {
            int H = _rnd.Next(0, 100);
            int S = _rnd.Next(0, 100);
            int V = _rnd.Next(0, 100);
            //increase value contrast
            if (Math.Abs(V - 50) <= 20) { V = V > 50? V + 15 : V - 15; }

            int[] color = HsvToRgb(H, S, V);
            int[] colorHSV = { H, S, V };   //for debugging only

            H = (H + 50) % 100;
            V = (V + 50) % 100;
            //increase value contrast
            if (Math.Abs(V - 50) <= 20) { V = V > 50 ? V + 15 : V - 15; }

            int[] complement = HsvToRgb(H, S, V);
            int[] complementHSV = { H, S, V };  //for debugging only

            return new List<int[]> { color, complement, colorHSV, complementHSV };
        }


        //ranges for each of h, s, and v is 0-100
        //translation from javascript function found at https://gist.github.com/mjackson/5311256
        private int[] HsvToRgb(int _h, int _s, int _v)
        {
            float h = _h / 100f;
            float s = _s / 100f;
            float v = _v / 100f;
            float r = 0;
            float g = 0;
            float b = 0;

            float i = (float)Math.Floor(h * 6);
            float f = h * 6 - i;
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            switch (i % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                case 5: r = v; g = p; b = q; break;
            }
            r *= 255;
            g *= 255;
            b *= 255;

            int[] _rgb = { (int)r, (int)g, (int)b };
            return _rgb;
        }


        public List<PointF[]> PlotTreeEnvelopePoints()
        {
            List<PointF[]> outerFlPtsLst = new();
            foreach (SpiralNode node in Nodes)
            {
                PointF[] outerFlPts = node.PlotNodeEnvelopePoints();
                outerFlPtsLst.Add(outerFlPts);
            }

            return outerFlPtsLst;
        }


        //let the app make sure we have/can create a MyDocuments
        //directory to put files into - and produce appropriate error messages
        public static string AppDirectoryPath()
        {
            string myDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appFolderName = "ScrollGenerator";
            string dirPath = Path.Combine(myDocsPath, appFolderName);

            return !Directory.Exists(dirPath) ? "" : dirPath;
        }


        //this overload uses cached points list to avoid recalculating everything again 
        public static void WriteSVG(List<PointF[]> spiralPtsLst, int boxWidth, int boxHeight, string scrollColor, int strokeWidth)
        {
            string dName = AppDirectoryPath();
            if (dName == "") { return; }

            string timeStmp = DateTime.Now.ToString("s").Replace(':', '-').Replace('T', '-');
            string fName = $"scroll-{timeStmp}.svg";
            string fPath = Path.Combine(dName, fName);

            if (!File.Exists(fPath))
            {
                using StreamWriter sw = File.CreateText(fPath);
                sw.Write(buildSVG(spiralPtsLst, boxWidth, boxHeight, scrollColor, strokeWidth));
            }

            static string buildSVG(List<PointF[]> spiralPtsLst, int boxWidth, int boxHeight, string scrollColor, int strokeWidth)
            {
                string svgFileString = $"<svg width=\"{boxWidth}\" height=\"{boxHeight}\" " +
                                        $"viewBox=\"0 0 {boxWidth} {boxHeight}\">\n";
                string sprlStrtString = $"  <path style=\"fill:none; stroke:{scrollColor}; stroke-width:{strokeWidth}\"\n    d=\"";
                string sprlEndString = $"\">\n  </path>\n";
                string sprlPtsString = "";

                foreach (PointF[] arry in spiralPtsLst)
                {
                    sprlPtsString += sprlStrtString;
                    sprlPtsString += $"M {arry[0].X},{arry[0].Y} ";
                    sprlPtsString += $"C {arry[0].X},{arry[0].Y}";

                    foreach (PointF pt in arry[1..]) { sprlPtsString += $" {pt.X},{pt.Y}"; }
                    sprlPtsString += sprlEndString;
                }
                svgFileString += sprlPtsString;
                svgFileString += "</svg>";

                return svgFileString;
            }
        }


        public void WriteSVG(int boxWidth, int boxHeight, string scrollColor, int strokeWidth)
        {
            string dName = AppDirectoryPath();
            if (dName == "") { return; }

            string timeStmp = DateTime.Now.ToString("s").Replace(':', '-').Replace('T', '-');
            string fName = $"scroll-{timeStmp}.svg";
            string fPath = Path.Combine(dName, fName);

            if (!File.Exists(fPath))
            {
                using StreamWriter sw = File.CreateText(fPath);
                sw.Write(buildSVG(boxWidth, boxHeight, scrollColor, strokeWidth));
            }

            string buildSVG(int boxWidth, int boxHeight, string scrollColor, int strokeWidth)
            {
                string svgFileString = $"<svg width=\"{boxWidth}\" height=\"{boxHeight}\" " +
                                        $"viewBox=\"0 0 {boxWidth} {boxHeight}\">\n";
                string sprlStrtString = $"  <path style=\"fill:none; stroke:{scrollColor}; stroke-width:{strokeWidth}\"\n    d=\"";
                string sprlEndString = $"\">\n  </path>\n";
                string sprlPtsString = "";

                foreach (SpiralNode node in Nodes)
                {
                    PointF[] spiralPts = node.PlotSpiralPoints();

                    sprlPtsString += sprlStrtString;
                    sprlPtsString += $"M {spiralPts[0].X},{spiralPts[0].Y} ";
                    sprlPtsString += $"C {spiralPts[0].X},{spiralPts[0].Y}";

                    foreach (PointF pt in spiralPts[1..]) { sprlPtsString += $" {pt.X},{pt.Y}"; }
                    sprlPtsString += sprlEndString;
                }
                svgFileString += sprlPtsString;
                svgFileString += "</svg>";

                return svgFileString;
            }
        }


        private static List<PointF> CenterPair(PointF initCtr, float strtAngl, float rad1, float twinScale)
        {
            float rad2 = rad1 * twinScale;
            float shiftAngl = 0.05f;
            float halfDist = 0.5f * Configs.rootBuffer;

            float ctr1X = initCtr.X + ((float)Math.Cos(strtAngl - shiftAngl) * (rad1 + halfDist));
            float ctr1Y = initCtr.Y + ((float)Math.Sin(strtAngl - shiftAngl) * (rad1 + halfDist));

            float ctr2X = initCtr.X + ((float)Math.Cos(Trig.ComplementAngle(strtAngl + shiftAngl)) *
                          (rad2 + halfDist * twinScale));
            float ctr2Y = initCtr.Y + ((float)Math.Sin(Trig.ComplementAngle(strtAngl + shiftAngl)) *
                         (rad2 + halfDist * twinScale));

            float ctrYavg = Math.Abs(Math.Abs(initCtr.Y - ctr1Y) - Math.Abs(initCtr.Y - ctr2Y)) / 2;
            ctr2Y -= ctrYavg;
            ctr1Y -= ctrYavg;

            return new List<PointF>() { new PointF(ctr1X, ctr1Y), new PointF(ctr2X, ctr2Y) };
        }


        public void GrowTwins(PointF initCtr, float rad, float strtAngl, float twinScale = 1)
        {
            if (!Configs.TWIN) { return; }

            if (twinScale > 1) { twinScale = 1f; }
            if (twinScale < 0.6) { twinScale = 0.6f; }

            List<PointF> ctrLst = CenterPair(initCtr, strtAngl, rad, twinScale);
            PointF ctr1 = ctrLst[1];
            PointF ctr2 = ctrLst[0];

            SpiralNode root = new(ctr1, rad, strtAngl);
            Nodes.Add(root);
            _ = CheckEdgeFit(root);
            SpiralNode twin = new(ctr2, rad * twinScale, Trig.ComplementAngle(strtAngl), true);
            if (CheckEdgeFit(twin)) { Nodes.Add(twin); }

            //average the two roots' start points, for solid transition
            float avgX = (root.StrtPt.X + twin.StrtPt.X) / 2;
            float avgY = (root.StrtPt.Y + twin.StrtPt.Y) / 2;

            root.StrtPt = new PointF(avgX, avgY);
            twin.StrtPt = new PointF(avgX, avgY);
        }


        public float FirstOpenAngl(int idx) //returns absolute angle
        {
            SpiralNode idxNode = Nodes[idx];

            //first leaf on a root node
            if (!idxNode.HasLeaves && idxNode.PrntIdx == -1)
            {
                float rootDist = 0;
                if (Configs.TWIN)
                { rootDist = Trig.PointDist(Nodes[0].Ctr, Nodes[1].Ctr); }
                return idxNode.RootBaseAngle(idx, rootDist);    //returns absolute angle
            }

            float firstOpen;
            if (!idxNode.HasLeaves) //first leaf on index node 
            {
                SpiralNode prntNode = Nodes[idxNode.PrntIdx];
                int problemSibIdx = prntNode.ObstructingSibIdx(idx);
                if (problemSibIdx != -1)
                { //index node's parent has an obstructing sibling
                    SpiralNode problemSibNode = Nodes[problemSibIdx];
                    float problemSibAngle = SpiralNode.SibConflictAngle(idxNode, problemSibNode);   //returns absolute angle
                    return problemSibAngle != -100 ? problemSibAngle : idxNode.BaseAngle(prntNode.Rad);//sibling too far away to obstruct leaves
                }
                else //index node has no obstructing siblings
                { return idxNode.BaseAngle(prntNode.Rad); } //returns absolute angle
            }
            else
            {
                int prevLeafIdx = idxNode.LastLeafIdx();
                float prevLeafAngl = idxNode.LastLeafAngle(); //returns absolute angle
                float prevLeafRad = Nodes[prevLeafIdx].Rad;
                float prevLeafArc = Trig.CalcLeafArcSpan(idxNode.Rad, prevLeafRad);

                firstOpen = prevLeafAngl - (idxNode.Clock * (Configs.nodeHalo + prevLeafArc));
            }
            return Trig.Mod2PI(firstOpen);
        }


        public bool CheckFit(SpiralNode newLeaf)
        {

            bool fitsNhbrs = CheckNhbrFit(newLeaf);
            bool fitsEdges = CheckEdgeFit(newLeaf);

            return fitsNhbrs && fitsEdges;
        }


        public bool CheckNhbrFit(SpiralNode newLeaf)
        {
            int leafPrntIdx = newLeaf.PrntIdx;

            //for testing fit check:
            //newLeaf.TmpNhbrIdxs.Clear();
            //newLeaf.TmpNhbrDists.Clear();

            //scan for nodes that collide with leaf
            for (int idx = 0; idx < Nodes.Count; idx++)
            {
                if (leafPrntIdx == idx) { continue; }//parent isn't a neighbor

                SpiralNode curr = Nodes[idx];
                float skinDistToLeaf = SpiralNode.GetSurfaceDist(newLeaf, curr);
                float cutoff = Configs.nodeBuffer;

                //collision cutoff is smaller for sibling 
                if (leafPrntIdx == curr.PrntIdx) { cutoff = 0.65f * Configs.nodeBuffer; }

                //if (skinDistToLeaf < 10) //for testing fit check
                //{
                //    newLeaf.TmpNhbrIdxs.Add(idx);
                //    newLeaf.TmpNhbrDists.Add(skinDistToLeaf);
                //}

                if (skinDistToLeaf < cutoff) //collision check
                {
                    newLeaf.NhbrIdxs.Add(idx);
                    newLeaf.NhbrDists.Add(skinDistToLeaf);
                }
            }

            //leaf won't fit as is, needs further adjustment
            return newLeaf.NhbrIdxs.Count <= 0;
        }


        public bool CheckEdgeFit(SpiralNode newLeaf)
        {
            PointF closestPt;

            List<float> edgeDists = new();
            List<PointF> edgePoints = new();

            List<float> prntEdgeDists = new();
            PointF prntCtr = new(0, 0);
            float maxLeafFitDist = 0f;
            if (newLeaf.PrntIdx != -1)
            {
                SpiralNode prntNode = Nodes[newLeaf.PrntIdx];
                float prntRad = prntNode.Rad;
                prntCtr = prntNode.Ctr;
                prntEdgeDists.AddRange(prntNode.EdgeDists);
                maxLeafFitDist = prntRad + (2 * newLeaf.Rad) + newLeaf.Lift + Configs.nodeBuffer;
            }

            for (int idx = 0; idx < _container.Edges.Count; idx++)
            {
                Polygon.FlEdge edge = _container.Edges[idx];
                bool jumpedEdge = false;
                if (newLeaf.PrntIdx != -1) //root nodes have no parent: can't do this check
                {
                    if (Math.Abs(prntEdgeDists[idx]) < maxLeafFitDist) //parent is close enough to edge to warrant checking
                    {
                        jumpedEdge = edge.CrossEdge(prntCtr, newLeaf.Ctr);
                    }
                }

                Tuple<float, PointF> result = edge.DistanceToEdge(newLeaf.Ctr);
                float edgeDist = result.Item1;
                closestPt = result.Item2;
                float skinEdgeDist = SpiralNode.GetSkinDistToPt(newLeaf, closestPt);
                //found a deal breaker
                if (jumpedEdge || skinEdgeDist < Configs.nodeBuffer / 2) { edgeDist = 0; }
                edgeDists.Add(Math.Abs(edgeDist));
                edgePoints.Add(closestPt);
            }
            //at this point edgeDists/edgePoints contains [corrected] copy of leaf's parent's data
            //overwrite existing [inherited] data from leaf with updated data
            newLeaf.EdgeDists = edgeDists;
            newLeaf.EdgePoints = edgePoints;

            return !newLeaf.EdgeDists.Contains(0);
        }


        public bool TryAdjustToEdges(ref SpiralNode newLeaf, SpiralNode prntNode, bool limited = false)
        {
            List<float> edgeFitAngls = new();
            float initStartAngl = FirstOpenAngl(newLeaf.PrntIdx);

            int edgeIdx = 0;
            int problemEdgeCount = newLeaf.EdgeDists.Count(x => x == 0);

            //get idx of either the closest (to leaf) edge or, most likely case: the only one with conflict
            if (problemEdgeCount == 1) { edgeIdx = newLeaf.EdgeDists.FindIndex(x => x == 0); }
            if (problemEdgeCount > 1)   //case of multiple conflicting edges
            {
                PointF leafCtr = newLeaf.Ctr;
                IEnumerable<(int idx, float dist)> edgePtDistFltr =
                    newLeaf.EdgePoints.Select((x, i) => (i, Trig.PointDist(x, leafCtr)));
                List<(int idx, float dist)> edgePtDists = edgePtDistFltr.ToList();
                edgePtDists.Sort((a, b) => a.dist > b.dist ? 1 : a.dist < b.dist ? -1 : 0);    //ascending by dist

                (int idx, float dist) = edgePtDists[0];
                edgeIdx = idx;  //index of edge closest to leaf
            }

            float edgeArc = Trig.EdgeFitArc(prntNode.EdgePoints[edgeIdx], prntNode.Ctr, prntNode.Rad, newLeaf.Rad, newLeaf.Lift);
            if (edgeArc == -100) { return false; }

            float anglFromPrnt = Trig.AngleToPoint(prntNode.Ctr, prntNode.EdgePoints[edgeIdx]);
            //check BOTH possible angles around leaf tangent
            //The option going in the direction of leaf growth (decreasing angles for positive Clock) 
            //needs to be tried first. If both options are valid (and both work potentially fit), 
            //the option tried first will be kept. To grow maximum number of leaves, we want
            //*this* leaf to fit before the edge, and the next one (if any), AFTER it

            List<float> tmpLst = new() { anglFromPrnt + edgeArc, anglFromPrnt - edgeArc };
            if (prntNode.Clock < 0) { tmpLst.Sort(); }

            float angleToFit1 = Trig.Mod2PI(tmpLst[0]);
            float angleToFit2 = Trig.Mod2PI(tmpLst[1]);

            string edgLg = "";
            if (Configs.LOG)
            {
                edgLg = $"angle from leaf's parent to edge#{edgeIdx}: {anglFromPrnt:F3}, distance: " +
                        $"{prntNode.EdgeDists[edgeIdx]:F3}; edge fit arc: {edgeArc:F3} produced fit angles: ";
            }

            bool inRange1 = limited ? prntNode.AngleInSproutRange(angleToFit1) : prntNode.AngleInGrowthRange(initStartAngl, angleToFit1);
            if (Configs.LOG) { edgLg += $"{angleToFit1:F3} " + (inRange1 ? "" : "(out of range) "); }

            bool inRange2 = limited ? prntNode.AngleInSproutRange(angleToFit2) : prntNode.AngleInGrowthRange(initStartAngl, angleToFit2);
            if (Configs.LOG) { edgLg += $" and {angleToFit2:F3} " + (inRange2 ? "" : "(out of range) \n"); }

            if (!inRange1 && !inRange2) { return false; }  //no valid resolutions exist, bail

            float origStrt = newLeaf.StrtAngl;
            int origSlot = newLeaf.Slot;
            bool fitsAll;
            int newSlot;

            if (inRange1)
            {
                newSlot = prntNode.CorrectedSlot(angleToFit1, origSlot);
                if (Configs.GRAD && Configs.SMTOLG) { newLeaf.Slot = newSlot; }
                newLeaf.AdjustLeaf(prntNode, angleToFit1, newSlot, _rnd);
                fitsAll = CheckFit(newLeaf);

                if (Configs.LOG)
                {
                    edgLg += $"adjusting leaf's start angle to {angleToFit1:F3} ";
                    edgLg += (fitsAll ? "resolved " : "did not resolve ") + "all existing conflicts";
                    AddToLog(edgLg + "\n");
                }
                if (fitsAll || !inRange2) { return fitsAll; }
                else if (inRange2 && !newLeaf.EdgeDists.Contains(0) && newLeaf.NhbrIdxs.Count > 0)
                {
                    //if both solutions are in range, attempt to resolve neighbor conflicts for the first 
                    //attempted before moving on to the second
                    fitsAll = TryAdjustToNhbrs(ref newLeaf, prntNode, false, limited);
                    if (fitsAll) { return true; }
                }
            }

            if (inRange2)
            {
                newSlot = prntNode.CorrectedSlot(angleToFit2, origSlot);
                if (Configs.GRAD && Configs.SMTOLG) { newLeaf.Slot = newSlot; }

                newLeaf.AdjustLeaf(prntNode, angleToFit2, newSlot, _rnd);
                fitsAll = CheckFit(newLeaf);

                if (Configs.LOG)
                {
                    edgLg += $"adjusting leaf's start angle to {angleToFit2:F3} ";
                    edgLg += (fitsAll ? "resolved " : "did not resolve ") + "all existing conflicts";
                    AddToLog(edgLg + "\n");
                }
                return fitsAll;  //caller will check neighbors on this one
            }
            return false;
        }


        public bool TryMinAdjustment(ref SpiralNode newLeaf, SpiralNode prntNode)
        {
            float minNhbrDist = newLeaf.NhbrDists.Min();
            float initStartAngl = Trig.ComplementAngle(newLeaf.StrtAngl);
            int nhbrIdx = newLeaf.ClosestNhbrIdx();
            if (nhbrIdx < 0) { return false; }
            SpiralNode nhbrNode = Nodes[nhbrIdx];

            float ratioMult = Trig.RadRatioMult(newLeaf.Rad, nhbrNode.Rad);
            ratioMult = Math.Max(ratioMult, 2f);

            for (int i = 0; i < 2; i++)
            {
                int direction = -1 + 2 * i;
                float tryAngle = Trig.Mod2PI(initStartAngl + direction * ratioMult * Configs.nodeHalo);

                if (Configs.LOG)
                {
                    string dirStr = direction > 0 ? "+" : "-";
                    string minLg = $"attempt minimal adjustment angle {tryAngle:F3} (from {initStartAngl:F3}, " +
                                   $"at curr rad {newLeaf.Rad}) in {dirStr} direction for neighbor {nhbrIdx} " +
                                   $"(minNhbrDist={minNhbrDist:F3}, rad={nhbrNode.Rad:F3} " +
                                   $"anglFromPrnt={Trig.AngleToPoint(prntNode.Ctr, nhbrNode.Ctr)})\n";
                    AddToLog(minLg);
                }

                SpiralNode copy = new(newLeaf);

                copy.AdjustLeaf(prntNode, tryAngle, newLeaf.Slot, _rnd);
                bool startFits = CheckFit(copy);
                if (startFits || newLeaf.NhbrIdxs.Count == 0)
                {
                    newLeaf = copy;
                    if (Configs.LOG) { AddToLog($"minimal adjustment worked for idx: {Nodes.Count}\n"); }
                    return startFits;
                }
            }
            return false;
        }


        public bool TryAdjustToNhbrs(ref SpiralNode newLeaf, SpiralNode prntNode, bool aftrRslt = false, bool limited = false)
        {
            int origSlot = newLeaf.Slot;
            int maxTries = Configs.useSlots.Count;
            int prevConflictCount = newLeaf.NhbrIdxs.Count;
            List<float> alreadyTried = new();
            bool startFits = false;     //that's why we are here 

            while (!startFits && newLeaf.NhbrIdxs.Count > 0 && maxTries > 0)
            {
                maxTries--; //convenient limit counter

                //only edge conflicts
                if (newLeaf.NhbrIdxs.Count == 0 && newLeaf.EdgeDists.Contains(0)) { return false; }
                float prevDistSum = newLeaf.NhbrDists.Sum();

                //special case: very minor conflict
                float minNhbrDist = newLeaf.NhbrDists.Min();
                if (minNhbrDist > Configs.nodeBuffer * 0.2)    //a VERY small problem
                {
                    startFits = TryMinAdjustment(ref newLeaf, prntNode);
                    //either worked completely, of it's no longer neighbor problems
                    if (startFits || newLeaf.NhbrIdxs.Count == 0) { return startFits; }
                }

                //special case: nearest sibling collision
                if (newLeaf.NhbrIdxs.Count == 1 && newLeaf.NhbrIdxs[0] == Nodes.Count - 1 && Nodes[^1].PrntIdx == newLeaf.PrntIdx)
                {
                    float firstOpenAngl = FirstOpenAngl(newLeaf.PrntIdx);
                    float leafArc = Trig.CalcLeafArcSpan(prntNode.Rad, newLeaf.Rad);
                    float nextLeafAngl = Trig.Mod2PI(firstOpenAngl - (prntNode.Clock * (Configs.nodeHalo + leafArc)));
                    newLeaf.AdjustLeaf(prntNode, nextLeafAngl, newLeaf.Slot, _rnd);
                    startFits = CheckFit(newLeaf);
                    if (startFits)
                    {
                        if (Configs.LOG) { AddToLog($"sibling adjustment worked for idx: {Nodes.Count}\n"); }
                        return true;
                    }
                    else if (newLeaf.NhbrIdxs.Count == 0) { return false; } //it's no longer neighbor problems
                }

                float nhbrFitAngl = GetNeighborFitAngle(newLeaf, prntNode, aftrRslt);
                if (limited && !aftrRslt)
                {
                    if (!prntNode.AngleInSproutRange(nhbrFitAngl))
                    {
                        if (Configs.LOG) { AddToLog($"maximum adjustment for resprouted leaf exceeded. Gave up.\n"); }
                        return false;
                    }
                }

                //no [new] resolutions offered, bail
                if (nhbrFitAngl != -100) //returned a valid angle we tried before
                {
                    if (alreadyTried.Contains(nhbrFitAngl))
                    {
                        if (Configs.LOG) { AddToLog($"no new solutions found, giving up.\n"); }
                        return false;
                    }
                }
                else //returned -100
                {
                    if (Configs.LOG) { AddToLog($"no good alternatives found, giving up.\n"); }
                    return false;
                }

                int newSlot = prntNode.CorrectedSlot(nhbrFitAngl, origSlot);
                if (Math.Abs(newLeaf.Slot - newSlot) > 2)
                {
                    if (Configs.LOG)
                    {
                        string sltLog = $"at this angle, leaf slot should be {newSlot}, current slot {newLeaf.Slot}\n";
                        sltLog += "unsuitable adjustment scale, giving up. ";
                        AddToLog(sltLog);
                    }
                    return false;
                }

                alreadyTried.Add(nhbrFitAngl);
                if (!aftrRslt)
                {
                    newLeaf.AdjustLeaf(prntNode, nhbrFitAngl, newSlot, _rnd);
                    startFits = CheckFit(newLeaf);  //leaf has been re-centered, check fit again
                }
                else
                {// try backing up, but if it doesn't work, ditch the copy and
                 //start over like it never happened
                    aftrRslt = false; //after the first pass, this will become a problem if true
                    SpiralNode copy1 = new(newLeaf);
                    if (Configs.GRAD && !Configs.SMTOLG) { copy1.Slot = newSlot; }
                    copy1.AdjustLeaf(prntNode, nhbrFitAngl, newSlot, _rnd);
                    startFits = CheckFit(copy1);  //leaf has been re-centered, check fit again
                    if (startFits)
                    {
                        newLeaf = copy1;
                        if (Configs.LOG) { AddToLog($"adjustment worked for idx: {Nodes.Count}\n"); }
                        return true;
                    }
                    else if (Configs.LOG) { AddToLog("didn't work:\n" + newLeaf.PrintNodeNhbrData()); }
                }

                if (startFits)
                {
                    if (Configs.LOG) { AddToLog($"adjustment worked for idx: {Nodes.Count}\n"); }
                    return true;
                }
                else if (alreadyTried.Count > 3 && newLeaf.NhbrDists.Sum() < prevDistSum &&
                        prevConflictCount > newLeaf.NhbrIdxs.Count)
                {
                    if (Configs.LOG) { AddToLog($"adjustment attempts causing more problems, giving up.\n"); }
                    return false;
                } //attempts are making things worse
            }
            if (Configs.LOG) { AddToLog($"after exhausting maximum attempts, giving up.\n"); }
            return false;
        }



        public float GetNeighborFitAngle(SpiralNode newLeaf, SpiralNode prntNode, bool aftrRslt = false)
        {
            float firstOpenAngl = FirstOpenAngl(newLeaf.PrntIdx); //in case there are no leaves yet

            int nhbrIdx;
            float leafArc = Trig.CalcLeafArcSpan(prntNode.Rad, newLeaf.Rad);
            if (newLeaf.NhbrIdxs.Count == 1) { nhbrIdx = newLeaf.NhbrIdxs[0]; }
            else { nhbrIdx = newLeaf.ClosestNhbrIdx(); }    //or find worst conflict

            SpiralNode nhbrNode = Nodes[nhbrIdx];
            if (Configs.LOG)
            {
                string confLog = "conflicting neighbors:\n";
                confLog += newLeaf.PrintNodeNhbrData();
                confLog += $"adjust to neighbor idx: {nhbrIdx}\n";
                AddToLog(confLog);
            }

            float fitArc = Trig.ClosestFitAngle(prntNode, newLeaf, nhbrNode);
            if (fitArc == -100f) { return -100f; }
            else
            {
                bool inRange;

                float initStart = Trig.ComplementAngle(newLeaf.StrtAngl);
                float prntToNhbrAngl = Trig.AngleToPoint(prntNode.Ctr, nhbrNode.Ctr);
                if (aftrRslt)
                {
                    //first try backing up the leaf rather than moving it forward as usual
                    float angl0 = Trig.Mod2PI(prntToNhbrAngl + (prntNode.Clock * fitArc));
                    inRange = prntNode.AngleInGrowthRange(firstOpenAngl, angl0);
                    if (inRange)
                    {
                        string bkpLg = $" current start (parent-to-leaf angle):{initStart}, " +
                            $"parent-to-neighbor angle: {prntToNhbrAngl};  attempt backing up to start angle: {angl0:F3}\n";
                        if (Configs.LOG) { AddToLog(bkpLg); }
                        return angl0;
                    }
                }
                //proceed as usual
                float angl1 = Trig.Mod2PI(prntToNhbrAngl - (prntNode.Clock * fitArc));
                float remainingArc = Trig.Mod2PI(prntNode.Clock * (fitArc - prntNode.EndAngl));
                inRange = prntNode.AngleInGrowthRange(firstOpenAngl, angl1);

                if (inRange && remainingArc >= 0.5 * leafArc)
                {
                    if (Configs.LOG) { AddToLog($"attempt new start angle: {angl1:F3}\n"); }
                    return angl1;
                }
            }
            return -100;
        }


        public bool TryAdjust(ref SpiralNode newLeaf, SpiralNode prntNode, bool canBackUp, bool limited = false)
        {
            bool adjustSuccess;
            //first see if there's an edge problem
            if (newLeaf.EdgeDists.Contains(0) && newLeaf.NhbrIdxs.Count == 0)
            {
                adjustSuccess = TryAdjustToEdges(ref newLeaf, prntNode, limited);
                if (adjustSuccess) { return true; }
                if (!adjustSuccess && newLeaf.EdgeDists.Contains(0)) { return false; }
                //else: still doesn't fit, but edge problems solved - neighbors
                //may be pacified by the next block
            }

            if (newLeaf.NhbrIdxs.Count > 0)
            {
                adjustSuccess = TryAdjustToNhbrs(ref newLeaf, prntNode, canBackUp, limited);
                if (adjustSuccess) { return true; }
            }
            return false;
        }


        // make a copy, try adjusting copy - if doesn't work, discard copy, 
        // make a fresh copy, reslot copy, if still problems, make second copy - adjust second copy (of reslotted copy), if doesn't work, discard second copy, 
        // reslot first copy,  if still problems, make second copy  - adjust second copy, if doesn't work, discard
        public bool TryToFit(ref SpiralNode newLeaf, SpiralNode prntNode, bool noRslt = false)
        {
            //try going up a slot (at a new slot angle) a few times to see if it fixes the problem
            SpiralNode copy1 = new(newLeaf);
            (int slt, float rad, float angl) oldConditions = (newLeaf.Slot, newLeaf.Rad, newLeaf.StrtAngl);
            (int slt, float rad, float angl) newConditions = (0, 0f, -100f);
            float initStartAngl = Trig.ComplementAngle(newLeaf.StrtAngl);

            //ADJUST HERE
            bool canBackUp = Configs.maxLeaves <= 3;
            bool adjustSuccess = TryAdjust(ref copy1, prntNode, canBackUp, noRslt);
            if (adjustSuccess)
            {
                newLeaf = copy1;
                return true;
            }
            else
            {
                copy1 = new SpiralNode(newLeaf);
                if (noRslt) { return false; }
            }

            float newStart = initStartAngl;
            List<int> slotLst = Configs.useSlots;
            int sltIdx = slotLst.FindIndex(x => x == copy1.Slot);
            for (int i = sltIdx + 1; i < slotLst.Count; i++)
            {
                int slt = slotLst[i];

                newStart = prntNode.ReslotAngl(slt, false);  //stops reslotAngl decrementing by one
                if (newStart == -100)   //can't reslot, bail
                {
                    newConditions = (slt, 0f, -100f);
                    LogReslot(false, 2, oldConditions, newConditions);
                    return false;
                }

                //RESLOT HERE
                if (prntNode.AngleInGrowthRange(initStartAngl, newStart))
                {
                    copy1.AdjustLeaf(prntNode, newStart, slt, _rnd);
                    if (Configs.GRAD && !Configs.SMTOLG &&
                         Configs.RadTooSmall(copy1.Rad)) { return false; }

                    bool reslotSuccess = CheckFit(copy1);

                    newConditions = (copy1.Slot, copy1.Rad, copy1.StrtAngl);
                    LogReslot(reslotSuccess, 2, oldConditions, newConditions);

                    if (reslotSuccess)
                    {
                        newLeaf = copy1;
                        return true;
                    }
                    else //opt for minor adjustments over reslotting if possible
                    {
                        SpiralNode copy2 = new(copy1);
                        //ADJUST HERE
                        adjustSuccess = TryAdjust(ref copy2, prntNode, true);
                        newConditions = (copy2.Slot, copy2.Rad, copy2.StrtAngl);
                        if (!adjustSuccess) { copy2 = new SpiralNode(copy1); }
                        else
                        {
                            newLeaf = copy2;
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        public void LogReslot(bool startFits, int rsltCause,
                                    (int slt, float rad, float angl) oldConditions,
                                    (int slt, float rad, float angl) newConditions)
        {
            if (!Configs.LOG) { return; }
            string lg = "";
            int oldSlot = oldConditions.slt;
            float oldRad = oldConditions.rad;
            float oldAngle = oldConditions.angl;

            int newSlot = newConditions.slt;
            float newRad = newConditions.rad;
            float newAngle = newConditions.angl;

            if (rsltCause == 2) //TryToFit
            {
                lg += $"Having failed to resolve conflicts by adjusting leaf angle, trying to fix " +
                      $"problem(s) by changing leaf slot from {oldSlot} to {newSlot};\n";
                if (startFits)
                {
                    lg += $"reslot effort successful, producing leaf radius {newRad:F3} at (new) angle {newAngle:F3}\n";
                }
                else
                {
                    lg += $"reslot effort failed";
                    if (newAngle != -100)
                    {
                        lg += $", producing leaf radius {newRad:F3} at (new) angle {newAngle:F3}";
                    }
                    lg += ".\n";
                }
            }
            if (rsltCause == 3) //attempt to increase leafRad to sufficient size for growing
            {
                lg += $"Leaf radius {oldRad:F3} too small; changed leaf slot " +
                    $"from {oldSlot} to {newSlot} to get radius={newRad:F3} " +
                    $"(start angle also changed from {oldAngle:F3} to {newAngle:F3})\n";
            }
            AddToLog(lg);
        }


        public bool GrowLeaf(SpiralNode prntNode, int prntIdx, float leafRad, float startAngle, int leafSlot, bool noRslt = false)
        {
            //if leaves get progressively smaller and the current leaf is already too small, don't bother growing any
            if (!Configs.BigEnoughToGrow(leafRad))
            {
                prntNode.Full = true;
                return false;
            }

            float lift = prntNode.CalcNodeLift(leafRad, startAngle);
            float leafCtrX = prntNode.Ctr.X + ((float)Math.Cos(startAngle) * (prntNode.Rad + leafRad + lift));
            float leafCtrY = prntNode.Ctr.Y + ((float)Math.Sin(startAngle) * (prntNode.Rad + leafRad + lift));
            SpiralNode newLeaf = new(prntIdx, prntNode.Clock, leafCtrX, leafCtrY,
                                        leafRad, Trig.ComplementAngle(startAngle), leafSlot, lift);

            //inherit parent's edge data                    
            newLeaf.EdgeDists.AddRange(prntNode.EdgeDists);
            newLeaf.EdgePoints.AddRange(prntNode.EdgePoints);

            bool startFits = CheckFit(newLeaf);
            if (!startFits) //result 1 - failed, retrying
            {
                LogLeaf(newLeaf, prntNode, 1, noRslt);
                startFits = noRslt ? TryToFit(ref newLeaf, prntNode, true) : TryToFit(ref newLeaf, prntNode);
            }

            if (startFits && !Configs.RadTooSmall(newLeaf.Rad))  //result 2, worked - either immediately, or after retry
            {
                Nodes.Add(newLeaf);
                int lastIdx = Nodes.Count - 1;
                prntNode.MarkParent(newLeaf, lastIdx);

                LogLeaf(newLeaf, prntNode, 2, noRslt);
                return true;
            }
            else //result 3, failed after retry
            {
                LogLeaf(newLeaf, prntNode, 3, noRslt);
                return false;
            }
        }


        public void LogLeaf(SpiralNode newLeaf, SpiralNode prntNode, int result, bool resprouted = false)
        {
            if (!Configs.LOG) { return; }
            string lfLog = "";
            string mod = resprouted ? "resprouted " : "";
            if (result == 1)
            {
                lfLog += $"\ngrowLeaf had a problem fitting {mod}leaf idx {Nodes.Count} (rad={newLeaf.Rad:F3}" +
                        $", slot={newLeaf.Slot}) at angle: {Trig.ComplementAngle(newLeaf.StrtAngl):F3} " +
                        $"on parent idx {newLeaf.PrntIdx}, (clock: {prntNode.Clock}, strtAngl: {prntNode.StrtAngl:F3})\n";
                if (newLeaf.NhbrIdxs.Count > 0)
                {
                    lfLog += "found neighbor conflicts:\n";
                    lfLog += newLeaf.PrintNodeNhbrData();
                }
                if (newLeaf.EdgeDists.Contains(0))
                {
                    lfLog += "found edge conflicts: ";
                    lfLog += newLeaf.PrintNodeEdgeData(true);
                }
            }
            if (result == 2)
            {
                float prntToLeafAngl = Trig.AngleToPoint(newLeaf.Ctr, prntNode.Ctr);
                lfLog += $"\ngrowLeaf added {mod}leaf idx {Nodes.Count - 1} (rad={newLeaf.Rad:F3}, " +
                        $"slot={newLeaf.Slot}) to parent idx {newLeaf.PrntIdx} (clock: {prntNode.Clock}, " +
                        $"strtAngl: {prntNode.StrtAngl:F3}, rad={prntNode.Rad:F3}) " +
                        $"at angle: {Trig.ComplementAngle(newLeaf.StrtAngl):F3} " +
                        $"angleToPoint(lf,prnt): {prntToLeafAngl:F3}\n";
            }
            if (result == 3)
            {
                lfLog += $"\ngrowLeaf gave up on adding {mod}leaf idx {Nodes.Count}, " +
                        $"slot={newLeaf.Slot}, to parent idx {newLeaf.PrntIdx}\n";
            }
            AddToLog(lfLog);
        }


        public bool TryPromotingTinyLeaf(SpiralNode current, ref int leafSlot, ref float leafRad, ref float leafAngl)
        {
            (int slt, float rad, float angl) oldConditions = (leafSlot, leafRad, leafAngl);

            while (Configs.RadTooSmall(leafRad) && leafSlot > 0)
            {
                leafSlot--;
                leafRad = Configs.NextLeafRad(current.Rad, leafSlot, _rnd);
            }

            if (!Configs.RadTooSmall(leafRad))
            {
                leafAngl = current.ReslotAngl(leafSlot);
                (int slt, float rad, float angl) newConditions = (leafSlot, leafRad, leafAngl);
                if (Configs.LOG) { LogReslot(true, 3, oldConditions, newConditions); }
                return true;
            }
            //else 
            return false;
        }



        public void SproutStumps()
        {
            int nodesLeft = Configs.maxNodes - Nodes.Count;
            if (nodesLeft == 0) { return; } //nothing left to grow

            List<(int idx, float rad)> stumpList = new();
            int targetSlot = 0;
            int minSlot = Configs.useSlots.Min();
            int maxSlot = Configs.useSlots.Max();
            targetSlot = minSlot;
            int growSlot = targetSlot;

            IEnumerable<(int i, float rad, int slot, bool tooSmall, bool hasLeaves, bool blocked)> nodeAttributes =
                Nodes.Select((x, i) => (i, x.Rad, x.Slot, x.TooSmall, x.HasLeaves, x.Blocked));
            List<(int idx, float rad, int slt, bool sm, bool hl, bool blkd)> attrList = nodeAttributes.ToList();

            IEnumerable<(int idx, float rad, int slt, bool sm, bool hl, bool blkd)> goodStumps =
                attrList.Where(x => x.sm && !x.hl && !x.blkd);

            IEnumerable<(int idx, float rad, int slt)> goodStumpLst = goodStumps.Select(x => (x.idx, x.rad, x.slt));

            for (int loop = minSlot; loop <= maxSlot; loop++)
            {
                IEnumerable<(int idx, float rad, int slt)> slotFltr = goodStumpLst.Where(x => x.slt == targetSlot);
                IEnumerable<(int idx, float rad)> frmtFltr = slotFltr.Select(x => (x.idx, x.rad));
                stumpList = frmtFltr.ToList();
               
                targetSlot++;
            
                //sort in descending order
                stumpList.Sort((a, b) => a.rad > b.rad ? -1 : a.rad < b.rad ? 1 : 0);
                foreach ((int idx, float rad) in stumpList)
                {
                    float mult = Configs.childProportion;
                    if (rad < Configs.minRadius / 2) { mult += 0.05f; }
                    float lfRad = mult * (rad * Configs.initRadius) / Configs.minRadius;
                    SpiralNode stump = Nodes[idx];

                    float strtAngl = stump.ToAbsoluteAngle(Configs.sproutAngle + (0.1f * growSlot));
                    bool success = GrowLeaf(stump, idx, lfRad, strtAngl, growSlot, true);
                    if (success)
                    {
                        nodesLeft--;
                        if (nodesLeft == 0) { return; }

                        int leafIdx = Nodes.Count - 1;
                        if (Nodes[leafIdx].CanGrow) { Develop(leafIdx, true); }
                        else { continue; }

                        nodesLeft = Configs.maxNodes - Nodes.Count;
                        if (nodesLeft == 0) { return; }
                    }
                    else { stump.Blocked = true; }
                }
            }
        }


        public void Develop(int idx, bool resprouted = false)
        {
            int maxNumLeaves = Configs.maxLeaves;
            int nodesLeft = Configs.maxNodes - Nodes.Count;
            SpiralNode current = Nodes[idx];

            if (Configs.RANDNUM) { maxNumLeaves = Configs.NumLeaves(_rnd); }
            float baseAngl = FirstOpenAngl(idx);    //returns ABSOLUTE angle
            int leafSlot = current.NextLeafSlot(baseAngl, maxNumLeaves);
            float leafRad = Configs.NextLeafRad(current.Rad, leafSlot, _rnd);
            float leafArc = Trig.CalcLeafArcSpan(current.Rad, leafRad); //minimum arc necessary to fit leaf
            float leafAngl = current.NextLeafAngle(leafArc, baseAngl, leafSlot);    //returns absolute angle

            for (int lf = 0; lf < maxNumLeaves; lf++)
            {   //bad start angle, can't grow a new leaf here
                if (leafAngl != -100)
                {
                    bool success;
                    if (Configs.RadTooSmall(leafRad))
                    {
                        if (Configs.GRAD && Configs.SMTOLG) //leaves get larger from here
                        {
                            success = TryPromotingTinyLeaf(current, ref leafSlot, ref leafRad, ref leafAngl);
                            if (!success) { current.Full = true; break; }
                        }
                        //leaves get smaller from here, and this one is already too small
                        else { current.Full = true; break; }
                    }

                    //try to grow new leaf
                    success = GrowLeaf(current, idx, leafRad, leafAngl, leafSlot);
                    if (!success) { current.Blocked = true && !resprouted; break; }

                    nodesLeft--;
                    if (nodesLeft == 0) { return; }

                    baseAngl = FirstOpenAngl(idx);  //returns absolute angle
                    //leafSlot = current.NextLeafSlot(baseAngl, maxNumLeaves);  //use overload to test slot allocation
                    //if (leafSlot == -1) { break; }    //use with last line to test slot allocation
                    if (current.HasLeaves && Nodes[^1].Slot == 0) { current.Full = true; return; }
                    leafSlot = Nodes[^1].Slot; //get actual value in case adjusted
                    leafSlot = current.NextLeafSlot(leafSlot, maxNumLeaves); //simple decrementing overload

                    leafRad = Configs.NextLeafRad(current.Rad, leafSlot, _rnd);
                    leafArc = Trig.CalcLeafArcSpan(current.Rad, leafRad); //minimum arc necessary to fit leaf
                    if (!current.HasRoomFor(leafArc * 1.75f)) { current.Full = true; break; }
                    leafAngl = current.NextLeafAngle(leafArc, baseAngl, leafSlot);
                }
                else  //NextLeafAngle returned -100, meaning input combination was invalid
                {
                    leafSlot = current.NextLeafSlot(leafSlot, maxNumLeaves); //simple decrementing overload
                    leafRad = Configs.NextLeafRad(current.Rad, leafSlot, _rnd);
                    leafArc = Trig.CalcLeafArcSpan(current.Rad, leafRad); //minimum arc necessary to fit leaf
                    if (!current.HasRoomFor(leafArc * 1.75f)) { current.Full = true; break; }
                    leafAngl = current.NextLeafAngle(leafArc, baseAngl, leafSlot);
                }
                if (current.HasLeaves && current.LeafIdxs.Count == maxNumLeaves) { current.Full = true; return; }
            }
            if (current.HasLeaves) { current.Full = true; }
        }


        public void Grow()
        {
            CreateLog();
            int loopCount = 0;
            int sterileCount = 0;
            int nodesLeft = Configs.maxNodes - Nodes.Count;
            int prevNodeCount = 0;

            if (Configs.LOG)
            { foreach (SpiralNode rootNode in Nodes) { AddToLog(rootNode.PrintNode(new char[] { 'B', 'E' })); } }

            while (nodesLeft > 0 && loopCount < 100)
            {
                loopCount++;
                if (Configs.LOG)
                { AddToLog($"\nTree loop #{loopCount} full nodes: {sterileCount} tree length: {Nodes.Count}\n"); }

                if (sterileCount > 0 && sterileCount >= Nodes.Count - 1 && Configs.GROWTINY)
                {
                    SproutStumps();
                    if (nodesLeft == Configs.maxNodes - Nodes.Count) { break; }
                    else { nodesLeft = Configs.maxNodes - Nodes.Count; }
                   
                    if (nodesLeft == 0) { break; }
                }
                sterileCount = 0;

                if (prevNodeCount == Nodes.Count)
                {
                    if (Configs.GROWTINY)
                    {
                        if (Nodes.Count(x => x.TooSmall && !x.HasLeaves && !x.Blocked) == 0) { break; }
                    }
                    else { break; }
                }
                else { prevNodeCount = Nodes.Count; }

                for (int idx = 0; idx < Nodes.Count; idx++)
                {
                    //reset to number of remaining nodes after subtracting current tree length
                    nodesLeft = Configs.maxNodes - Nodes.Count;
                    SpiralNode current = Nodes[idx];

                    if (current.CanGrow)
                    {
                        Develop(idx);
                        nodesLeft = Configs.maxNodes - Nodes.Count;
                        if (nodesLeft == 0) { WriteLog(); return; }
                    }
                    else { sterileCount++; }
                }
                nodesLeft = Configs.maxNodes - Nodes.Count;
            }

            WriteLog();
            return;
        }
    }
}