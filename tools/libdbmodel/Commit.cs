using System;
using System.Collections.Generic;
using Npgsql;
using System.Linq;

namespace Benchmarker.Models
{
	public class Commit
	{
		public string Hash { get; set; }
		public string Branch { get; set; }
		public string MergeBaseHash { get; set; }
		public DateTime? CommitDate { get; set; }
		
		public Commit ()
		{
		}

		bool ExistsInPostgres (NpgsqlConnection conn)
		{
			var parameters = new PostgresRow ();
			parameters.Set ("hash", NpgsqlTypes.NpgsqlDbType.Varchar, Hash);
			return PostgresInterface.Select (conn, "commit", new string[] { "commitDate" }, "hash = :hash", parameters).Count () > 0;
		}

		public string GetOrUploadToPostgres (NpgsqlConnection conn)
		{
			if (ExistsInPostgres (conn))
				return Hash;

			Logging.GetLogging ().Info ("commit " + Hash + " not found - inserting");

			var row = new PostgresRow ();
			row.Set ("hash", NpgsqlTypes.NpgsqlDbType.Varchar, Hash);
			row.Set ("commitDate", NpgsqlTypes.NpgsqlDbType.Date, CommitDate);
			row.Set ("branch", NpgsqlTypes.NpgsqlDbType.Varchar, Branch);
			row.Set ("mergeBaseHash", NpgsqlTypes.NpgsqlDbType.Varchar, MergeBaseHash);
			return PostgresInterface.Insert<string> (conn, "commit", row, "hash");
		}
	}
}
