using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortailsOpacBase.Provider.Oracle
{
    public class OracleProvider
    {
        public OracleProvider()
        {

        }

        public static bool Connect()
        {
            try
            {
                OracleConnection con = new OracleConnection();

                // create connection string using builder
                OracleConnectionStringBuilder ocsb = new OracleConnectionStringBuilder();
                ocsb.Password = "docuware";
                ocsb.UserID = "docuware@ophdev";
                ocsb.DataSource = "datadev-scan.opacoise.fr:1531/OPHDEV_CLIENT";
                ocsb.ConnectionTimeout = 10000;

                // connect
                con.ConnectionString = ocsb.ConnectionString;
                con.Open();
                Console.WriteLine("Connection established (" + con.ServerVersion + ")");

                return true;
            }
            catch(Exception e)
            {
                return false;
            }
        }
    }
}
