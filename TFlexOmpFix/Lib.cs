using System;

namespace TFlexOmpFix
{
    public class Lib
    {
        public static Guid Guid
        {
            get
            {
#if x64
                // x64
                return new Guid("{0DA08E3D-22A8-4698-BA86-1D6C999E96AA}");
#else
                // x32
                return new Guid("{B581782C-A08D-4699-8E9D-3F65FCCFE7F5}");
#endif
            }
        }

        public static string Name
        {
            get
            {
                return "Экспорт оснастки в КИС \"Омега\"";
            }
        }
    }
}