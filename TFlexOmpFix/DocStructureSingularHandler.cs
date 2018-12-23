using System.Linq;
using TFlex.Model;
using TFlexOmpFix.Exceptions;

namespace TFlexOmpFix
{
    public class DocStructureSingularHandler :
        DocStructureHandler,
        IDocStructureHandler
    {
        public void IsValid(Document doc)
        {
            if (doc.GetProductStructures().Count() != 1)
            {
                throw new DocStructureException(doc.FileName);
            }
            else if (Next != null)
            {
                Next.IsValid(doc);
            }
        }
    }
}