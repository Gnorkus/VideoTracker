using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace VideoTrack.Model
{
    public class CoreDatabaseConn
    {

        private NpgsqlConnection m_DatabaseConn;
        public CoreDatabaseConn(string strConnection=null) 
        { 
            if (strConnection == null)
                m_DatabaseConn = new NpgsqlConnection(Static.defaultConnString);
            else
                m_DatabaseConn = new NpgsqlConnection(strConnection);
        }
    }
}
