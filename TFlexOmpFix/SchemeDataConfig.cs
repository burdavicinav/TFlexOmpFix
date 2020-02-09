using TFlex.Model;

namespace TFlexOmpFix
{
    public class SchemeDataConfig
    {
        public ProductStructure Structure { get; set; }

        public SchemeDataConfig(ProductStructure structure)
        {
            Structure = structure;
        }

        public SchemeData GetScheme()
        {
            SchemeData schemeData = new SchemeData();

            var scheme = Structure.GetScheme();

            foreach (var param in scheme.Parameters)
            {
                switch (param.Name)
                {
                    case "Обозначение":
                        schemeData.SignId = param.UID;
                        break;

                    case "Наименование":
                        schemeData.NameId = param.UID;
                        break;

                    case "Количество":
                        schemeData.QtyId = param.UID;
                        break;

                    case "Раздел":
                        schemeData.SectionId = param.UID;
                        break;

                    case "Код документа":
                        schemeData.DocCodeId = param.UID;
                        break;

                    case "Исполнение":
                        schemeData.ConfigId = param.UID;
                        break;

                    case "Тип для комплекта":
                        schemeData.ObjectType = param.UID;
                        break;

                    default:
                        break;
                }
            }

            return schemeData;
        }
    }
}