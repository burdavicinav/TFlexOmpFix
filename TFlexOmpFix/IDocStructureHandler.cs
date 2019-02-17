using TFlex.Model;

namespace TFlexOmpFix
{
    public interface IDocStructureHandler
    {
        void IsValid(Document doc, string configuration = null);
    }
}