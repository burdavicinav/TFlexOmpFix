using System;

namespace TFlexOmpFix
{
    public interface ILogging
    {
        void Write();

        void Write(Exception e);

        void Close();
    }
}