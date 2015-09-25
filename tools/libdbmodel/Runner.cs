using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Benchmarker.Common.Models;

namespace Benchmarker.Common
{
	public interface Runner
	{
		Result.Run Run (string profilesDirectory, string profileFilename, out bool timedOut);
		Result.Run Run (out bool timedOut);
	}
}
