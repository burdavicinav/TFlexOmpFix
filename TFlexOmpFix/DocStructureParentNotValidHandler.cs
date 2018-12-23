﻿using System.Linq;
using TFlex.Model;
using TFlexOmpFix.Exceptions;

namespace TFlexOmpFix
{
    public class DocStructureParentNotValidHandler :
        DocStructureHandler,
        IDocStructureHandler
    {
        public void IsValid(Document doc)
        {
            ProductStructure prod = doc.GetProductStructures().FirstOrDefault();

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
                Next.IsValid(doc);
            }
        }
    }
}