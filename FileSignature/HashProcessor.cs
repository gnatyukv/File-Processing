using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileSignature
{
    static class HashProcessor
    {
        private static bool fault;

        public static void ReadFile(SyncQueue<Block> output, string filePath, long blockSize)
        {
            try
            {
                foreach (var block in FileReader.GetNextBlock(filePath, blockSize))
                {
                    if (fault) break; // stop if any errors occurred while consuming
                    output.Enqueue(block);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                output.Close();
            }
        }

        public static void ComputeHashes(SyncQueue<Block> input, SyncQueue<Block> output)
        {
            try
            {
                Block block;
                while (input.TryDequeue(out block))
                {
                    block.ComputeHash();
                    output.Enqueue(block);
                }
            }
            catch (Exception ex)
            {
                fault = true; // signal producer to stop
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                output.Close();
            }
        }

        public static void MultiplexProducers(SyncQueue<Block>[] input)
        {
            try
            {
                var multiplexer = new Multiplexer<Block>();

                Block block;
                while (multiplexer.MonitorProducers(input, out block))
                {
                    PrintHash(block);
                    if (GC.GetTotalMemory(false) > Program.MemoryLimit)
                    {
                        ReleaseResources();
                    }
                }
            }
            catch (Exception ex)
            {
                fault = true; // signal producer to stop
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                ReleaseResources();
            }
        }

        private static void PrintHash(Block block)
        {
            Console.WriteLine(block.ToString());
        }

        private static void ReleaseResources()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
