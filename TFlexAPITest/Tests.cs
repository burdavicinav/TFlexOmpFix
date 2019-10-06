using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TFlex;
using TFlex.Model;

namespace TFlexAPITest
{
    public static class Tests
    {
        public static void StdObjectsRead()
        {
            StreamWriter input = new StreamWriter("dbg.txt");

            System.IO.DirectoryInfo dir =
                new DirectoryInfo(@"C:\Program Files (x86)\T-FLEX\Стандартные элементы 15\Стандартные изделия 15");

            FileInfo[] files = dir.GetFiles("*.grb", SearchOption.AllDirectories);

            int length = files.Length;

            foreach (var file in files)
            {
                input.WriteLine("* " + file.Name);

                Document doc = TFlex.Application.OpenDocument(file.FullName, false);

                foreach (var perem in doc.GetVariables())
                {
                    if (perem.Name == "$sp")
                    {
                        Match match = Regex.Match(perem.Expression, @"%%\d{3}|%%\D");

                        do
                        {
                            input.WriteLine(match.Value);
                            match = match.NextMatch();
                        }
                        while (match.Value != String.Empty);
                    }
                }

                doc.Close();
            }

            input.Close();
        }

        public static void ModelConfig()
        {
            Document doc = TFlex.Application.OpenDocument(
                @"C:\Users\Alexander\Downloads\Telegram Desktop\7000-0001\7000-0001_0000-001.grb", false);

            var config = doc.ModelConfigurations;

            for (int i = 0; i < config.ConfigurationCount; i++)
            {
                ModelConfiguration c1 = config.GetConfiguration(i);
                string str = config.GetConfigurationName(i);
            }

            var structures = doc.GetProductStructures();

            foreach (var structure in structures)
            {
            }
        }
    }
}