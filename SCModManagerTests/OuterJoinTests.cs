using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SCModManager.Avalonia.Utility;

namespace SCModManagerTests
{
    [TestClass]
    public class OuterJoinTests
    {
        [TestMethod]
        public void TestInternalDifferences()
        {
            var l1 = new[] {1, 2, 4, 5, 6};
            var l2 = new[] {1, 2, 3, 5, 6};

            var (removed, added) = l1.OuterJoin(l2, (i, j) => i - j);

            Assert.AreEqual(1, removed.Count);
            Assert.AreEqual(1, added.Count);

            Assert.AreEqual(3, added.First());
            Assert.AreEqual(4, removed.First());
        }


        [TestMethod]
        public void TestExternalDifferences()
        {
            var l1 = new[] { 2, 3, 4, 5, 6 };
            var l2 = new[] { 1, 2, 3, 4, 5 };

            var (removed, added) = l1.OuterJoin(l2, (i, j) => i - j);

            Assert.AreEqual(1, removed.Count);
            Assert.AreEqual(1, added.Count);

            Assert.AreEqual(1, added.First());
            Assert.AreEqual(6, removed.First());
        }

        [TestMethod]
        public void TestNoDifferences()
        {
            var l1 = new[] { 1, 2, 3, 4, 5, 6 };
            var l2 = new[] { 1, 2, 3, 4, 5, 6 };

            var (removed, added) = l1.OuterJoin(l2, (i, j) => i - j);

            Assert.AreEqual(0, removed.Count);
            Assert.AreEqual(0, added.Count);
        }
    }
}
