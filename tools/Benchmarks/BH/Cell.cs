
using System;
using System.Collections.Generic;

/**
 * A class used to represent internal nodes in the tree
 */
public class Cell : Node
{
	// subcells per cell
	public static int NSUB;

	/**
	 * The children of this cell node.  Each entry may contain either
	 * another cell or a body.
	 */
	public Node[] subp;

	public static void initCell()
	{
		Cell.NSUB = 8;  // 1 << NDIM
	}

	public static Cell makeCell()
	{
		Cell c = new Cell();
		c.initNode();
		c.subp = new Node[NSUB];
		for(int i = 0; i < Cell.NSUB; ++i)
			c.subp[i] = null;

		return c;
	}

	/**
	 * Descend Tree and insert particle.  We're at a cell so
	 * we need to move down the tree.
	 *
	 * @param p    the body to insert into the tree
	 * @param xpic
	 * @param l
	 * @param tree the root of the tree
	 * @return the subtree with the new body inserted
	 */
	public override Cell loadTree(Body p, MathVector xpic, int l, BTree tree)
	{
		// move down one level
		int si = Node.oldSubindex(xpic, l);
		Node rt = subp[si];
		if(rt != null)
			subp[si] = rt.loadTree(p, xpic, l >> 1, tree);
		else
			subp[si] = p;

		return this;
	}

	/**
	 * Descend tree finding center of mass coordinates
	 *
	 * @return the mass of this node
	 */
	public override double hackcofm()
	{
		double mq = 0.0;
		MathVector tmpPos = MathVector.makeMathVector();
		MathVector tmpv = MathVector.makeMathVector();
		for(int i = 0; i < NSUB; i++)
		{
			Node r = this.subp[i];
			if(r != null)
			{
				double mr = r.hackcofm();
				mq = mr + mq;
				tmpv.multScalar2(r.pos, mr);
				tmpPos.addition(tmpv);
			}
		}
		mass = mq;
		pos = tmpPos;
		pos.divScalar(mass);

		return mq;
	}

	/**
	 * Recursively walk the tree to do hackwalk calculation
	 */
	public override HG walkSubTree(double dsq, HG hg)
	{
		if(subdivp(dsq, hg))
		{
			for(int k = 0; k < Cell.NSUB; k++)
			{
				Node r = this.subp[k];
				if(r != null)
					hg = r.walkSubTree(dsq / 4.0, hg);
			}
		}
		else
			hg = gravSub(hg);

		return hg;
	}

	/**
	 * Decide if the cell is too close to accept as a single term.
	 *
	 * @return true if the cell is too close.
	 */
	public bool subdivp(double dsq, HG hg)
	{
		MathVector dr = MathVector.makeMathVector();
		dr.subtraction2(pos, hg.pos0);
		double drsq = dr.dotProduct();

		// in the original olden version drsp is multiplied by 1.0
		return (drsq < dsq);
	}
}



