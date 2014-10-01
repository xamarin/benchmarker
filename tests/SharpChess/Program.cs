using System;
using System.Threading;

using SharpChess.Model;

/* compile : mcs -r:SharpChess.Model.dll Program.cs */

class Program
{
	public static void Main (string[] args)
	{
		if (args.Length < 2) {
			Console.WriteLine ("usage: gamefile searchdepth");
			Environment.Exit (1);
		}

		string strPath = args [0];
		if (Game.Load(strPath))
			Console.WriteLine("Loaded save game: " + strPath);
		else
			throw new ArgumentException("Unable to load save game: " + strPath);

		Game.MaximumSearchDepth = Int32.Parse (args [1]);

		Game.SuspendPondering();
		Game.PlayerToPlay.OpposingPlayer.Clock.Stop();
		Game.PlayerToPlay.Intellegence = Player.PlayerIntellegenceNames.Computer;
		Game.PlayerToPlay.OpposingPlayer.Intellegence = Player.PlayerIntellegenceNames.Human;
		Game.PlayerToPlay.Clock.Stop();
		Game.PlayerToPlay.Clock.Start();
		Game.PlayerToPlay.Brain.StartThinking();

		while (Game.PlayerToPlay.Brain.IsThinking && !Game.PlayerToPlay.Brain.IsPondering)
			System.Threading.Thread.Sleep (1);
	}
}
