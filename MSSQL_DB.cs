using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TableParser
{
    public class MSSQL_DB
    {
        public string DBName { get; private set; }
        public string DBServer { get; private set; }
        public static string Source;
        private List<string> errors;
        private readonly string _TransactDataToPostgres_CreateQuery;
        public readonly SqlConnection conn;

        private string connectionString;

        public MSSQL_DB(string DBName, string DBServer)
        {
            this.DBName = DBName;
            this.DBServer = DBServer;
            Source = string.Format("Server={0};Database={1};Trusted_Connection=True;",DBServer ,DBName);
            conn = new SqlConnection(Source);
            _TransactDataToPostgres_CreateQuery = String.Format(@"
                            CREATE OR ALTER PROCEDURE [dbo].[TransactDataToPostgres] ({0} varchar(100), {1} varchar(100))
                            AS 
                            begin
                            DECLARE
	                            @cursor_table table (table_name varchar(300), create_table_text text)
                            DECLARE 
	                            @v_table_name varchar(300),
	                            @v_create_table_text varchar(7000),
	                            @OPENQUERY varchar(8000)
                            SET @OPENQUERY = 'EXEC ('+CHAR(39)+'CREATE SCHEMA  IF NOT EXISTS '+ {0} +CHAR(39)+') AT [{1}]'
                            EXEC (@OPENQUERY)
	
                            INSERT INTO @cursor_table SELECT * FROM GetTabsFromSchema({0}) 
                            DECLARE insert_table_cur CURSOR FOR 
	                            SELECT table_name, create_table_text from @cursor_table 
                            open insert_table_cur
                            fetch next from insert_table_cur into @v_table_name, @v_create_table_text;
                            While @@FETCH_STATUS = 0
	                            begin 	
		                            SET @OPENQUERY = 'EXEC ('+CHAR(39)+@v_create_table_text+CHAR(39)+') At [{1}]'
		                            EXEC (@OPENQUERY)
		                            fetch next from insert_table_cur into @v_table_name, @v_create_table_text; 
	                            end
                            close insert_table_cur 

                            open insert_table_cur
	                            fetch next from insert_table_cur into @v_table_name, @v_create_table_text;
	                            While @@FETCH_STATUS = 0 
	                            begin
	                            SET @OPENQUERY = 'INSERT OPENQUERY ({1} ,'+char(39)+ 'select * from ' +'""'+{0}+'""' +'.'+'""'+@v_table_name+'""' + char(39) +') 
	                            SELECT * FROM ' + @v_table_name
	                            EXEC (@OPENQUERY)
	                            fetch next from insert_table_cur into @v_table_name, @v_create_table_text;
	                            end 
                            close insert_table_cur
                            end

                            ", DBName, DBServer);

            connectionString = String.Format("Server={0};Database={1};Trusted_Connection=True;", DBServer, DBName);
        }

        public void OpenConnection() {
                try
                {
                    conn.Open();
                    ExecuteQuery();
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
        }
        

        public void ExecuteQuery()
        {
            using (var command = new SqlCommand(_TransactDataToPostgres_CreateQuery, conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            })
            {
                if (IsSQLQueryValid(_TransactDataToPostgres_CreateQuery, out errors))
                {
                    conn.Open();
                    command.ExecuteNonQuery();
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show(errors.ToString(),
                                          "Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Error);
                }
            }
        }

        public bool IsSQLQueryValid(string sql, out string errors)
        {
            errors = new List<string>();
            TSql140Parser parser = new TSql140Parser(false);
            TSqlFragment fragment;
            IList<ParseError> parseErrors;

            using (TextReader reader = new StringReader(sql))
            {
                fragment = parser.Parse(reader, out parseErrors);
                if (parseErrors != null && parseErrors.Count > 0)
                {
                    errors = parseErrors.Select(e => e.Message).ToList();
                    return false;
                }
            }
            return true;
        }

    }
}
