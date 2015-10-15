using System;
using System.Collections.Generic;
using Common.Logging;

namespace Benchmarks.Graph4
{
	public class Node
	{
		Node n1, n2, n3, n4;

		public void connect (Node _n1, Node _n2, Node _n3, Node _n4)
		{
			n1 = _n1;
			n2 = _n2;
			n3 = _n3;
			n4 = _n4;
		}

		public int countNodes ()
		{
			Queue<Node> q = new Queue<Node> ();
			HashSet<Node> s = new HashSet<Node> ();
			/*
		q.Enqueue (this);
		while (q.Count > 0) {
			Node n = q.Dequeue ();
			if (s.Contains (n))
				continue;
			s.Add (n);
			q.Enqueue (n.n1);
			q.Enqueue (n.n2);
			q.Enqueue (n.n3);
			q.Enqueue (n.n4);
		}
		*/
			return s.Count;
		}

		static void connectNode (Node n, Node[] nodes, Random r)
		{
			int size = nodes.Length;
			n.connect (nodes [r.Next (size)],
				nodes [r.Next (size)],
				nodes [r.Next (size)],
				nodes [r.Next (size)]
			);
		}

		public static Node randomGraph (int size, Random r)
		{
			Node[] nodes = new Node [size];
			for (int i = 0; i < size; ++i)
				nodes [i] = new Node ();
			for (int i = 0; i < size; ++i)
				connectNode (nodes [i], nodes, r);
			return nodes [0];
		}

		public static Node otherRandomGraph (int size, int bufsize, Random r)
		{
			int i;
			Node[] buffer = new Node [bufsize];
			Node n = null;

			for (i = 0; i < bufsize; ++i)
				n = buffer [i] = new Node ();
			for (i = 0; i < bufsize; ++i)
				connectNode (buffer [i], buffer, r);
			for (; i < size; ++i) {
				n = new Node ();
				int j = r.Next (bufsize);
				buffer [j] = n;
				connectNode (n, buffer, r);
			}
			/* in case of pinning */
			for (i = 0; i < bufsize; ++i)
				buffer [i] = null;
			return n;
		}

		public static int Main (ILog logger)
		{
			Random r = new Random (31415);
			//Node g = otherRandomGraph (8000000, 128, r);
			Node g = randomGraph (200000, r);
			logger.InfoFormat ("long graph constructed");
			for (int i = 0; i < 30; ++i) {
				//otherRandomGraph (1000000, 128, r);
				randomGraph (20000, r);
			}
			logger.InfoFormat ("done");
			logger.InfoFormat ("nodes: " + g.countNodes ());
			return 0;
		}
	}
}
