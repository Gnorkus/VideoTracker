using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Forms;
using Npgsql;

namespace VideoTrack.Model
{
    // Define a custom attribute as a helper class
    // This will allow us to specify the length of string fields
    // in a database.  It can be modified for use on just
    // about any SQL flavor exists.
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class MaxLengthAttribute : Attribute
    {
        public MaxLengthAttribute(long value) { MaxLength = value; }
        public long MaxLength { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class PrimaryIdentityKeyAttribute : Attribute
    {
        public PrimaryIdentityKeyAttribute() {  }
    }





    public class CoreDatabase
    {
        private const string TABLE_EXISTS_TEST = @"SELECT EXISTS(
                                                  SELECT FROM
                                                  information_schema.tables
                                                  WHERE table_schema LIKE 'public' AND
                                                      table_type LIKE 'BASE TABLE' AND
                                                      table_name = '";

        private const string TABLE_EXISTS_SUFFIX = "');";


        // Our derived classes can create their own table names, just
        // in case we want to use a query that has less members
        // than the original class (i.e.  you're not asking for
        // all of the fields in the query).
        protected string TableName { get; set; }
        protected NpgsqlConnection m_connref;
    

        public CoreDatabase() 
        {
            m_connref = null;
        }

        public bool Initialize(ref NpgsqlConnection newConn)
        {
            if (m_connref == null)
                m_connref = newConn;

            if (TableName == null || TableName.Length == 0)
            {
                Type type = this.GetType();
                TableName = type.Name;
                if (TableExists(TableName) == false)
                    CreateTable();
            }

            return true;
        }

        public bool TableExists(string TableName) { 
            string strQueryString = TABLE_EXISTS_TEST + TableName + TABLE_EXISTS_SUFFIX;
            string tstr;
            bool rval = false;

            if (m_connref == null)
                return false;

            m_connref.Open();

            var command = m_connref.CreateCommand();
            command.CommandText = strQueryString;
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    rval = reader.GetBoolean(0);
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return false;
                }
                finally
                {
                }
            }

            m_connref.Close();

            return rval; 
        }

        public bool DropTable()
        {
            Type type = this.GetType();
            string attemptTableName = type.Name; // Use the class name as the table name
            var sb = new StringBuilder($"DROP TABLE IF EXISTS \"{attemptTableName}\"");

            try
            {
                m_connref.Open();
                NpgsqlCommand command = new NpgsqlCommand(sb.ToString(), m_connref);
                command.ExecuteNonQuery();
                m_connref.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }


            return true;
        }


        public bool CreateTable() 
        {
            if (m_connref == null)
                return false;

            Type type = this.GetType(); 
            string attemptTableName = type.Name; // Use the class name as the table name
            PropertyInfo[] properties = type.GetProperties();

            var sb = new StringBuilder($"CREATE TABLE IF NOT EXISTS \"{attemptTableName}\" (");

            foreach (var prop in properties)
            {
                var columnName = prop.Name;
                var columnType = GetPostgresType(prop);
                sb.Append($"\"{columnName}\" {columnType}, ");
            }

            sb.Length -= 2; // Remove the trailing comma and space
            sb.Append(");");

            Clipboard.SetText(sb.ToString());

            try
            {
                m_connref.Open();
                NpgsqlCommand command = new NpgsqlCommand(sb.ToString(), m_connref);
                command.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            finally
            {
                m_connref.Close();
            }

            return true;
        }

        private static string GetPostgresType(PropertyInfo prop)
        {
            var type = prop.PropertyType;

            if (type == typeof(int))
            {
                var primaryIdentityKeyAttr = prop.GetCustomAttribute<PrimaryIdentityKeyAttribute>();
                return (primaryIdentityKeyAttr != null) ? "INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY"
                                                : "INTEGER";
            }
            if (type == typeof(long))
                return "BIGINT";
            if (type == typeof(float) || type == typeof(double))
                return "DOUBLE PRECISION";
            if (type == typeof(decimal))
                return "NUMERIC";
            if (type == typeof(bool))
                return "BOOLEAN";
            if (type == typeof(DateTime))
                return "TIMESTAMP";
            if (type == typeof(string))
            {
                // Check for [MaxLength] attribute
                var maxLengthAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
                return maxLengthAttr != null ? $"VARCHAR({maxLengthAttr.MaxLength})" : "TEXT";
            }

            throw new NotSupportedException($"Unsupported property type: {type.Name}");
        }

    }
}
