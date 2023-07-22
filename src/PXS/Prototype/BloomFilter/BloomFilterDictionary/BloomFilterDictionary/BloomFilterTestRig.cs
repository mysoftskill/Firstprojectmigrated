using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BloomFilter
{
    class BloomFilterTestRig
    {
        const int _size = 75000000;

        struct Puid
        {
            int a, b, c, d;

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
            DictionaryTest();
        }

        private void BloomFilterTest()
        {
        }

        private void DictionaryTest()
        {
            long startVm = Process.GetCurrentProcess().PeakVirtualMemorySize64;
            long startWs = Process.GetCurrentProcess().WorkingSet64; 

            // dictionary based
            Dictionary<Puid, string> d = new Dictionary<Puid, string>(_size + 1);
            Random rand = new Random();
            Puid p0 = new Puid(1, 2, 3, 4);

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
