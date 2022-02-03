using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Leaf
{
    public class LeafInfo : GH_AssemblyInfo
    {
        public override string Name => "Leaf";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Properties.Resources.ico.ToBitmap();

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Design using L-Systems.";

        public override Guid Id => new Guid("DF0D5046-4414-4526-A8FA-F804DC912B2E");

        //Return a string identifying you or your company.
        public override string AuthorName => "Sadi Wali";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "sadiwali@hotmail.com";
    }
}