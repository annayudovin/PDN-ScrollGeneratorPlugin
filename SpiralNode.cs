using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ScrollGeneratorEffect
{
    public class SpiralNode
    {
        public int PrntIdx { get; }
        public PointF Ctr { get; set; }
        public float Rad { get; set; }
        public float StrtAngl { get; set; }
        public float EndAngl { get; set; }
        public int Clock { get; }
        public bool Full { get; set; }
        public bool TooSmall => Math.Round(Rad) <= Configs.minRadius;
        public bool Blocked { get; set; }
        public bool CanGrow => !Full && !Blocked && !TooSmall;
        public bool HasLeaves => LeafCount() > 0;
        public bool IsTwin { get; }
        public bool IsRoot => PrntIdx == -1;
        public PointF SprlCtr { get; set; }
        public PointF EnvelopeCtr { get; set; }
        public PointF StrtPt { get; set; }
        public int Slot { get; set; }
        public float Lift { get; set; }
        public List<int> LeafIdxs { get; set; } = new List<int>();
        public List<float> LeafAngls { get; set; } = new List<float>();
        public List<int> NhbrIdxs { get; set; } = new List<int>();
        public List<float> NhbrDists { get; set; } = new List<float>();

        //public List<int> TmpNhbrIdxs { get; set; } = new List<int>();     //for testing fit check only
        //public List<float> TmpNhbrDists { get; set; } = new List<float>();    //for testing fit check only
        public List<float> EdgeDists { get; set; } = new List<float>();
        public List<PointF> EdgePoints { get; set; } = new List<PointF>();

        public int LeafCount()
        {
            return LeafAngls.Count;
        }

        public int LastLeafIdx()
        {
            return LeafIdxs.Count > 0 ? LeafIdxs[^1] : -1;
        }


        public float LastLeafAngle()    //returns absolute angle
        {   //return first open instead of "invalid"?
            return LeafAngls.Count > 0 ? LeafAngls[^1] : ToAbsRemainingAngle(Configs.startAt);
        }


        public int ClosestNhbrIdx()
        {
            if (NhbrDists.Count == 0) { return -1; }
            float minNhbrDist = NhbrDists.Min();
            int lowestDistIdx = NhbrDists.FindIndex(x => x == minNhbrDist);
            return NhbrIdxs[lowestDistIdx];
        }


        //for root node(s) only
        public SpiralNode(PointF ctr, float radius, float angle, bool isTwin = false)
        {
            PrntIdx = -1;
            Ctr = ctr;
            Rad = radius;
            StrtAngl = angle;
            Clock = 1;
            Slot = 0;
            Lift = Configs.rootLift;
            Full = false;
            Blocked = false;
            IsTwin = isTwin;
            EndAngl = Trig.Mod2PI(StrtAngl + (Clock * Configs.endAt));
            SprlCtr = InitSprlCtr();
            EnvelopeCtr = InitEnvelopeCtr();
            StrtPt = InitRootStrtPt();
        }


        public SpiralNode(int prntIdx, int prntClock, float ctrX, float ctrY, float radius, float angle, int slot = 0, float lift = 0)
        {
            PrntIdx = prntIdx;
            Ctr = new PointF(ctrX, ctrY);
            Rad = radius;
            StrtAngl = angle;
            Clock = prntIdx == -1 ? 1 : -1 * prntClock;
            Slot = prntIdx == -1 ? 0 : slot;
            Lift = prntIdx == -1 ? Configs.rootLift : lift;
            Full = false;
            Blocked = false;
            IsTwin = false;
            EndAngl = Trig.Mod2PI(StrtAngl + (Clock * Configs.endAt));
            SprlCtr = InitSprlCtr();
            EnvelopeCtr = InitEnvelopeCtr();
            if (prntIdx == -1) { StrtPt = InitRootStrtPt(); }
        }


        public SpiralNode(SpiralNode orig) //creates a copy of the original
        {
            PrntIdx = orig.PrntIdx;
            Ctr = new PointF(orig.Ctr.X, orig.Ctr.Y);
            Rad = orig.Rad;
            StrtAngl = orig.StrtAngl;
            Clock = orig.Clock;
            Slot = orig.Slot;
            Lift = orig.Lift;
            Full = false;
            Blocked = false;
            IsTwin = false;
            EndAngl = orig.EndAngl;
            SprlCtr = new PointF(orig.SprlCtr.X, orig.SprlCtr.Y);
            StrtPt = new PointF(orig.StrtPt.X, orig.StrtPt.Y);
            //no leaves yet, skip

            NhbrIdxs = new List<int>();
            NhbrDists = new List<float>();
            NhbrIdxs.AddRange(orig.NhbrIdxs);
            NhbrDists.AddRange(orig.NhbrDists);

            EdgeDists = new List<float>();
            EdgePoints = new List<PointF>();
            EdgeDists.AddRange(orig.EdgeDists);
            EdgePoints.AddRange(orig.EdgePoints);
        }


        public string PrintNode(char[] flags)
        {
            string nodePrint = "";

            if (flags.Contains('B'))
            {
                nodePrint += $"NODE parent idx: {PrntIdx}; ctr: ({Ctr.X:F3}, {Ctr.Y:F3}); " +
                        $"rad: {Rad:F3}; start angle: {StrtAngl:F3}; end angle: {EndAngl:F3}; " +
                        $"slot: {Slot}; clock: {Clock}\n";
            }
            if (flags.Contains('E')) { nodePrint += PrintNodeEdgeData(); }
            if (flags.Contains('L')) { nodePrint += PrintNodeLeafData(); }
            if (flags.Contains('N')) { nodePrint += PrintNodeNhbrData(); }

            return nodePrint;
        }


        public string PrintNodeEdgeData(bool conflictOnly = false)
        {
            string nodePrint = "";

            for (int idx = 0; idx < EdgeDists.Count; idx++)
            {
                float edgeDist = EdgeDists[idx];
                if (conflictOnly)
                {
                    if (EdgeDists[idx] != 0) { continue; }
                    else { edgeDist = Trig.PointDist(Ctr, EdgePoints[idx]); }
                }
                nodePrint += $"edge idx#{idx} distance: {edgeDist}; point: {EdgePoints[idx]:F3}\n";
            }

            return nodePrint;
        }


        public string PrintNodeLeafData()
        {
            string nodePrint = "";

            for (int idx = 0; idx < LeafCount(); idx++)
            {
                nodePrint += $"leaf idx: {LeafIdxs[idx]}, angle: {LeafAngls[idx]:F3}\n";
            }

            return nodePrint;
        }


        public string PrintNodeNhbrData()
        {
            string nodePrint = "";

            for (int idx = 0; idx < NhbrIdxs.Count; idx++)
            {
                nodePrint += $"neighbor idx: {NhbrIdxs[idx]}, surface dist:  {NhbrDists[idx]:F3}\n";
            }

            return nodePrint;
        }


        public PointF InitEnvelopeCtr()
        {
            float shiftAng = Trig.Mod2PI(StrtAngl + Clock * 2.75f);
            float shiftX = Ctr.X + (float)Math.Cos(shiftAng) * (float)(Math.PI / 12f) * Rad;
            float shiftY = Ctr.Y + (float)Math.Sin(shiftAng) * (float)(Math.PI / 12f) * Rad;

            return new PointF(shiftX, shiftY);
        }


        public PointF InitSprlCtr()
        {
            float shiftAng = Clock * 3 * (float)Math.PI / 4;
            float newAng = Trig.Mod2PI(StrtAngl + shiftAng);

            float shiftX = Ctr.X + (float)Math.Cos(newAng) * (Rad / (float)Math.Tau);
            float shiftY = Ctr.Y + (float)Math.Sin(newAng) * (Rad / (float)Math.Tau);

            return new PointF(shiftX, shiftY);
        }


        public PointF InitRootStrtPt()
        {
            float scl = 0.099f;
            float angl = (2 * (float)Math.Tau) - 0.1f;
            float sclRad = Rad * scl;

            float x = SprlCtr.X + (sclRad * angl * (float)Math.Cos(angl + StrtAngl));
            float y = SprlCtr.Y + (sclRad * angl * (float)Math.Sin(angl + StrtAngl));

            return new PointF(x, y);
        }

        private void Recenter(PointF prntCtr, float prntRad, float newAngleFromPrnt = -100f, float newRad = 0f)
        {
            if (newRad > 0)
            {
                Rad = newRad;
                Full = false;
                Blocked = false;
            }
            if (newAngleFromPrnt != -100)
            {
                StrtAngl = Trig.ComplementAngle(newAngleFromPrnt);
                EndAngl = Trig.Mod2PI(StrtAngl + (Clock * Configs.endAt));
            }

            float ctrX = prntCtr.X + (float)Math.Cos(newAngleFromPrnt) * (prntRad + Rad + Lift);
            float ctrY = prntCtr.Y + (float)Math.Sin(newAngleFromPrnt) * (prntRad + Rad + Lift);
            Ctr = new PointF(ctrX, ctrY);

            SprlCtr = InitSprlCtr();
            EnvelopeCtr = InitEnvelopeCtr();
        }


        //from experimental observation: spiral drawing has slight discrepancy from leaf angle
        //discrepancy at min at maxTheta-5*Math.PI/4 and maxTheta-Math.PI/4
        //discrepancy increases from maxTheta-5*Math.PI/4 to maxTheta-3*Math.PI/4 - peak at maxTheta-3*Math.PI/4?
        //discrepancy decreases between maxTheta-3*Math.PI/4 and maxTheta-Math.PI/4
        //discrepancy in opposite direction slowly increases from maxTheta-5*Math.PI/4 to maxTheta-3*Math.PI/2
        //and more steeply increases from there toward center (maxTheta-2*Math.PI)
        //maxTheta=4*Math.PI
        public void CreateLeafPt(SpiralNode leafNode)
        {
            float maxSprlAngl = 2f * (float)Math.Tau;
            float scl = 0.099f * Rad;

            float anglToLeaf = Trig.ComplementAngle(leafNode.StrtAngl);
            float angl = maxSprlAngl - Trig.Mod2PI(-1 * ToRelativeAngle(anglToLeaf)); //clearer derivation
            float angleRatio = 0f;
            float baseDistRate = 0.2f;

            float prntInitRatio = Rad / Configs.initRadius;    //large when parent is close to or larger than initRadius
            float leafInitRatio = leafNode.Rad / Configs.initRadius;   //large when leafNode is close to or larger than initRadius
            float initLeafRatio = Configs.initRadius / leafNode.Rad;   //large when leafNode is small, inverse of leafInitRatio

            //adjustment should be larger for very large leaves, esp. of very large parents
            float radRatio = Math.Max(leafInitRatio, prntInitRatio);
            if (radRatio < 1) { radRatio = (float)Math.Max(Math.Min(leafInitRatio, prntInitRatio) + 0.65f, 1.2f); }

            if (maxSprlAngl - (5 * (float)Math.PI / 4) <= angl && angl <= maxSprlAngl - ((float)Math.PI / 4)) //two mins
            {
                float peakDelta = Math.Min(angl - (maxSprlAngl - (5 * (float)Math.PI / 4)), maxSprlAngl - ((float)Math.PI / 4) - angl);
                angleRatio = 0.1f + (peakDelta / ((float)Math.PI / 2f)); //smaller when closer to a min
            }
            else
            {
                if (angl < maxSprlAngl - (5 * (float)Math.PI / 4))
                {
                    baseDistRate = angl < maxSprlAngl - (3 * (float)Math.PI / 2) ? 0.02f : 0.01f;
                    angleRatio = -1 * (maxSprlAngl - (5 * (float)Math.PI / 4) - angl) / ((float)Math.PI / 2);
                    radRatio = initLeafRatio;
                }
                //few leaves can fit closer than Math.PI/4, assume no distortion after this min
            }

            float adj = angleRatio * baseDistRate * radRatio;
            float x = SprlCtr.X + (scl * (angl + adj) * (float)Math.Cos((Clock * angl) + (Clock * adj) + StrtAngl));
            float y = SprlCtr.Y + (scl * (angl + adj) * (float)Math.Sin((Clock * angl) + (Clock * adj) + StrtAngl));

            leafNode.StrtPt = new PointF(x, y);
        }


        //flips all angles pertaining to negative clock nodes to be like positive
        //when need to preserve directionality, use: Trig.Mod2PI(testAngl - StrtAngl);
        public float ToRelativeAngle(float angle)
        {
            float relativeAngl = Trig.Mod2PI((Clock * angle) - (Clock * StrtAngl));
            return relativeAngl;
        }


        public float ToAbsRemainingAngle(float angle)
        {
            float absoluteAngl = Trig.Mod2PI(StrtAngl - (Clock * angle));
            return absoluteAngl;
        }


        public float ToAbsoluteAngle(float angle)
        {
            float absoluteAngl = Trig.Mod2PI(StrtAngl + (Clock * angle));
            return absoluteAngl;
        }


        public float BaseAngle(float prntRad)   //returns absolute angle
        {
            if (!IsRoot)
            {
                float parentArc = (float)Math.Asin(prntRad / (prntRad + Rad)); //half of parent projection onto node
                return ToAbsRemainingAngle(parentArc);
            }
            else { return -100; }
        }


        public float RootBaseAngle(int rootIdx, float rootDist)     //returns absolute angle
        {
            float rootBase = Configs.TWIN ? Configs.RootBaseArc(rootIdx, rootDist) : Configs.startAt;
            return ToAbsRemainingAngle(rootBase);
        }


        public float RemainingArcAfter(float Angl)
        {
            float remainingArc = ToRelativeAngle(Angl) - Configs.endAt;
            return remainingArc;
        }


        public bool HasRoomFor(float leafArc)
        {
            if (!CanGrow) { return false; }
            if (LeafCount() == 0) { return true; }

            float remainingArc = RemainingArcAfter(LastLeafAngle());
            return remainingArc >= leafArc;
        }


        //tiny leaves grown on the portion of the spiral curling below outer circle radius
        //need double padding, or they look too crowded
        public bool DoubleTheGap()
        {
            float remainingArc = RemainingArcAfter(LastLeafAngle());
            return remainingArc <= (2f / 7f) * (float)Math.PI;
        }


        //outlines the "outer envelope" of the spiral for closer fit (as opposed to using the node radius)
        //all defined ranges and approximation functions experimentally determined
        //EnvelopeCtr property exists specifically to provide a pre-computed center for one of the ranges
        public float RadAtAngle(float approachAngl)
        {
            //range (at the top of the spiral) where it "bulges out" and the outer
            //radius creates a bump on top of the base fitting circle
            float[] liftUseRange = new float[2] { 0.65f * (float)Math.PI, 1.3f * (float)Math.PI };
            if (AngleInArc(approachAngl, liftUseRange[0], liftUseRange[1]))
            {
                float hiX = Ctr.X - (float)Math.Cos(StrtAngl) * 0.25f * Rad;
                float hiY = Ctr.Y - (float)Math.Sin(StrtAngl) * 0.25f * Rad;
                float _x = hiX + (float)Math.Cos(approachAngl) * 0.83f * Rad;
                float _y = hiY + (float)Math.Sin(approachAngl) * 0.83f * Rad;
                float atAngl = Trig.PointDist(Ctr.X, Ctr.Y, _x, _y);
                return atAngl;
            }

            bool singleCurlRoot = !Configs.TWIN && PrntIdx == -1;
            if (!singleCurlRoot)
            {
                //range where the spiral "curls in" and outer radius grows
                //much smaller than the base circle used by BFSTree to pack leaves
                float[] closerFitRange = new float[2] { 0.001f, 0.651f * (float)Math.PI };
                if (AngleInArc(approachAngl, closerFitRange[0], closerFitRange[1]))
                {
                    float _x = EnvelopeCtr.X + (float)Math.Cos(approachAngl) * (0.75f * Rad);
                    float _y = EnvelopeCtr.Y + (float)Math.Sin(approachAngl) * (0.75f * Rad);
                    return Trig.PointDist(Ctr.X, Ctr.Y, _x, _y);
                }
            }
            //range (at the base of the spiral, opposite closerFitRange) where it
            //again grows beyond the bounds of the fitting cirle and terminates in a
            //"stalk" attaching it to parent
            float[] spiralStalkRange = new float[2] { 1.75f * (float)Math.PI, (float)Math.Tau - 0.0001f };
            if (AngleInArc(approachAngl, spiralStalkRange[0], spiralStalkRange[1]))
            {
                float _x = SprlCtr.X + (float)Math.Cos(approachAngl) * (1.175f * Rad);
                float _y = SprlCtr.Y + (float)Math.Sin(approachAngl) * (1.175f * Rad);
                return (float)Math.Ceiling(Trig.PointDist(Ctr.X, Ctr.Y, _x, _y));
            }
            return (float)Math.Ceiling(Rad);
        }


        public static float GetSkinDistToPt(SpiralNode newLeaf, PointF edgePt)
        {
            float distBetween = Trig.PointDist(newLeaf.Ctr, edgePt);
            float anglToLeaf = Trig.AngleToPoint(newLeaf.Ctr, edgePt);
            float leafSurfc = newLeaf.RadAtAngle(anglToLeaf);

            return distBetween - leafSurfc;
        }

        public static float GetSurfaceDist(SpiralNode newLeaf, SpiralNode idxNode)
        {
            float distBetween = Trig.PointDist(newLeaf.Ctr, idxNode.Ctr);
            float anglToLeaf = Trig.AngleToPoint(newLeaf.Ctr, idxNode.Ctr);
            float anglToNode = Trig.ComplementAngle(anglToLeaf);
            float leafSurfc = newLeaf.RadAtAngle(anglToLeaf);
            float nodeSurfc = idxNode.RadAtAngle(anglToNode);

            return distBetween - (nodeSurfc + leafSurfc);
        }


        //parent node has a sibling right next to it, restricting leaf growth
        public int ObstructingSibIdx(int idx)
        {
            int lastSibIdx = idx + 1;
            return LeafIdxs.Count > 0 &&
                LeafIdxs.Contains(idx) &&
                LeafIdxs.Contains(lastSibIdx)
                ? lastSibIdx
                : -1;
        }


        public void MarkParent(SpiralNode newLeaf, int leafIdx)
        {
            float leafAngl = Trig.ComplementAngle(newLeaf.StrtAngl);

            LeafIdxs.Add(leafIdx);
            LeafAngls.Add(leafAngl);
            CreateLeafPt(newLeaf);  //node is attached, good time to do this
        }


        public float CalcNodeLift(float leafRad, float anglToLeaf)
        {
            float scl = 0.099f;
            float sclRad = Rad * scl;
            float maxSprlAngl = 2f * (float)Math.Tau;
            int prntClock = Clock;

            float angl = maxSprlAngl - Trig.Mod2PI(-1 * ToRelativeAngle(anglToLeaf));

            float mult = angl > maxSprlAngl - (Math.PI / 3) ? 0.05f : 0.05f * Math.Max((angl / ((float)Math.PI / 2)) - 2f, 0f);
            float radRatio = Math.Max((Rad / Configs.initRadius) - (leafRad / Rad), Configs.initRadius / Rad);
            float adj = prntClock * mult * radRatio * angl / maxSprlAngl;

            float x = SprlCtr.X + (sclRad * (angl + adj) * (float)Math.Cos((prntClock * angl) + (prntClock * adj) + StrtAngl));
            float y = SprlCtr.Y + (sclRad * (angl + adj) * (float)Math.Sin((prntClock * angl) + (prntClock * adj) + StrtAngl));

            float prntLift = Trig.PointDist(Ctr.X, Ctr.Y, x, y) - Rad;
            float leafLift = leafRad * 0.123f;
            if (Rad < Configs.minRadius) { leafLift *= 1.1f; }

            return prntLift + leafLift;
        }


        public bool AngleInSproutRange(float testAngl)
        {
            float scanFrom = Configs.sproutMaxAngle;
            float scanTo = Configs.sproutAngle;
            return AngleInArc(testAngl, scanFrom, scanTo);
        }


        public bool AngleInSlotRange(float testAngl, int slot)      //testAngle is absolute
        {
            float scanFrom = Configs.endAt;  //when slot==0
            if (slot > 0) { scanFrom = Configs.slotAngles[slot - 1]; }

            float scanTo = Configs.slotAngles[slot] + (0.5f * Configs.slotSizes[slot]);
            if (Configs.shiftFactor != 0)
            {
                scanFrom = Trig.Mod2PI(scanFrom + Configs.shiftFactor);
                scanTo = Trig.Mod2PI(scanTo + Configs.shiftFactor);
            }
            return AngleInArc(testAngl, scanFrom, scanTo);
        }


        public bool AngleInArc(float testAngl, float scanFrom, float scanTo)
        {
            //provided range is already relative, no need to convert that
            float relTest = ToRelativeAngle(testAngl);
            return scanFrom < relTest && relTest < scanTo;
        }


        public bool AngleInGrowthRange(float scanFrom, float testAngl)
        {
            float relTest = ToRelativeAngle(testAngl);
            float startRange = Configs.endAt;
            float endRange = ToRelativeAngle(scanFrom);
            return startRange < relTest && relTest < endRange;
        }



        public float NextLeafAngle(float leafArc, float baseAngl, int inSlot = -1)  //baseAngl is absolute
        {
            if (inSlot < 0) { inSlot = 0; }
            float origBase = baseAngl;

            bool useSlotBase = Configs.maxLeaves < Configs.slotSizes.Count || Configs.RANDNUM;
            if (useSlotBase || Configs.shiftFactor != 0)
            {
                if (!AngleInSlotRange(Trig.Mod2PI(origBase), inSlot))    //faster decision
                {
                    float slotBase = Configs.slotAngles[inSlot];
                    float maxEndRange = (float)Math.Tau - Configs.minRootBase;
                    float endRange = Math.Min(ToRelativeAngle(origBase) + Math.Max(0.15f, leafArc), maxEndRange);
                    float strtRange = Configs.endAt;

                    if (Configs.shiftFactor != 0)
                    {
                        slotBase = Trig.Mod2PI(slotBase + Configs.shiftFactor);
                        if (PrntIdx == -1 && !HasLeaves) { endRange = maxEndRange; }
                    }

                    if (strtRange < slotBase && slotBase < endRange) { baseAngl = ToAbsoluteAngle(slotBase); }
                    else if (Configs.shiftFactor != 0) { return -100; }  //when shiftFactor == 0, we keep the original baseAngl
                }
            }

            if (Configs.GRAD && !Configs.SMTOLG && DoubleTheGap())
            { leafArc = (2 * Configs.nodeHalo) + leafArc; }

            if (LeafCount() == 0)
            {
                if (!IsRoot) { leafArc = Configs.nodeHalo + leafArc; }
                else if (Configs.CanDecreaseLeafArc(IsTwin)) { leafArc = 0.9f * leafArc; }
            }

            float nextLeafAngl = Trig.Mod2PI(baseAngl - (Clock * leafArc));
            return nextLeafAngl;    //returns absolute angle
        }


        public int CorrectedSlot(float atAngl, int origSlot)
        {
            if (AngleInSlotRange(atAngl, origSlot)) { return origSlot; }
            else
            {
                for (int slt = 0; slt < Configs.slotAngles.Count; slt++)
                {
                    if (AngleInSlotRange(atAngl, slt)) { return slt; }
                }
            }
            return origSlot;
        }


        public int NextLeafSlot(float atAngl = -100, int maxNumLeaves = 0)  //atAngl is absolute
        {
            if (atAngl == -100) { return Configs.slotSizes.Count - 1; }

            if (Configs.RANDNUM)
            {
                return NextSlotFromList(maxNumLeaves, Configs.slotSizes.Count);
            }

            float tstAngl;
            tstAngl = ToRelativeAngle(atAngl);
            int nextSlot = Configs.FindNextSlot(tstAngl);

            if (Configs.maxLeaves < Configs.slotSizes.Count)
            {
                int listSlot = Configs.useSlots.Max();
                if (listSlot < nextSlot) { return listSlot; }
            }

            return nextSlot;
        }


        public int NextLeafSlot(int currSlot, int maxNumLeaves = 0)
        {
            if (Configs.maxLeaves < Configs.slotSizes.Count || Configs.RANDNUM)
            {
                return NextSlotFromList(maxNumLeaves, currSlot);
            }
            //else:
            int nextSlot = 0;
            if (currSlot > 0) { nextSlot = currSlot - 1; }
            return nextSlot;
        }


        private int NextSlotFromList(int maxNumLeaves, int currSlot)
        {
            if (LeafCount() == 0 && !Configs.RANDNUM)
            {
                //it's our first time trying to get a slot for first leaf - answer is good even for RANDNUM
                if (currSlot != Configs.useSlots.Max()) { return Configs.useSlots.Max(); }
            }
            if (currSlot == 0) { return 0; } //it's not getting any smaller!
            if (maxNumLeaves == 0) { maxNumLeaves = Configs.slotSizes.Count; }

            List<int> slotLst;
            if (!Configs.RANDNUM) { slotLst = Configs.useSlots; }
            else
            {
                slotLst = Configs.GetSlotList(maxNumLeaves);
                slotLst.Reverse();
            }

            int nxtSlotIdx = slotLst.FindIndex(x => x < currSlot);
            return nxtSlotIdx == -1 ? currSlot - 1 : slotLst[nxtSlotIdx];
        }


        public float ReslotAngl(int currSlot, bool decrement = true)
        {
            int nextSlot = !decrement ? currSlot : currSlot - 1;
            if (nextSlot < 0) { return -100; }

            float slotAngl = Configs.slotAngles[nextSlot];
            if (Configs.shiftFactor != 0) { slotAngl += Configs.shiftFactor; }

            return ToAbsoluteAngle(slotAngl);
        }


        public void AdjustLeaf(SpiralNode prntNode, float tryAngleFromParent, int newSlot, Random? rnd = null)//this. is leaf
        {
            if (Configs.GRAD) //radius usually changes based on slot#
            {
                float updatedRad = Rad;
                if (Slot != newSlot)  //not trying to keep radius from changing based on slot#
                {
                    updatedRad = Configs.NextLeafRad(prntNode.Rad, newSlot, rnd!);
                    Slot = newSlot;
                }
                Lift = prntNode.CalcNodeLift(updatedRad, tryAngleFromParent);
                Recenter(prntNode.Ctr, prntNode.Rad, tryAngleFromParent, updatedRad);
            }
            else
            {   //radius stays the same, but the following angles may be affected
                if (Slot != newSlot) { Slot = newSlot; }
                Lift = prntNode.CalcNodeLift(Rad, tryAngleFromParent);
                Recenter(prntNode.Ctr, prntNode.Rad, tryAngleFromParent);
            }

            //existing values are no longer valid for new center: 
            NhbrIdxs = new List<int>();
            NhbrDists = new List<float>();
            EdgeDists = new List<float>();
            EdgePoints = new List<PointF>();
            EdgeDists.AddRange(prntNode.EdgeDists);
            EdgePoints.AddRange(prntNode.EdgePoints);
        }


        public static float SibConflictAngle(SpiralNode idxNode, SpiralNode problemSib) //returns absolute angle
        {
            float sibDist = Trig.PointDist(idxNode.Ctr, problemSib.Ctr);
            float anglToSib = Trig.AngleToPoint(idxNode.Ctr, problemSib.Ctr);

            //must account for case of big gap after last sib (like due to edge proximity)
            if (sibDist < (2.5 * idxNode.Rad) + problemSib.Rad) //sibling close enough to matter
            {
                float lastSibArc = Configs.nodeHalo + (float)Math.Asin(problemSib.Rad / sibDist); //sibling projection onto node
                float firstOpen = anglToSib - (idxNode.Clock * lastSibArc);
                return firstOpen;
            }
            else { return -100; }
        }


        public PointF[] PlotNodeEnvelopePoints()
        {
            float x;
            float y;
            float theta = 0f;
            float maxTheta = (float)Math.Tau;
            float incr = 0.1f;

            List<PointF> outerPoints = new();
            while (theta < maxTheta)
            {
                float approachAngl = ToAbsRemainingAngle(theta);
                x = Ctr.X + (float)Math.Cos(approachAngl) * (RadAtAngle(approachAngl));
                y = Ctr.Y + (float)Math.Sin(approachAngl) * (RadAtAngle(approachAngl));
                theta += incr;
                outerPoints.Add(new PointF(x, y));
            }

            return outerPoints.ToArray();
        }


        public PointF[] PlotSpiralPoints()
        {
            float x;
            float y;
            float theta = 0f;
            float maxTheta = (2f * (float)Math.Tau) - 0.1f;
            float incr = 0.1f;
            float scl = 0.099f * Rad;
            PointF anchr = StrtPt;

            List<PointF> spiralPoints = new();
            while (theta < maxTheta)
            {
                x = SprlCtr.X + (scl * theta * (float)Math.Cos((Clock * theta) + StrtAngl));
                y = SprlCtr.Y + (scl * theta * (float)Math.Sin((Clock * theta) + StrtAngl));

                theta += incr;
                spiralPoints.Add(new PointF(x, y));
            }
            spiralPoints.Add(new PointF(anchr.X, anchr.Y));
            return spiralPoints.ToArray();
        }
    }
}