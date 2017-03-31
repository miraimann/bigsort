using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;
using Bigsort.Implementation;
using NUnit.Framework;

namespace Bigsort.Tests
{
    [TestFixture]
    public class FragmentsPoolTests
    {
        private const int ArraySize = 20;
        private IPoolMaker _poolMaker;
        private IFragmentsPool<byte> _pool;
        private byte[] _array; 

        [SetUp]
        public void Setup()
        {
            _poolMaker = new PoolMaker();
            _array = new byte[ArraySize];
            _pool = _poolMaker
                .MakeFragmentsPool(_array);
        }

        [Test]
        public void Test1()
        {
            Check(_pool.TryGet(3), 0, 3);
            Check(_pool.TryGet(2), 3, 2);
            Check(_pool.TryGet(3), 5, 3);
            Check(_pool.TryGet(2), 8, 2);
            Check(_pool.TryGet(10), 10, 10);
            Assert.IsNull(_pool.TryGet(1));
        }

        [Test]
        public void Test2()
        {
            Check(_pool.TryGet(3), 0, 3);
            Check(_pool.TryGet(2), 3, 2);

            var x = _pool.TryGet(3);
            Check(x, 5, 3);
            x.Dispose();
            
            Check(_pool.TryGet(2), 5, 2);
            Check(_pool.TryGet(2), 7, 2);
        }

        [Test]
        public void Test3()
        {
            var a = _pool.TryGet(3);
            Check(a, 0, 3);
            
            Check(_pool.TryGet(2), 3, 2);
            Check(_pool.TryGet(2), 5, 2);
            
            a.Dispose();
            
            Check(_pool.TryGet(3), 0, 3);
            Check(_pool.TryGet(2), 7, 2);
        }

        [Test]
        public void Test4()
        {
            Check(_pool.TryGet(3), 0, 3);

            var b = _pool.TryGet(2);
            Check(b, 3, 2);

            var c = _pool.TryGet(2);
            Check(c, 5, 2);
            
            Check(_pool.TryGet(2), 7, 2);
            
            b.Dispose();
            c.Dispose();

            Check(_pool.TryGet(4), 3, 4);
        }

        [Test]
        public void Test5()
        {
            Check(_pool.TryGet(3), 0, 3);

            var b = _pool.TryGet(2);
            Check(b, 3, 2);

            var c = _pool.TryGet(3);
            Check(c, 5, 3);

            Check(_pool.TryGet(2), 8, 2);

            c.Dispose();
            b.Dispose();

            Check(_pool.TryGet(5), 3, 5);
        }

        [Test]
        public void Test6()
        {
            Check(_pool.TryGet(2), 0, 2);

            var a = _pool.TryGet(3);
            Check(a, 2, 3);

            var b = _pool.TryGet(2);
            Check(b, 5, 2);

            var c = _pool.TryGet(3);
            Check(c, 7, 3);

            Check(_pool.TryGet(2), 10, 2);

            a.Dispose();
            c.Dispose();
            b.Dispose();

            Check(_pool.TryGet(7), 2, 7);
        }

        private void Check(
            IPooled<ArrayFragment<byte>> result,
            int expectedOffset,
            int expectedLength)
        {
            Assert.IsNotNull(result);
            Assert.AreSame(_array, result.Value.Array);
            Assert.AreEqual(expectedOffset, result.Value.Offset);
            Assert.AreEqual(expectedLength, result.Value.Count);
        }
    }
}
