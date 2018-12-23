namespace TFlexOmpFix
{
    public interface ICryptoAlg
    {
        string Encoding(string str);

        string Decoding(string str);
    }
}