namespace MySqlDumper
{
	public sealed record class Config(
		string HostName,
		string Port,
		string Login,
		string Password,
		string DatabaseName,
		string OutputDirectory,
		bool ReplaceExistingFiles,
		bool SkipErrors,
		List<string> TablesToDump)
	{
		public bool Equals(Config? other) => false; // TODO
		public override int GetHashCode() => 0; // TODO
	}
}