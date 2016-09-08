using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileSignature
{
    static class HashProcessor
    {
        public static void ReadFile(SyncQueue<Block> output, string filePath, long blockSize)
        {
            try
            {
                foreach (var block in FileReader.GetNextBlock(filePath, blockSize))
                {
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
