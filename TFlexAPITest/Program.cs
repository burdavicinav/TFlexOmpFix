using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFlexAPITest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            APILoader.Init();

            Tests.ModelConfig();
        }
    }
}