using Oracle.DataAccess.Client;
using System;
using TFlexOmpFix.Objects;

namespace TFlexOmpFix.Repositories
{
    public class TFlexObjSynchRepository
    {
        public V_SEPO_TFLEX_OBJ_SYNCH GetSynchObj(string section, string docSign)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.CommandText =
                @"select id, id_section, tflex_section, id_docsign, tflex_docsign,
                    kotype, botype, botypename, botypeshortname, bostatecode,
                    bostatename, bostateshortname, filegroup, filegroupname,
                    filegroupshortname, owner, ownername, ompsection, ompsectionname
                from omp_adm.v_sepo_tflex_obj_synch
                where tflex_section = :section and coalesce(tflex_docsign, '0') = :docsign";

            cmd.Connection = Connection.GetInstance();

            cmd.Parameters.Add("section", section);
            cmd.Parameters.Add("docsign", (docSign == String.Empty) ? "0" : docSign);

            using (OracleDataReader rd = cmd.ExecuteReader())
            {
                if (rd.Read())
                {
                    V_SEPO_TFLEX_OBJ_SYNCH synch = new V_SEPO_TFLEX_OBJ_SYNCH();

                    synch.ID = rd.GetDecimal(0);
                    synch.ID_SECTION = rd.GetDecimal(1);
                    synch.TFLEX_SECTION = rd.GetString(2);

                    if (!rd.IsDBNull(3)) synch.ID_DOCSIGN = rd.GetDecimal(3);
                    if (!rd.IsDBNull(4)) synch.TFLEX_DOCSIGN = rd.GetString(4);

                    synch.KOTYPE = rd.GetDecimal(5);
                    synch.BOTYPE = rd.GetDecimal(6);
                    synch.BOTYPENAME = rd.GetString(7);
                    synch.BOTYPESHORTNAME = rd.GetString(8);
                    synch.BOSTATECODE = rd.GetDecimal(9);
                    synch.BOSTATENAME = rd.GetString(10);
                    synch.BOSTATESHORTNAME = rd.GetString(11);

                    if (!rd.IsDBNull(12)) synch.FILEGROUP = rd.GetDecimal(12);
                    if (!rd.IsDBNull(13)) synch.FILEGROUPNAME = rd.GetString(13);
                    if (!rd.IsDBNull(14)) synch.FILEGROUPSHORTNAME = rd.GetString(14);
                    if (!rd.IsDBNull(15)) synch.OWNER = rd.GetDecimal(15);
                    if (!rd.IsDBNull(16)) synch.OWNERNAME = rd.GetString(16);

                    synch.OMPSECTION = rd.GetDecimal(17);
                    synch.OMPSECTIONNAME = rd.GetString(18);

                    return synch;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}