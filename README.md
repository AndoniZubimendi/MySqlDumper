MySQLDumper
===========

Command line utility to dump MySQL database tables to sql files . Built with .NET 9.




Usage
-----

```sh
$ MySqlDumper.exe --server <server> -P <port> -u <user> -p <password> --database <db> --dir <dir>
```

Or

```sh
$ MySqlDumper.exe -c <config_file>
```

```
Usage:
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

```

Sample ini file

```
Server=my_server_ip
Port=3306
Login=my_user
Password=my_pass
Database=my_database
OutputDirectory=outputdir
;Tables=table1,table2
;SkipErrors=false
```