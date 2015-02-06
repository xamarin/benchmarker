/*
 * Copyright (c) 2008 Standard Performance Evaluation Corporation (SPEC)
 * All rights reserved.
 *
 * Copyright (c) 1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * Modified by Kaivalya M. Dixit & Don McCauley (IBM) to read input files This
 * source code is provided as is, without any express or implied warranty.
 */

using Benchmark;
using System.Diagnostics;
using System.IO;
using System;

public class BenchmarkMain
{
	public static String [] FILES_NAMES = new String []
	{
		"input/202.tar",
		"input/205.tar",
		"input/208.tar",
		"input/209.tar",
		"input/210.tar",
		"input/211.tar",
		"input/213x.tar",
		"input/228.tar",
		"input/239.tar",
		"input/misc.tar"
	};

	public static readonly int FILES_NUMBER = FILES_NAMES.Length;
	public static readonly int LOOP_COUNT = 2;
	public static Source [] SOURCES;
	public static byte [] COMPRESS_BUFFERS;
	public static byte [] DECOMPRESS_BUFFERS;
	public static Compress CB;

	public void Run ()
	{
		for (int i = 0; i < LOOP_COUNT; i++)
		{
			for (int j = 0; j < FILES_NUMBER; j++)
			{
				Source source = SOURCES [j];
				OutputBuffer comprBuffer, decomprBufer;
				comprBuffer = Compress.PerformAction (source.Buffer, source.Length, Benchmark.Compress.COMPRESS, COMPRESS_BUFFERS);
				decomprBufer = Compress.PerformAction (COMPRESS_BUFFERS, comprBuffer.Length, Benchmark.Compress.UNCOMPRESS, DECOMPRESS_BUFFERS);
				Console.Out.Write (source.Length + " " + source.CRC + " ");
				Console.Out.Write (comprBuffer.Length + " " + comprBuffer.CRC + " ");
				Console.Out.WriteLine (decomprBufer.Length + " " + decomprBufer.CRC);
			}
		}
	}

	public static void Main ()
	{
		var self = new BenchmarkMain ();
		var stopwatch = new Stopwatch ();
		PrepareBuffers ();
		stopwatch.Start ();
		self.Run ();
		stopwatch.Stop ();
		Console.Out.WriteLine (stopwatch.ElapsedMilliseconds);
	}

	static void PrepareBuffers ()
	{
		CB = new Compress ();
		SOURCES = new Source [FILES_NUMBER];
		for (int i = 0; i < FILES_NUMBER; i ++)
			SOURCES [i] = new Source (FILES_NAMES [i]);
		DECOMPRESS_BUFFERS = new byte [Source.MAX_LENGTH];
		COMPRESS_BUFFERS = new byte [Source.MAX_LENGTH];
	}

	public class Source
	{
		private byte [] buffer;
		private long crc;
		private int length;
		public static int MAX_LENGTH;

		public Source (String fileName)
		{
			buffer = FillBuffer (fileName);
			length = buffer.Length;
			MAX_LENGTH = Math.Max (length, MAX_LENGTH);
			CRC32 crc32 = new CRC32();
			crc32.Update (buffer, length);
			crc = crc32.Value;
		}

		internal long CRC { get { return crc; } }
		internal int Length { get { return length; } }
		internal byte [] Buffer { get { return buffer; } }

		private byte [] FillBuffer (String fileName)
		{
			return File.ReadAllBytes (fileName);
		}
	}
}
