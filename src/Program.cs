using PicoArgs_dotnet;
using IniParser;
using IniParser.Model;

namespace MySqlDumper
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "MySqlDumper | Originally made by https://github.com/GabryB03/";
            Logger.ShowInfo("Welcome to MySqlDumper, changes by https://github.com/AndoniZubimendi/");

            const string outputFilename = "database.sql";

            var config = GetConfig(args);

            Logger.ShowWarning("Dumping the database, please wait a while.");

            try
            {
                var sqlDumper = new SqlDumper(config);
                sqlDumper.DumpDatabase(config.OutputDirectory + '/' + outputFilename);
                Logger.ShowSuccess("The database has been succesfully dumped! Press ENTER to exit from the program.");
            }
            catch (Exception ex)
            {
                var completeReport = "\r\nException Message: " + ex.Message + "\r\nException Source: " + ex.Source +
                                     "\r\nException StackTrace: " + ex.StackTrace + "\r\nException Method: " +
                                     ex.TargetSite?.Name + "\r\nException Class: " + ex.TargetSite?.DeclaringType?.Name;
                Logger.ShowError(
                    $"An error occured while trying to dump the database. Please, report those info in GitHub opening an issue:\r\n\r\n{completeReport}");
            }
        }

        private static Config GetConfig(string[] args)
        {
            var pico = new PicoArgs(args);

            // handle help
            if (pico.Contains("-h", "--help", "-?"))
            {
                Console.WriteLine(CommandLineMessage);
                Environment.Exit(0);
            }

            // parse command line parameters
            var configFile = pico.GetParamOpt("-c", "--config");

            if (!string.IsNullOrWhiteSpace(configFile))
            {
                var config = Load(configFile);
                return config;
            }

            var server = pico.GetParamOpt("-s", "--server") ?? Environment.GetEnvironmentVariable("DB_SERVER");
            var port = pico.GetParamOpt("-P", "--port") ?? Environment.GetEnvironmentVariable("DB_PORT");

            var login = pico.GetParamOpt("-u", "--username") ?? Environment.GetEnvironmentVariable("DB_USERNAME");
            var password = pico.GetParamOpt("-p", "--password") ?? Environment.GetEnvironmentVariable("DB_PASSWORD");
            var tablesStringList = pico.GetParamOpt("-t", "--tables") ??
                                            Environment.GetEnvironmentVariable("DB_TABLES");

            var database = pico.GetParamOpt("-d", "--database") ?? Environment.GetEnvironmentVariable("DB_DATABASE");
            var dir = pico.GetParamOpt("-o", "--dir") ?? Environment.GetEnvironmentVariable("DB_DIR");

            var replace = pico.Contains("-r", "--replace");
            var skipErrors = pico.Contains("-k", "--skip-errors");

            pico.Finished();

            if (string.IsNullOrWhiteSpace(tablesStringList))
            {
                tablesStringList = "";
            }


            // ensure required parameters are present
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(port) ||
                string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(dir))
            {
                Console.WriteLine(CommandLineMessage);
                Environment.Exit(1);
            }

            var tablesToDump = tablesStringList.Split(",").ToList();

            dir = EnsurePathExists(dir);

            return new Config(server, port, login, password, database, dir,
                replace, skipErrors, tablesToDump
            );
        }


        private static Config Load(string configFile)
        {
            Console.WriteLine($"Leyendo {configFile}");

            var parser = new IniDataParser();

            string fileContent = File.ReadAllText(configFile);
            IniData data = parser.Parse(fileContent);

            var server = data.Global["Server"];
            var port = data.Global["Port"] ?? "3306";
            var login = data.Global["Login"];
            var password = data.Global["Password"];
            var database = data.Global["Database"];
            var dir = data.Global["OutputDirectory"];
            var tablesToDumpStringList = data.Global["TablesToDump"] ?? "";

            var replace = data.Global["ReplaceExistingFiles"] != null &&
                          bool.Parse(data.Global["ReplaceExistingFiles"]);
            var skipErrors = data.Global["SkipErrors"] != null && bool.Parse(data.Global["SkipErrors"]);

            Console.WriteLine($"Configuración cargada para {server}/{database} en {dir}");

            // ensure required parameters are present
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) ||
                string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(dir))
            {
                Console.WriteLine($"Error reading config file {configFile}");
                Environment.Exit(1);
            }

            var referenceTables = tablesToDumpStringList.Split(",").ToList();
            dir = EnsurePathExists(dir);


            return new Config(server, port, login, password, database, dir,
                replace, skipErrors, referenceTables
            );
        }

        /// <summary>
        /// Ensure the path of the given filename exists, and return it with a trailing backslash
        /// </summary>
        private static string EnsurePathExists(string path)
        {
            if (!path.EndsWith('/'))
            {
                path += "/";
            }

            var directoryPath = Path.GetDirectoryName(path)
                                ?? throw new InvalidOperationException(
                                    $"Could not determine directory path for '{path}'");

            _ = Directory.CreateDirectory(directoryPath);
            return directoryPath + "/";
        }
        
        private const string CommandLineMessage = """
                                                  Usage: MySqlDumper.exe --server <server> -P <port> -u <user> -p <password> --database <db> --dir <dir>

                                                  Required:
                                                    -c  --config <ini>         Config file
                                                  Or:
                                                    -s, --server <server>      MySQL to connect to
                                                    -P, --port   <port>        MySQL port to connect to
                                                    -u, --username <login>     Username to login
                                                    -p, --password <pass>      Password for login
                                                    -d, --database <db>        Database to process
                                                    -o, --dir <dir>            Output directory

                                                  Options:
                                                    -t, --tables <table1,table2>            Tables to include (comma separated)
                                                    -r, --replace                           Replace existing files (default is to fail if file exists)
                                                    -k, --skip-errors                       Skip errors without writing to file
                                                    -h, --help, -?                          Help information
                                                  """;
    }
}