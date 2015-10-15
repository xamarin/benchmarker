
using System;
using System.IO;
using System.Text;

namespace Benchmarks.Perimeter
{
	/**
 * A class to represent a black quad tree node in the image.
 * A black node represents a block of an image that contains only 1's.
 */
	public class BlackNode : QuadTreeNode
	{

		/**
	 * Construct a <tt>black</tt> quad tree node.
	 *
	 * @param quadrant the quadrant that this node represents
	 * @param the      parent quad tree node
	 */
		public BlackNode (Quadrant quadrant, QuadTreeNode parent)
			: base (quadrant, parent)
		{
			;
		}

		/**
	 * Compute the perimeter for a black node.
	 *
	 * @param size
	 */
		public override int perimeter (int size)
		{
			int retval = 0;
			// North
			QuadTreeNode neighbor = gtEqualAdjNeighbor (QuadTreeNode.NORTH);
			if (neighbor == null || neighbor is WhiteNode) {
				retval += size;
			} else if (neighbor is GreyNode) {
				retval += neighbor.sumAdjacent (Quadrant.cSouthEast, Quadrant.cSouthWest, size);
			} else {
				;
			}

			// East
			neighbor = gtEqualAdjNeighbor (QuadTreeNode.EAST);
			if (neighbor == null || neighbor is WhiteNode) {
				retval += size;
			} else if (neighbor is GreyNode) {
				retval += neighbor.sumAdjacent (Quadrant.cSouthWest, Quadrant.cNorthWest, size);
			} else {
				;
			}

			// South
			neighbor = gtEqualAdjNeighbor (QuadTreeNode.SOUTH);
			if (neighbor == null || neighbor is WhiteNode) {
				retval += size;
			} else if (neighbor is GreyNode) {
				retval += neighbor.sumAdjacent (Quadrant.cNorthWest, Quadrant.cNorthEast, size);
			} else {
				;
			}

			// West
			neighbor = gtEqualAdjNeighbor (QuadTreeNode.WEST);
			if (neighbor == null || neighbor is WhiteNode) {
				retval += size;
			} else if (neighbor is GreyNode) {
				retval += neighbor.sumAdjacent (Quadrant.cNorthEast, Quadrant.cSouthEast, size);
			} else {
				;
			}

			return retval;
		}

		/**
	 * Sum the perimeter of all white leaves in the two specified
	 * quadrants of the sub quad tree rooted at this node.
	 *
	 * @param quad1 the first specified quadrant
	 * @param quad2 the second specified quadrant
	 * @param size  the size of the image represented by this node.
	 * @return the perimeter of the adjacent nodes
	 */
		public override int sumAdjacent (Quadrant quad1, Quadrant quad2, int size)
		{
			return 0;
		}
	}
}
