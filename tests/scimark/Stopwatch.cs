using System;

namespace SciMark2
{
	public class Stopwatch
	{
		private bool running;
		private double last_time;
		private double total;

		public Stopwatch()
		{
			this.reset();
		}

		public static double seconds()
		{
			return (double) DateTime.Now.Ticks * 1E-07;
		}

		public virtual void reset()
		{
			this.running = false;
			this.last_time = 0.0;
			this.total = 0.0;
		}

		public virtual void start()
		{
			if (this.running)
				return;
			this.running = true;
			this.total = 0.0;
			this.last_time = Stopwatch.seconds();
		}

		public virtual void resume()
		{
			if (this.running)
				return;
			this.last_time = Stopwatch.seconds();
			this.running = true;
		}

		public virtual double stop()
		{
			if (this.running)
			{
				this.total += Stopwatch.seconds() - this.last_time;
				this.running = false;
			}
			return this.total;
		}

		public virtual double read()
		{
			if (this.running)
			{
				this.total += Stopwatch.seconds() - this.last_time;
				this.last_time = Stopwatch.seconds();
			}
			return this.total;
		}
	}
}

