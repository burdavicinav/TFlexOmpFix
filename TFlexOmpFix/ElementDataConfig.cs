using System;
using TFlex.Model;

namespace TFlexOmpFix
{
    public class ElementDataConfig
    {
        private string GetCellValue(RowElementCell cell)
        {
            string cellValue = String.Empty;

            if (cell.Variable != null)
            {
                cellValue = cell.Variable.TextValue;
            }
            else
            {
                cellValue = (cell.Value != null) ? cell.Value.ToString() : String.Empty;
            }

            return cellValue;
        }

        public RowElement Element { get; set; }

        public SchemeData Scheme { get; set; }

        public ElementDataConfig(RowElement elem, SchemeData scheme)
        {
            Element = elem;
            Scheme = scheme;
        }

        public ElementData ConfigData()
        {
            ElementData elemData = new ElementData();

            // ячейки данных
            RowElementCell sectionCell = Element.GetCell(Scheme.SectionId);
            RowElementCell signCell = Element.GetCell(Scheme.SignId);
            RowElementCell nameCell = Element.GetCell(Scheme.NameId);
            RowElementCell qtyCell = Element.GetCell(Scheme.QtyId);
            RowElementCell positionCell = Element.Position;
            RowElementCell docCodeCell = Element.GetCell(Scheme.DocCodeId);

            // секция
            elemData.Section = GetCellValue(sectionCell);
            // обозначение
            elemData.Sign = GetCellValue(signCell);
            // наименование
            elemData.Name = GetCellValue(nameCell);

            // количество
            string qtyStr = GetCellValue(qtyCell);
            decimal qty;
            bool isParse = decimal.TryParse(qtyStr, out qty);

            elemData.Qty = (isParse) ? qty : 1;

            // позиция
            elemData.Position = GetCellValue(positionCell);

            // код документа
            elemData.DocCode = GetCellValue(docCodeCell);

            // файл
            elemData.FilePath = Element.SourceFragmentPath;

            return elemData;
        }
    }
}