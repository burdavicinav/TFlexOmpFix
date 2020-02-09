using Oracle.DataAccess.Client;
using System;
using System.Text;
using System.Text.RegularExpressions;
using TFlexOmpFix.Objects;

namespace TFlexOmpFix.Repositories
{
    public class TFlexObjSynchRepository
    {
        public V_SEPO_TFLEX_OBJ_SYNCH GetSynchObj(string section, string docSign, string sign, int objectType)
        {
            OracleCommand cmd = new OracleCommand();

            cmd.Parameters.Add("section", section);
            cmd.Parameters.Add("docsign", (docSign == String.Empty) ? "0" : docSign);
            cmd.Parameters.Add("objtype", objectType);

            cmd.CommandText =
                @"select id, id_section, tflex_section, id_docsign, tflex_docsign,
                    kotype, botype, botypename, botypeshortname, bostatecode,
                    bostatename, bostateshortname, filegroup, filegroupname,
                    filegroupshortname, owner, ownername, ompsection, ompsectionname,
                    param_dependence, id_param, param, param_expression, id_sectype
                from omp_adm.v_sepo_tflex_obj_synch
                where tflex_section = :section and coalesce(tflex_docsign, '0') = :docsign
                    and coalesce(sectype_sign, 0) = :objtype
                order by param_dependence desc";

            cmd.Connection = Connection.GetInstance();

            using (OracleDataReader rd = cmd.ExecuteReader())
            {
                while (rd.Read())
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
                    synch.PARAM_DEPENDENCE = rd.GetInt32(19);

                    if (!rd.IsDBNull(20)) synch.ID_PARAM = rd.GetInt32(20);
                    if (!rd.IsDBNull(21)) synch.PARAM = rd.GetString(21);
                    if (!rd.IsDBNull(22)) synch.PARAM_EXPRESSION = rd.GetString(22);
                    if (!rd.IsDBNull(23)) synch.ID_SECTYPE = rd.GetInt32(23);

                    if (synch.PARAM_DEPENDENCE == 0 ||
                        synch.PARAM_DEPENDENCE == 1 && Regex.IsMatch(sign, synch.PARAM_EXPRESSION))
                    {
                        return synch;
                    }
                }
            }

            return null;
        }
    }
}