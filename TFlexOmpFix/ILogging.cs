namespace TFlexOmpFix
{
    public interface ILogging
    {
        void Write(string log);

        void Close();
    }
}