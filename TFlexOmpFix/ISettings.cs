namespace TFlexOmpFix
{
    public interface ISettings
    {
        Settings Read();

        void Write(Settings settings);
    }
}