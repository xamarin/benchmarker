
using System;
using System.IO;
using System.Text;

namespace Benchmarks.Perimeter
{
	/**
 * A class representing the South East quadrant of the image
 */
	public class SouthEast : Quadrant
	{
		/**
	 * Return true iff this quadrant is adjacent to the boundary
	 * of an image in the given direction.
	 *
	 * @param direction the image boundary
	 * @return true if the quadrant is adjacent, false otherwise.
	 */
		public override bool adjacent (int direction)
		{
			return (direction == QuadTreeNode.SOUTH || direction == QuadTreeNode.EAST);
		}

		/**
	 * Return the quadrant of a block of equal size that is
	 * adjacent to the given side of this quadrant.
	 *
	 * @param direction the image boundary
	 * @return the reflected quadrant
	 */
		public override Quadrant reflect (int direction)
		{
			if (direction == QuadTreeNode.WEST || direction == QuadTreeNode.EAST) {
				return Quadrant.cSouthWest;
			}
			return Quadrant.cNorthEast;
		}

		/**
	 * Return the child that represents this quadrant of the given
	 * node.
	 *
	 * @param node the node that we want the child from.
	 * @return the child node representing this quadrant
	 */
		public override QuadTreeNode child (QuadTreeNode node)
		{
			return node.getSouthEast ();
		}
	}
}
