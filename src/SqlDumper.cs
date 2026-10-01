using MySql.Data.MySqlClient;
using System.Data;
using System.Text;

namespace MySqlDumper
{
    public class SqlDumper
    {
        private readonly string _ip;
        private readonly string _user;
        private readonly string _password;
        private readonly string _database;
        private readonly string _port;
        private readonly string _outputDirectory;
        private readonly List<string> _tablesToDump;

        private const int Timeout = 99999999;

        public SqlDumper(Config config)
        {
            _ip = config.HostName;
            _database = config.DatabaseName;
            _user = config.Login;
            _password = config.Password;
            _port = config.Port;
            _outputDirectory = config.OutputDirectory;
            _tablesToDump = config.TablesToDump;
        }

        public void DumpDatabase(string outputPath)
        {
            var filePath = Path.Combine(_outputDirectory, Path.GetFileName(outputPath));
            
            using var conn = new MySqlConnection(
                $"server={_ip};user={_user};password={_password};database={_database};port={_port};" +
                $"Connection Timeout={Timeout};default command timeout={Timeout}"
            );
            conn.Open();

            var tables = _tablesToDump;
            if (_tablesToDump.Count == 0)
            {
                var dbTables = conn.GetSchema("Tables");

                tables = [];
                foreach (DataRow row in dbTables.Rows)
                {
                    var tableName = row["TABLE_NAME"].ToString();
                    if (tableName == null)
                    {
                        continue;
                    }
                    tables.Add(tableName);
                }
            }
            
            Parallel.ForEach(tables.AsEnumerable(), tableName =>
            {
                var sb = new StringBuilder();
                var tableFileName = tableName + ".sql";

                var tableFilePath = Path.Combine(_outputDirectory, Path.GetFileName(tableFileName));

                using var innerConn = new MySqlConnection(
                    $"server={_ip};user={_user};password={_password};database={_database};port={_port};" +
                    $"Connection Timeout={Timeout};default command timeout={Timeout}"
                );
                innerConn.Open();

                using var cmdCreateTable = new MySqlCommand($"SHOW CREATE TABLE `{tableName}`;", innerConn);
                cmdCreateTable.CommandTimeout = Timeout;

                using (var reader = cmdCreateTable.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var createTable = reader["Create Table"].ToString();

                        if (createTable != null) {
                            lock (sb)
                            {
                                sb.AppendLine(createTable + ";");
                            }
                        }
                    }
                }

                using var cmdSelect = new MySqlCommand($"SELECT * FROM `{tableName}`;", innerConn);
                cmdSelect.CommandTimeout = Timeout;

                using (var reader = cmdSelect.ExecuteReader())
                {
                    var batchInserts = new List<string>();

                    while (reader.Read())
                    {
                        var insert = new StringBuilder($"INSERT INTO `{tableName}` VALUES(");

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            if (i > 0)
                            {
                                insert.Append(',');
                            }

                            if (reader.IsDBNull(i))
                            {
                                insert.Append("NULL");
                            }
                            else
                            {
                                var value = reader.GetValue(i);

                                switch (value)
                                {
                                    case string s:
                                        insert.Append($"'{s.Replace("'", "''")}'");
                                        break;
                                    case DateTime time:
                                        insert.Append($"'{time.ToString("yyyy-MM-dd")}'");
                                        break;
                                    case decimal or float or double:
                                        insert.Append(value.ToString()?.Replace(",", "."));
                                        break;
                                    default:
                                        insert.Append(value);
                                        break;
                                }
                            }
                        }

                        insert.Append(");");
                        batchInserts.Add(insert.ToString());

                        if (batchInserts.Count < 1000) continue;
                        lock (sb)
                        {
                            sb.AppendLine(string.Join("\n", batchInserts));
                        }

                        batchInserts.Clear();
                    }

                    if (batchInserts.Count > 0)
                    {
                        lock (sb)
                        {
                            sb.AppendLine(string.Join("\n", batchInserts));
                        }
                    }
                }

                lock (sb)
                {
                    sb.AppendLine();
                }
                
                using var file = new StreamWriter(tableFilePath);
                file.Write(sb.ToString());
            });
        }
    }
}
