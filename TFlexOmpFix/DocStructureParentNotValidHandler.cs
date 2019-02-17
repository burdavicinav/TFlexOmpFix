using System.Linq;
using TFlex.Model;
using TFlexOmpFix.Exceptions;

namespace TFlexOmpFix
{
    public class DocStructureParentNotValidHandler :
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

            RowElement row = prod.GetAllRowElements().Where(
                x => x.ParentRowElement == null).FirstOrDefault();

            SchemeDataConfig schemeConfig = new SchemeDataConfig(prod);
            SchemeData scheme = schemeConfig.GetScheme();

            ElementDataConfig dataConfig = new ElementDataConfig(row, scheme);
            ElementData elemData = dataConfig.ConfigData();

            if (elemData.Sign == string.Empty ||
                 elemData.MainSection != "Документация" && elemData.Sign.Contains("СБ"))
            {
                throw new DocStructureParentSignNotValidException(doc.FileName);
            }
            else if (Next != null)
            {
                Next.IsValid(doc, configuration);
            }
        }
    }
}