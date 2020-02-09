using System.Linq;

namespace TFlexOmpFix
{
    public class ElementData
    {
        public string Section { get; set; }

        public string MainSection
        {
            get
            {
                if (Section == null) return null;

                // разделение секции на части
                string[] parts = Section.Split('\\');

                // если частей больше одной, и при этом 1 часть - "Спецификации"
                // то возвращается вторая часть
                if (parts.Count() > 1)
                {
                    if (parts[0] == "Спецификации")
                    {
                        return parts[1];
                    }
                }

                // иначе возвращается секция
                return Section;
            }
        }

        public string Position { get; set; }

        public string Sign { get; set; }

        public string Name { get; set; }

        public decimal? Qty { get; set; }

        public string FilePath { get; set; }

        public string DocCode { get; set; }

        public string Config { get; set; }

        public string SignFull { get { return string.Concat(Sign, Config); } }

        public int ObjectType { get; set; }
    }
}