using System;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;

namespace PrimeGen
{
    
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 1 && args.Length < 3)
            {
                if (Int32.Parse(args[0]) % 8 == 0)
                {
                    var genPrimeNum = new PrimeNum();
                    if (args.Length == 1)
                    {
                        genPrimeNum.setupGen(Int32.Parse(args[0]));
                    }
                    else
                    {
                        genPrimeNum.setupGen(Int32.Parse(args[0]), Int32.Parse(args[1]));
                    }
                }
                else
                {
                    helpMsg();
                }
            }
            else
            {
                helpMsg();
            }
        }

        static void helpMsg()
        {
            
        }
        
    }

    class PrimeNum
    {
        public static object myLock = new Object();
        private int _numOfExec;

        public PrimeNum()
        {
            _numOfExec = 1;
        }

        public BigInteger generateNum(int bitLen)
        { 
            var rngCsp = new RNGCryptoServiceProvider();
            int numOfBytes = bitLen / 8;
            byte[] bytes = new byte[numOfBytes];
            rngCsp.GetBytes(bytes);
            var num = new BigInteger(bytes);
            num = BigInteger.Abs(num);
            if (!num.IsEven)
            {
                return num;
            }

            return -1;
        }

        public void findPrimeNum(int bitLen, int numOfPrime)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            // Use ParallelOptions instance to store the CancellationToken
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            try {
                Parallel.For(0, Int32.MaxValue, po, i =>
                {
                    var num = generateNum(bitLen);
                    if (num != -1)
                    {
                        if (num.IsProbablyPrime())
                        {
                            lock (myLock)
                            {
                                if (_numOfExec > numOfPrime)
                                {
                                    cts.Cancel();
                                    po.CancellationToken.ThrowIfCancellationRequested();
                                }
                                else if (_numOfExec < numOfPrime)
                                {
                                    Console.WriteLine("{0}: {1}", _numOfExec, num);
                                    Console.Write("\n");
                                    _numOfExec++;
                                }
                                else if (_numOfExec == numOfPrime)
                                {
                                    Console.WriteLine("{0}: {1}", _numOfExec, num);
                                    _numOfExec++;
                                }
                                
                            }
                        }
                    }
                });  
            }
            catch (OperationCanceledException e)
            { 
            }
            finally
            {
                cts.Dispose();
            }
        }

        public void setupGen(int bitLen, int numOfPrime = 1)
        {
            var timer = new Stopwatch();
            timer.Start();
            findPrimeNum(bitLen, numOfPrime);
            timer.Stop();
            Console.WriteLine("Time to Generate: {0}", timer.Elapsed);
        }
        
        public void findPrimeNumSeqential(int bitLen, int numOfPrime = 1)
        {
            var timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < numOfPrime; i++)
            {
                var num = generateNum(bitLen);
                Console.WriteLine("{0}: {1}", i, num);
            }
            timer.Stop();
            Console.WriteLine(timer.Elapsed);
        }
        
    }

    public static class MyExtensions
    {
        public static bool IsProbablyPrime(this BigInteger value, int k = 10)
        {
            var n = value;
            var d = n - 1;
            var r = 0;
            BigInteger a = default;
            var bytes = new byte[value.ToByteArray().Length];
            while (d % 2 == 0)
            {
                d = d / 2;
                r++;
            }

            for (int i = 0; i < k; i++)
            {
                do
                {
                    var gen = new Random();
                    gen.NextBytes(bytes);
                    a = new BigInteger(bytes);
                } while (a < 2 || a >= n - 2);

                var x = BigInteger.ModPow(a, d, n);
                if (x == 1 || x == n - 1)
                {
                    continue;
                }

                for (int j = 0; j < r - 1; j++)
                {
                    x = BigInteger.ModPow(x, 2, n);
                    if (x == 1)
                    {
                        return false;
                    } else if (x == n - 1)
                    {
                        break;
                    }
                }

                if (x != n - 1)
                {
                    return false;
                }
            }
            return true;
        } 
    }
}

