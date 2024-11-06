using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline
{
    public static class SapDebugUtil
    {
        public class SapTestAssert : Exception
        {
            public SapTestAssert(string msg)
                : base(msg)
            {

            }
        }
    }
}
