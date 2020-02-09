using System;

namespace TFlexOmpFix.Objects
{
    public class V_SEPO_TFLEX_OBJ_SYNCH
    {
        public decimal ID { get; set; }

        public decimal ID_SECTION { get; set; }

        public string TFLEX_SECTION { get; set; }

        public Nullable<decimal> ID_DOCSIGN { get; set; }

        public string TFLEX_DOCSIGN { get; set; }

        public decimal BOTYPE { get; set; }

        public string BOTYPENAME { get; set; }

        public string BOTYPESHORTNAME { get; set; }

        public decimal BOSTATECODE { get; set; }

        public string BOSTATENAME { get; set; }

        public string BOSTATESHORTNAME { get; set; }

        public Nullable<decimal> FILEGROUP { get; set; }

        public string FILEGROUPNAME { get; set; }

        public string FILEGROUPSHORTNAME { get; set; }

        public Nullable<decimal> OWNER { get; set; }

        public string OWNERNAME { get; set; }

        public decimal KOTYPE { get; set; }

        public decimal OMPSECTION { get; set; }

        public string OMPSECTIONNAME { get; set; }

        public virtual int PARAM_DEPENDENCE { get; set; }

        public virtual int ID_PARAM { get; set; }

        public virtual string PARAM { get; set; }

        public virtual string PARAM_EXPRESSION { get; set; }

        public int ID_SECTYPE { get; set; }
    }
}