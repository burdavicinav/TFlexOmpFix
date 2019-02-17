using System.Linq;
using TFlex.Model;
using TFlexOmpFix.Exceptions;

namespace TFlexOmpFix
{
    public class DocStructureSingularHandler :
        DocStructureHandler,
        IDocStructureHandler
    {
        public void IsValid(Document doc, string configuration = null)
        {
            if (doc.ModelConfigurations.ConfigurationCount == 0
                && doc.GetProductStructures().Count() != 1
                || doc.ModelConfigurations.ConfigurationCount > 0
                && doc.GetProductStructures().Where(x => x.Name == configuration).Count() == 0)
            {
                throw new DocStructureException(doc.FileName);
            }
            else if (Next != null)
            {
                Next.IsValid(doc, configuration);
            }
        }
    }
}