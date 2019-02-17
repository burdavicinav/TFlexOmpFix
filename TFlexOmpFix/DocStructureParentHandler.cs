using System.Linq;
using TFlex.Model;
using TFlexOmpFix.Exceptions;

namespace TFlexOmpFix
{
    public class DocStructureParentHandler :
        DocStructureHandler,
        IDocStructureHandler
    {
        public void IsValid(Document doc, string configuration = null)
        {
            ProductStructure prod;

            if (doc.ModelConfigurations.ConfigurationCount == 0)
            {
                prod = doc.GetProductStructures().FirstOrDefault();
            }
            else
            {
                prod = doc.GetProductStructures().Where(x => x.Name == configuration).FirstOrDefault();
            }

            if (prod.GetAllRowElements().Where(x => x.ParentRowElement == null).Count() != 1)
            {
                throw new DocStructureParentException(doc.FileName);
            }
            else if (Next != null)
            {
                Next.IsValid(doc, configuration);
            }
        }
    }
}