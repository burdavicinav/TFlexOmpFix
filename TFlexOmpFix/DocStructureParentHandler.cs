using System.Linq;
using TFlex.Model;
using TFlexOmpFix.Exceptions;

namespace TFlexOmpFix
{
    public class DocStructureParentHandler :
        DocStructureHandler,
        IDocStructureHandler
    {
        public void IsValid(Document doc)
        {
            ProductStructure prod = doc.GetProductStructures().FirstOrDefault();

            if (prod.GetAllRowElements().Where(x => x.ParentRowElement == null).Count() != 1)
            {
                throw new DocStructureParentException(doc.FileName);
            }
            else if (Next != null)
            {
                Next.IsValid(doc);
            }
        }
    }
}