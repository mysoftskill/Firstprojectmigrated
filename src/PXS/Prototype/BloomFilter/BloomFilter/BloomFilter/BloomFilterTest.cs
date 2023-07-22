using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace BloomFilter
{
    class BloomFilterTestRig
    {
        const int _size = 50000000;
        const float _errorRate = .000001F; 

        public struct Puid
        {
            public int a, b, c, d;

            public Puid(int A, int B, int C, int D)
            {
                a = A;
                b = B;
                c = C;
                d = D;
            }

            public override int GetHashCode()
            {
                /*
                int hash = 17;
                hash = hash * 31 + a.GetHashCode();
                hash = hash * 31 + b.GetHashCode();
                hash = hash * 31 + c.GetHashCode();
                hash = hash * 31 + d.GetHashCode();
                return hash;
                */

                return a.GetHashCode() ^ b.GetHashCode() ^ c.GetHashCode() ^ d.GetHashCode();
            }
        }


        static void Main(string[] args)
        {
            BloomFilterTestRig bft = new BloomFilterTestRig();
            bft.Run();
        }

        void Run()
        {
            BloomFilterTest();
        }
        
        /// <summary>
        /// Hashes a 32-bit signed int using Thomas Wang's method v3.1 (https://www.concentric.net/~Ttwang/tech/inthash.htm).
        /// Runtime is suggested to be 11 cycles. 
        /// </summary>
        /// <param name="input">The integer to hash.</param>
        /// <returns>The hashed result.</returns>
        static int AlternateHash(int input)
        {
            unchecked
            {
                uint x = (uint)input;
                x = ~x + (x << 15); // x = (x << 15) - x- 1, as (~x) + y is equivalent to y - x - 1 in two's complement representation
                x = x ^ (x >> 12);
                x = x + (x << 2);
                x = x ^ (x >> 4);
                x = x * 2057; // x = (x + (x << 3)) + (x<< 11);
                x = x ^ (x >> 16);
                return (int)x;
            }

        }
        static int AlternateHashPuid(Puid p)
        {
            int hash = 0;

            //hash = Filter<int>.HashInt32(p.a) ^ Filter<int>.HashInt32(p.b) ^ Filter<int>.HashInt32(p.c) ^ Filter<int>.HashInt32(p.d);
            hash = AlternateHash(p.a) ^ AlternateHash(p.b) ^ AlternateHash(p.c) ^ AlternateHash(p.d);
            int otherHash = p.GetHashCode();
            return hash;
        }

        private void BloomFilterTest()
        {
            long startVm = Process.GetCurrentProcess().PeakVirtualMemorySize64;
            long startWs = Process.GetCurrentProcess().WorkingSet64;

            Filter<Puid> bloomFilter = new Filter<Puid>(_size, _errorRate, AlternateHashPuid);

            Random rand = new Random();
            Puid p0 = new Puid(1, 2, 3, 4);

            List<Puid> savedPuids = new List<Puid>();

            Stopwatch sw1 = Stopwatch.StartNew();
            for (int i = 0; i < _size; i++)
            {
                Puid p = new Puid(rand.Next(), rand.Next(), rand.Next(), rand.Next());
                bloomFilter.Add(p);

                if (i % 1000 == 0)
                {
                    savedPuids.Add(p);
                }
            }
            sw1.Stop();

            // do 4 lookups, don't time the first one, as it can be slow, due to JIT
            int c = 0;
            if (bloomFilter.Contains(p0))
                c++;

            Stopwatch sw2 = Stopwatch.StartNew();
            foreach (Puid p in savedPuids)
            {
                if (bloomFilter.Contains(p))
                {
                    c++;
                }
            }
            sw2.Stop();

            Debug.Assert(c == savedPuids.Count);
            double ave = sw1.Elapsed.TotalMilliseconds / _size;
            Console.WriteLine("bloom filter, inserted {0} items in {1} ms, time per item = {2} ms", _size, sw1.Elapsed.TotalMilliseconds, ave);
            ave = sw2.Elapsed.TotalMilliseconds / savedPuids.Count;
            Console.WriteLine("bloom filter, lookup {0} items in {1} ms, time per item = {2} ms", savedPuids.Count, sw2.Elapsed.TotalMilliseconds, ave);
            double mb = 1024 * 1024;
            long endVm = Process.GetCurrentProcess().PeakVirtualMemorySize64;
            long endWs = Process.GetCurrentProcess().WorkingSet64;
            long usedVm = endVm - startVm;
            long usedWs = endWs - startWs;
            Console.WriteLine("working set = {0} MB, {1} bytes/item", usedVm / mb, ((double)usedWs) / _size);

        }

        private void DictionaryTest()
        {
            long startVm = Process.GetCurrentProcess().PeakVirtualMemorySize64;
            long startWs = Process.GetCurrentProcess().WorkingSet64;

            // dictionary based
            Dictionary<Puid, string> d = new Dictionary<Puid, string>(_size + 1);
            Random rand = new Random();
            Puid p0 = new Puid(0, 0, 0, 0);

            List<Puid> savedPuids = new List<Puid>();

            Stopwatch sw1 = Stopwatch.StartNew();
            for (int i = 0; i < _size; i++)
            {
                Puid p = new Puid(rand.Next(), rand.Next(), rand.Next(), rand.Next());
                d.Add(p, null);

                if (i % 1000 == 0)
                {
                    savedPuids.Add(p);
                }
            }
            sw1.Stop();

            // do 4 lookups, don't time the first one, as it can be slow, due to JIT
            int c = 0;
            if (d.ContainsKey(p0))
                c++;

            Stopwatch sw2 = Stopwatch.StartNew();
            foreach (Puid p in savedPuids)
            {
                if (d.ContainsKey(p))
                {
                    c++;
                }
            }
            sw2.Stop();

            Debug.Assert(c == savedPuids.Count);
            double ave = sw1.Elapsed.TotalMilliseconds / _size;
            Console.WriteLine("dictionary, inserted {0} items in {1} ms, time per item = {2} ms", _size, sw1.Elapsed.TotalMilliseconds, ave);
            ave = sw2.Elapsed.TotalMilliseconds / savedPuids.Count;
            Console.WriteLine("dictionary, lookup {0} items in {1} ms, time per item = {2} ms", savedPuids.Count, sw2.Elapsed.TotalMilliseconds, ave);
            double mb = 1024 * 1024;
            long endVm = Process.GetCurrentProcess().PeakVirtualMemorySize64;
            long endWs = Process.GetCurrentProcess().WorkingSet64;
            long usedVm = endVm - startVm;
            long usedWs = endWs - startWs;
            Console.WriteLine("working set = {0} MB, {1} bytes/item", usedVm / mb, ((double)usedWs) / _size);
        }
    }
}
