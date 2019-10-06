using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFlexAPITest
{
    public static class APILoader
    {
        public static bool Init()
        {
            TFlex.ApplicationSessionSetup setup = new TFlex.ApplicationSessionSetup();
            setup.ReadOnly = false;
            setup.EnableDOCs = true;

            return TFlex.Application.InitSession(setup);
        }
    }
}