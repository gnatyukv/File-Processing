using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace FileSignature
{
    class Program
    {
        public static long MemoryLimit { get; private set; }

        static DateTime beginTime;
        static DateTime endTime;

        static void Main(string[] args)
        {
            string filePath;
            long blockSize;
            if (!ValidateArgs(args, out filePath, out blockSize))
            {
                Console.ReadLine();
                return;
            }

            MemoryLimit = 100000000; // B

            int threadCount;
            int boundedCap;
            SetPipelineParams(blockSize, out threadCount, out boundedCap);

            var dataBlocks = new SyncQueue<Block>(boundedCap);
            var blocksToCompute = new SyncQueue<Block>[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                blocksToCompute[i] = new SyncQueue<Block>(boundedCap);
            }

            beginTime = DateTime.Now;

            StartPipeline(filePath, blockSize, threadCount, dataBlocks, blocksToCompute);

            endTime = DateTime.Now;
            Console.WriteLine("\nRun time: {0} s", (endTime - beginTime).Duration().TotalSeconds);

            Console.ReadLine();
        }

        static void StartPipeline(string filePath, long blockSize, int threadCount, SyncQueue<Block> dataBlocks, SyncQueue<Block>[] blocksToCompute)
        {
            var computeStages = new Thread[threadCount];
            var readStage = new Thread(() => HashProcessor.ReadFile(dataBlocks, filePath, blockSize));
            readStage.Start();
            for (int i = 0; i < threadCount; i++)
            {
                // avoid capturing 'i' variable in an anonymous method
                int k = i;
                computeStages[i] = new Thread(() => HashProcessor.ComputeHashes(dataBlocks, blocksToCompute[k]));
                computeStages[i].Start();
            }
            var multiplexStage = new Thread(() => HashProcessor.MultiplexProducers(blocksToCompute));
            multiplexStage.Start();

            readStage.Join();
            foreach (var stage in computeStages)
            {
                stage.Join();
            }
            multiplexStage.Join();
        }

        static void SetPipelineParams(long blockSize, out int threadCount, out int boundedCap)
        {
            threadCount = Environment.ProcessorCount;

            long longBoundedCap = MemoryLimit / (threadCount * blockSize);
            if (longBoundedCap < 1)
                longBoundedCap = 1;
            else if (longBoundedCap > 1024)
                longBoundedCap = 1024;
            boundedCap = (int)longBoundedCap;

            while (threadCount * blockSize >= MemoryLimit)
            {
                if (threadCount == 1) break;
                threadCount--;
            }
        }

        static bool ValidateArgs(string[] args, out string filePath, out long blockSize)
        {
            if (args.Length != 2)
            {
                if (args.Length < 2)
                    Console.WriteLine("Not enough command line arguments.");
                else
                    Console.WriteLine("Too many command line arguments.");
            }
            else
            {
                try
                {
                    blockSize = long.Parse(args[1]);
                    if (blockSize < 1)
                        throw new ArgumentOutOfRangeException("blockSize", "Value must be positive.");
                    else
                    {
                        filePath = args[0];
                        return true;
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Console.WriteLine("ArgumentOutOfRangeException: {0}", ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: {0}", ex.Message);
                }
            }
            filePath = null;
            blockSize = 0;
            return false;
        }
    }
}
