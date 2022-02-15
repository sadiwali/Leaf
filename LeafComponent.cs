using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Text;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using Grasshopper.Kernel.Types;
using GH_IO.Serialization;

namespace Leaf {
    public class LeafComponent : GH_Component {

        public string[][] rules;
        public string prevIter;
        public Random rand = new Random();

        public LeafComponent() : base("Leaf System", "LS",
          "Produce a string using the L-system.",
          "Leaf", "Main") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {

            pManager.AddTextParameter("Rules", "R", "Rules to apply.", GH_ParamAccess.list, "");
            pManager.AddTextParameter("Axiom", "A", "The starting character or characters.", GH_ParamAccess.item, "");
            pManager.AddIntegerParameter("Cycle", "i", "L-System strings can get very large very quickly. Your computer may not be able to handle high cycle values. It's a good idea to use a panel instead of a slider so you don't accidentally crash Grasshopper.", GH_ParamAccess.item, 0);

            pManager[0].Optional = false;
            pManager[1].Optional = false;
            pManager[2].Optional = false;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddTextParameter("Shape Code", "S", "The computed L-system string.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            // create variables that correspond to inputs
            List<string> _rules = new List<string>();
            string axiom = "";
            int cycle = 0;
            // reference the inputs to variables
            if (!DA.GetDataList(0, _rules)) return;
            DA.GetData(1, ref axiom);
            if (!DA.GetData(2, ref cycle)) return;

            // no rules
            if (_rules == null || _rules.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No rules added.");
            }
            // no axiom
            if (axiom == null || axiom == "") {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Enter an axiom to start the L-System.");
            }
            // cycle is less than 0
            if (cycle < 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Cycle cannot be negative.");
                return;
            }

            // set the prevIter value to the axiom
            prevIter = axiom;

            // Process rules in the global array
            ProcessRules(_rules);

            string out_string = ProduceLSystem(rules, axiom, cycle);

            // assign the output
            DA.SetData(0, out_string);
        }

        void ProcessRules(List<string> r) {

            // process the list r to remove any empties
            int trueRuleListSize = 0;

            for (int i = 0; i < r.Count; i++) {
                if (r[i] != null) {
                    trueRuleListSize++;
                }
            }

            // rules array to return
            rules = new string[trueRuleListSize][];

            // go through all rules, and process them
            for (int i = 0; i < trueRuleListSize; i++) {
                string[] rule = r[i].Split('=');
                rules[i] = rule;
            }

        }

        string ProcessRule(string prevIter, int c) {
            string character = prevIter[c].ToString();
            string toRet = "";

            for (int r = 0; r < rules.Length; r++) {
                string[] rule = rules[r];
                // if character not in rule, move onto next rule
                if (!rule[0].Contains(character)) continue;
                // if rule does not have right hand side, move on
                if (rule.Length == 1) continue;

                // identify the type of rule
                if (rule[0].Length == 1) {
                    // simple replacement rule
                    toRet = rule[1];
                    break;
                } else if (rule[0].Contains("<") || rule[0].Contains(">")) {
                    // context sensitive rule

                    int indOfLeft = rule[0].IndexOf("<");
                    int indOfRight = rule[0].IndexOf(">");

                    // condition before and after symbol
                    string before = indOfLeft == -1 ? null : rule[0].Substring(0, indOfLeft);
                    string after = indOfRight == -1 ? null : rule[0].Substring(indOfRight + 1);

                    // before moving on further, check that character is truly in the context sensitive rule
                    string cont_character = rule[0][indOfLeft + 1].ToString();

                    // string too short, rule does not apply
                    if (before != null) {
                        int eval = c - before.Length;
                        if (eval < 0) {
                            continue;
                        }
                    }

                    if (after != null) {
                        if (c + after.Length >= prevIter.Length) {
                            continue;
                        }
                    }

                    // rule has both sides of context present
                    if (before != null && after != null) {
                        if (prevIter.Substring(c - before.Length, before.Length + after.Length + 1) == before + cont_character + after) {
                            toRet = rule[1];
                            break;
                        }
                    } else if (before != null) {
                        if (prevIter.Substring(c - before.Length, before.Length + 1) == before + cont_character) {
                            toRet = rule[1];
                            break;
                        }
                    } else if (after != null) {
                        if (prevIter.Substring(c, after.Length + 1) == cont_character + after) {
                            toRet = rule[1];
                            break;
                        }
                    }

                } else if (rule[0].Contains("(") || rule[0].Contains(")")) {
                    // probabilistic rule 
                    int indA = rule[0].IndexOf("(") + 1;
                    int indB = rule[0].IndexOf(")");
                    int distAB = indB - indA;
                    float probValue;
                    if (distAB == 0) {
                        probValue = 0;
                    } else {
                        probValue = float.Parse("0" + rule[0].Substring(indA, distAB));
                    }

                    double randVal = rand.NextDouble();
                    int outcome = (randVal <= probValue) ? 1 : 2;

                    if (rule.Length > 2) {
                        toRet = rule[outcome];
                        break;
                    } else {
                        toRet = (outcome == 2) ? "" : rule[1];
                        break;
                    }

                } else {
                    // no rules
                }
            }
            return toRet.Length > 0 ? toRet : character;
        }

        string ProduceLSystem(string[][] rules, string axiom, int cycle) {
            for (int i = 0; i < cycle; i++) {

                StringBuilder sB = new StringBuilder();

                for (int c = 0; c < prevIter.Length; c++) {
                    sB.Append(ProcessRule(prevIter, c));
                }

                prevIter = sB.ToString();
            }
            return prevIter;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.ico.ToBitmap();

        public override Guid ComponentGuid => new Guid("94996c1c-90b5-46da-a541-b8400c390e20");
    }

    public class InstructionsComponent : GH_Component {

        public InstructionsComponent() : base("Leaf Instructions", "LI",
          "How to use Leaf String Rewriter.",
          "Leaf", "Info") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) { }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddTextParameter("Instructions", "I", "Recognized symbols and what they do", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Version 1.1.0");
            // assign the output
            string outString = "";
            outString += "Turtle:\n";
            outString += "---------------\n";
            outString += "pitch up: ^\n";
            outString += "pitch down: /\n";
            outString += "turn left: -\n";
            outString += "turn right: +\n";
            outString += "move forward without placing block: _\n";
            outString += "all other symbols will cause the turtle to place a block and move forward 1 step.: _\n";

            outString += "\nBranching:\n";
            outString += "---------------\n";
            outString += "start branch: [\n";
            outString += "end branch: ]\n";

            outString += "\nRule only:\n";
            outString += "---------------\n";
            outString += "Context left: <\n";
            outString += "Context right: >\n";
            outString += "Stochastic left: (\n";
            outString += "Stochastic right: )\n";

            outString += "\nHow to write rules:\n";
            outString += "---------------\n";
            outString += "To write a simple rule: a=ab (not a = ab)\n";
            outString += "To write a context-sensitive rule: a<b>c=d or a<b=c b>c=d. The selected symbol is 'b'. Experiment with this rule to learn more.\n";
            outString += "To write a stochastic rule: a(0.5)=b or a(0.5)=b=c. The selected symbol is 'a'. In the first case, 50% chance a is replaced with b, or nothing. In the second case, 50% chance a is replaced with b or c.\n";
            outString += "\n";
            outString += "All other rules follow the genreal L-System format.\n";

            DA.SetData(0, outString);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.helpicon.ToBitmap();

        public override Guid ComponentGuid => new Guid("713ae280-7e52-11ec-90d6-0242ac120003");
    }

    // custom data type
    public class LeafGeometryData : Grasshopper.Kernel.Types.IGH_Goo {

        private string symbol;
        private Brep brep;
        private Point3d[] points;

        public LeafGeometryData() { }

        public LeafGeometryData(string s, Brep b, List<Point3d> p) {
            symbol = s;
            brep = b;
            points = new Point3d[p.Count];
            for (int i = 0; i < p.Count; i++) {
                points[i] = p[i];
            }
        }
        public string Symbol {
            get {
                return symbol;
            }
            set {
                symbol = value;
            }
        }

        public Brep Geometry {
            get {
                return brep;
            }
            set {
                brep = value;
            }
        }

        public Point3d BL {
            get {
                return points[0];
            }
            set {
                points[0] = value;
            }
        }

        public Point3d TL {
            get {
                return points[1];
            }
            set {
                points[1] = value;
            }
        }

        public Point3d BR {
            get {
                return points[2];
            }
            set {
                points[2] = value;
            }
        }

        public Plane OrientPlane {
            get {
                return new Plane(points[0], points[1], points[2]);
            }
        }

        public double GetVoxelSize {
            get {
                return Math.Abs(points[0].DistanceTo(points[1]));
            }
        }

        public Point3d Point {
            get {
                Vector3d vecA = this.TL - this.BL;
                vecA.Unitize();
                Vector3d vecB = this.BR - this.BL;
                vecB.Unitize();
                Vector3d vecC = Rhino.Geometry.Vector3d.CrossProduct(vecA, vecB);
                vecC.Unitize();
                vecC *= -this.GetVoxelSize / 2;
                // get a point in the center face
                Line l = new Line(this.TL, this.BR);
                Point3d p = l.PointAt(0.5);

                Transform t = Rhino.Geometry.Transform.Translation(vecC);
                p.Transform(t);
                return p;
            }
        }

        public bool IsValid {
            get {
                if (symbol == null || symbol == "") return false;
                if (!brep.IsValid) return false;
                if (points.Length != 3) return false;
                return true;
            }
        }

        public string IsValidWhyNot {
            get {
                return "Check your inputs.";
            }
        }

        public string TypeName {
            get {
                return "Leaf Data";
            }
        }

        public string TypeDescription {
            get {
                return "Some data";
            }
        }

        public bool CastFrom(object source) {
            throw new NotImplementedException();
        }

        public bool CastTo<T>(out T target) {
            throw new NotImplementedException();
        }

        public IGH_Goo Duplicate() {
            return new LeafGeometryData();
        }

        public IGH_GooProxy EmitProxy() {
            throw new NotImplementedException();
        }

        public bool Read(GH_IReader reader) {
            throw new NotImplementedException();
        }

        public object ScriptVariable() {
            throw new NotImplementedException();
        }

        public bool Write(GH_IWriter writer) {
            throw new NotImplementedException();
        }
    }

    public class TurtlePointer {

        private Point3d point, bl, tl, br;
        private double vs, dist, angle;
        public TurtlePointer(int x, int y, int z, double v, double d, double a) {
            vs = v;
            dist = d;
            point = new Point3d(x, y, z);
            angle = a;
            bl = new Point3d(x - v / 2, y + v / 2, z - v / 2);
            tl = new Point3d(x - v / 2, y + v / 2, z + v / 2);
            br = new Point3d(x + v / 2, y + v / 2, z - v / 2);
        }

        public TurtlePointer State {
            get {
                return this.MemberwiseClone() as TurtlePointer;
            }
            set {
                this.point = value.Point;
                this.bl = value.BL;
                this.tl = value.TL;
                this.br = value.BR;
            }
        }

        public Point3d BL {
            get {
                return bl;
            }
        }
        public Point3d TL {
            get {
                return tl;
            }

        }
        public Point3d BR {
            get {
                return br;
            }
        }

        public Point3d Point {
            get {
                return point;
            }
        }

        public Double Angle {
            get {
                return angle;
            }
            set {
                angle = value;
            }
        }

        public Plane OrientPlane {
            get {
                return new Plane(bl, tl, br);
            }
        }

        public double GetVoxelSize {
            get {
                return this.vs;
            }
        }

        public Point3d[] CurveEnds() {
            Vector3d vecA = tl - bl;
            vecA.Unitize();
            Vector3d vecB = br - bl;
            vecB.Unitize();
            Vector3d vecC = Rhino.Geometry.Vector3d.CrossProduct(vecA, vecB);
            vecC.Unitize();
            Vector3d vecForward = vecC * dist / 2;
            Vector3d vecBackward = vecC * (dist * -1);

            // transforms
            var tDirForward = Rhino.Geometry.Transform.Translation(vecForward);
            var tDirBackward = Rhino.Geometry.Transform.Translation(vecBackward);

            // duplicate the center point
            Point3d pForward = new Point3d(point);
            Point3d pBackward = new Point3d(point);

            // transform the points
            //pForward.Transform(tDirForward);
            pBackward.Transform(tDirBackward);

            // array to return
            Point3d[] toRet = new Point3d[2];

            toRet[0] = pBackward;
            toRet[1] = pForward;

            return toRet;

        }

        public void Move() {
            Vector3d vecA = tl - bl;
            vecA.Unitize();
            Vector3d vecB = br - bl;
            vecB.Unitize();
            Vector3d vecC = Rhino.Geometry.Vector3d.CrossProduct(vecA, vecB);
            vecC.Unitize();
            vecC *= dist;

            var tDir = Rhino.Geometry.Transform.Translation(vecC);
            point.Transform(tDir);
            bl.Transform(tDir);
            tl.Transform(tDir);
            br.Transform(tDir);
        }

        public void TurnLeft() {
            Vector3d vecC = tl - bl;
            vecC.Unitize();

            var tRot = Rhino.Geometry.Transform.Rotation((Math.PI / 180) * angle, vecC, point);
            point.Transform(tRot);
            bl.Transform(tRot);
            tl.Transform(tRot);
            br.Transform(tRot);
        }

        public void TurnRight() {
            Vector3d vecC = tl - bl;
            vecC.Unitize();

            var tRot = Rhino.Geometry.Transform.Rotation((Math.PI / 180) * -angle, vecC, point);
            point.Transform(tRot);
            bl.Transform(tRot);
            tl.Transform(tRot);
            br.Transform(tRot);
        }

        public void PitchUp() {
            Vector3d vecC = br - bl;
            vecC.Unitize();

            var tRot = Rhino.Geometry.Transform.Rotation((Math.PI / 180) * angle, vecC, point);
            point.Transform(tRot);
            bl.Transform(tRot);
            tl.Transform(tRot);
            br.Transform(tRot);
        }

        public void PitchDown() {
            Vector3d vecC = br - bl;
            vecC.Unitize();

            var tRot = Rhino.Geometry.Transform.Rotation((Math.PI / 180) * -angle, vecC, point);
            point.Transform(tRot);
            bl.Transform(tRot);
            tl.Transform(tRot);
            br.Transform(tRot);
        }
    }

    public class TurtleVoxelsComponent : GH_Component {

        public TurtleVoxelsComponent() : base("Leaf Voxel Turtle", "LVT",
          "A 3D agent based voxel drawing component based around the L-System.",
          "Leaf", "Main") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddTextParameter("Shape Code", "S", "L-System string to take in.", GH_ParamAccess.item, "");
            pManager.AddNumberParameter("Voxel Size", "V", "Standard voxel size.", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Angle", "A", "Turn angle.", GH_ParamAccess.item, 90);
            pManager.AddNumberParameter("Step Distance", "D", "Distance to move over for each voxel. This value should equal Voxel Size (V).", GH_ParamAccess.item, 1);
            pManager.AddGenericParameter("Leaf Geometry", "G", "Geometries to substitute symbols with.", GH_ParamAccess.list);
            pManager[0].Optional = false;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddBrepParameter("Geometries", "GS", "The 3D representation of the L-System", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            // create variables that correspond to inputs
            string shapeCode = "";
            GH_Number voxelSize = new GH_Number(1);
            GH_Number angle = new GH_Number(90);
            GH_Number stepDistance = new GH_Number(1);
            List<LeafGeometryData> mD = new List<LeafGeometryData>();

            // reference the inputs to variables
            if (!DA.GetData(0, ref shapeCode)) return;
            if (!DA.GetData(1, ref voxelSize)) return;
            if (!DA.GetData(2, ref angle)) return;
            if (!DA.GetData(3, ref stepDistance)) return;

            DA.GetDataList(4, mD);

            /// make dictionary of geometries 
            IDictionary<string, int> geoDict = new Dictionary<string, int>();
            for (int i = 0; i < mD.Count; i++) {
                if (mD[i] != null && mD[i].IsValid) {
                    geoDict.Add(mD[i].Symbol, i);
                }
            }

            // create turtle pointer
            TurtlePointer TP = new TurtlePointer(0, 0, 0, voxelSize.Value, stepDistance.Value, angle.Value);

            // create pointer stack limit to 500 steps of recursion. May increase this in the future
            TurtlePointer[] pointerStack = new TurtlePointer[500];
            int stackPointer = 0;

            // list of breps to accumulate. Starts with 5000 slots, adds another 5000 if filled
            List<Brep> boxes = new List<Brep>(5000);

            for (int i = 0; i < shapeCode.Length; i++) {
                string c = shapeCode[i].ToString();
                if (c == "^") {
                    TP.PitchUp();
                } else if (c == "/") {
                    TP.PitchDown();
                } else if (c == "-") {
                    TP.TurnLeft();
                } else if (c == "+") {
                    TP.TurnRight();
                } else if (c == "_") {
                    TP.Move();
                } else if (c == "[") {
                    pointerStack[stackPointer] = TP.State;
                    stackPointer++;
                    // if this happens, throw an exception
                    if (stackPointer >= pointerStack.Length) {
                        stackPointer = pointerStack.Length - 1;
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Stack overflow!");
                        return;
                    }
                } else if (c == "]") {
                    TP.State = pointerStack[stackPointer - 1];
                    stackPointer--;
                    if (stackPointer <= 0) {
                        stackPointer = 0;
                    }
                } else {
                    // move the pointer forward
                    TP.Move();
                    if (geoDict.ContainsKey(c)) {
                        // replace with geometry
                        boxes.Add(InsertGeometry(mD[geoDict[c]], TP));
                    } else {
                        // replace with block
                        boxes.Add(CreateBlock(TP));
                    }
                    
                    
                }

            }

            // assign the output
            DA.SetDataList(0, boxes);

        }

        public Brep CreateBlock(TurtlePointer tP) {
            Plane p = new Plane(tP.TL, tP.BL, tP.BR);
            Box b = new Box(p, new Interval(0, tP.GetVoxelSize), new Interval(0, tP.GetVoxelSize), new Interval(0, tP.GetVoxelSize));
            return b.ToBrep();
        }

        public Brep InsertGeometry(LeafGeometryData lD, TurtlePointer tP) {
            GeometryBase gB = lD.Geometry.Duplicate();
            Brep br = Brep.TryConvertBrep(gB);
            Plane sourcePlane = lD.OrientPlane;
            Plane targetPlane = tP.OrientPlane;
            // if br needs to be scaled, scale it

            double scaleFactor = tP.GetVoxelSize / lD.GetVoxelSize;
            // scale br
            if (scaleFactor != 1) {
                var trans = Rhino.Geometry.Transform.Scale(lD.Point, scaleFactor);
                br.Transform(trans);
                sourcePlane.Transform(trans);
            }

            // orient the two planes
            var t = Rhino.Geometry.Transform.PlaneToPlane(sourcePlane, targetPlane);
            br.Transform(t);
            return br;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.turtleicon.ToBitmap();

        public override Guid ComponentGuid => new Guid("3aa93db1-48fc-444f-89e4-c8d6364d8a0a");
    }

    public class TurtleLineComponent : GH_Component {
        // code will be optimized later
        public TurtleLineComponent() : base("Leaf Line Turtle", "LLT",
          "A 3D agent based line drawing component based around the L-System.",
          "Leaf", "Main") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddTextParameter("Shape Code", "S", "L-System string to take in.", GH_ParamAccess.item, "");
            pManager.AddNumberParameter("Angle", "A", "Turn angle.", GH_ParamAccess.item, 90);
            pManager.AddNumberParameter("Step Distance", "D", "Distance to move over for each line.", GH_ParamAccess.item, 1);
            pManager[0].Optional = false;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddLineParameter("Lines", "L", "The 3D representation of the L-System", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            // create variables that correspond to inputs
            string shapeCode = "";
            GH_Number angle = new GH_Number(90);
            GH_Number stepDistance = new GH_Number(1);

            // reference the inputs to variables
            if (!DA.GetData(0, ref shapeCode)) return;
            if (!DA.GetData(1, ref angle)) return;
            if (!DA.GetData(2, ref stepDistance)) return;

            // create turtle pointer
            TurtlePointer TP = new TurtlePointer(0, 0, 0, stepDistance.Value, stepDistance.Value, angle.Value);

            // create pointer stack limit to 500 steps of recursion. May increase this in the future
            TurtlePointer[] pointerStack = new TurtlePointer[500];
            int stackPointer = 0;

            // list of curves to accumulate. Starts with 5000 slots, adds another 5000 if filled
            List<Line> lines = new List<Line>(5000);

            for (int i = 0; i < shapeCode.Length; i++) {
                string c = shapeCode[i].ToString();
                if (c == "^") {
                    TP.PitchUp();
                } else if (c == "/") {
                    TP.PitchDown();
                } else if (c == "-") {
                    TP.TurnLeft();
                } else if (c == "+") {
                    TP.TurnRight();
                } else if (c == "_") {
                    TP.Move();
                } else if (c == "[") {
                    pointerStack[stackPointer] = TP.State;
                    stackPointer++;
                    // if this happens, throw an exception
                    if (stackPointer >= pointerStack.Length) {
                        stackPointer = pointerStack.Length - 1;
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Stack overflow!");
                        return;
                    }
                } else if (c == "]") {
                    TP.State = pointerStack[stackPointer - 1];
                    stackPointer--;
                    if (stackPointer <= 0) {
                        stackPointer = 0;
                    }
                } else {
                    // move the pointer forward
                    TP.Move();
                    // any other symbol means place a curve
                    lines.Add(CreateLine(TP));

                }
            }
            // assign the output
            DA.SetDataList(0, lines);
        }

        public Line CreateLine(TurtlePointer tP) {
            Point3d[] curveEnds = tP.CurveEnds();
            // construct a line
            Line crv = new Line(curveEnds[0], curveEnds[1]);
            return crv;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.turtleicon2.ToBitmap();

        public override Guid ComponentGuid => new Guid("f8792a0b-67ba-4c61-bbd3-bbc4b60c1ff8");
    }

    public class GeometryComponent : GH_Component {

        public GeometryComponent() : base("Leaf Geometry", "LG",
          "Used to substitute symbols with geometry via the Leaf Turtle component.",
          "Leaf", "Geometry") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {

            pManager.AddTextParameter("Symbol", "S", "Symbol to replace", GH_ParamAccess.item, "");
            pManager.AddBrepParameter("Geometry", "G", "Geometry to replace with.", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "P", "Points must be on the front face of the voxel, and selected in order: Bottom left, Top left, Bottom right.", GH_ParamAccess.list);

            pManager[0].Optional = false;
            pManager[1].Optional = false;
            pManager[2].Optional = false;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Leaf Geometry", "G", "experimental output", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            // create variables that correspond to inputs
            string symbol = null;
            Brep brep = null;
            List<Point3d> points = new List<Point3d>();

            // reference the inputs to variables
            DA.GetData(0, ref symbol);
            DA.GetData(1, ref brep);
            DA.GetDataList(2, points);

            bool errored = false;

            // symbol
            if (symbol == null || symbol == "") {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "You must choose a symbol to replace.");
                errored = true;
            } else if ("^/-+_".Contains(symbol)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Cannot replace geometry for movement symbols.");
                errored = true;
            } else if (symbol.Length > 1) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Create a separate Leaf Geometry component to replace more than 1 symbol at a time.");
                errored = true;
            }

            // brep
            if (brep == null || !brep.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Set a brep geometry to replace with.");
                errored = true;
            }

            // points
            if (points[0] == null || points.Count != 3) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Check that you have set exactly 3 points.");
                errored = true;
            }

            if (errored) return;

            // TODO check points for squraeness 

            // package data for sending
            LeafGeometryData mD = new LeafGeometryData(symbol, brep, points);

            // assign the output
            DA.SetData(0, mD);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.leafgeoicon.ToBitmap();

        public override Guid ComponentGuid => new Guid("866f12ba-7e53-11ec-90d6-0242ac120003");
    }
}