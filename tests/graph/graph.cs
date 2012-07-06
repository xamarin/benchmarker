using System;
using System.Collections.Generic;

public class Node {
	Node n1, n2, n3, n4;
#if EDGE8
	Node n5, n6, n7, n8;
#endif

	public void connect (Node _n1, Node _n2, Node _n3, Node _n4
#if EDGE8
			, Node _n5, Node _n6, Node _n7, Node _n8
#endif
			     ) {
		n1 = _n1;
		n2 = _n2;
		n3 = _n3;
		n4 = _n4;
#if EDGE8
		n5 = _n5;
		n6 = _n6;
		n7 = _n7;
		n8 = _n8;
#endif
	}

	public int countNodes () {
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
#if EDGE8
			q.Enqueue (n.n5);
			q.Enqueue (n.n6);
			q.Enqueue (n.n7);
			q.Enqueue (n.n8);
#endif
		}
		*/
		return s.Count;
	}

	public static Node randomGraph (int size, Random r) {
		Node[] nodes = new Node [size];
		for (int i = 0; i < size; ++i)
			nodes [i] = new Node ();
		for (int i = 0; i < size; ++i)
			nodes [i].connect (nodes [r.Next (size)],
					nodes [r.Next (size)],
					nodes [r.Next (size)],
					nodes [r.Next (size)]
#if EDGE8
					, nodes [r.Next (size)],
					nodes [r.Next (size)],
					nodes [r.Next (size)],
					nodes [r.Next (size)]
#endif
					   );
		return nodes [0];
	}

	public static int Main () {
		Random r = new Random (31415);
		Node g = randomGraph (4000000, r);
		Console.WriteLine ("long graph constructed");
		for (int i = 0; i < 30; ++i)
			randomGraph (500000, r);
		Console.WriteLine ("done");
		Console.WriteLine ("nodes: " + g.countNodes ());
		return 0;
	}
}
