using System;

using TFlex;

namespace TFlexOmpFix
{
    public class OmpFixFactory : PluginFactory
    {
        public override Plugin CreateInstance()
        {
#if DEBUG
            //].Show("Test!");

#endif
            return new OmpFixPlugin(this);
        }

        public override Guid ID
        {
            get
            {
                return Lib.Guid;
            }
        }

        public override string Name
        {
            get
            {
                return Lib.Name;
            }
        }
    };
}