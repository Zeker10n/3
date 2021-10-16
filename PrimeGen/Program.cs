/*
 * Author: Tristan Hoenninger
 * Csci 251
 * This program generates and prints a specified amount of prime numbers,
 * the bit length of the numbers, and the time it took to generate the numbers
 * in a parallel manner.
 * This program can take in two variables from the
 * command line with one being optional, the bit length of the numbers to
 * generate, and the amount of numbers to generate. If no amount is entered it
 * will default to 1. If the user doesn't enter a bit length that is a multiple of 8
 * and greater than or equal to 32 it will print out a help message. If the user
 * gives an invalid amount of arguments it will print a help message.
 */
using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;


namespace PrimeGen
{
    
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 1 && args.Length < 3)
            {
                var bitlen = Int32.Parse(args[0]);
                if ( bitlen % 8 == 0 && bitlen >= 32)
                {
                    var genPrimeNum = new PrimeNum();
                    if (args.Length == 1)
                    {
                        genPrimeNum.SetupGen(bitlen);
                    }
                    else
                    {
                        genPrimeNum.SetupGen(bitlen, Int32.Parse(args[1]));
                    }
                }
                else
                {
                    HelpMsg();
                }
            }
            else
            {
                HelpMsg();
            }
        }
        
        /// <summary>
        /// Prints a help message when given invalid arguments.
        /// </summary>
        static void HelpMsg()
        {
            Console.WriteLine("dotnet run <bits> <count=1>");
            Console.WriteLine("- bits - the number of bits of the prime number, this must be a".PadLeft(67));
            Console.WriteLine("multiple of 8, and at least 32 bits.".PadLeft(42));
            Console.WriteLine("- count - the number of prime numbers to generate, defaults to 1".PadLeft(68));
        }
        
    }

    class PrimeNum
    {
        public static object myLock = new Object();
        private int _numOfExec;
        
        /// <summary>
        /// Instantiates a new instance of PrimeNum.
        /// </summary>
        public PrimeNum()
        {
            _numOfExec = 1;
        }
        
        /// <summary>
        /// Generates a random BigInteger and returns it if its even and not divisible by 3, 5, or 7
        /// and returns a -1 if it doesn't meet that criteria. 
        /// </summary>
        /// <param name="bitLen">The bit length of the BigInteger to generate</param>
        /// <returns>Returns the generated number if its even and not divisible by 3, 5, or 7.
        /// It returns -1 if these criteria aren't meet.</returns>
        public BigInteger GenerateNum(int bitLen)
        { 
            var rngCsp = new RNGCryptoServiceProvider();
            int numOfBytes = bitLen / 8;
            byte[] bytes = new byte[numOfBytes];
            rngCsp.GetBytes(bytes);
            var num = new BigInteger(bytes);
            num = BigInteger.Abs(num);
            if (!num.IsEven && num % 5 != 0 && num % 3 != 0 && num % 7 != 0 )
            {
                return num;
            }

            return -1;
        }
        
        /// <summary>
        /// Generates and prints a specified number of prime numbers as it finds them and does so in a parallel method.
        /// Cancels the threads once the specified number of prime numbers have been printed.
        /// </summary>
        /// <param name="bitLen">The bit length of the BigInteger object(s) that will be generated</param>
        /// <param name="numOfPrime">The number of prime numbers that need to be generated</param>
        public void FindPrimeNum(int bitLen, int numOfPrime)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            // Use ParallelOptions instance to store the CancellationToken
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            try
            {
                Parallel.For(0, Int32.MaxValue, po, (i, state) =>
                    {
                        if (po.CancellationToken.IsCancellationRequested && _numOfExec > numOfPrime)
                        {
                            po.CancellationToken.ThrowIfCancellationRequested();
                            state.Break();
                        }
                        else
                        {
                            var num = GenerateNum(bitLen);
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
                        }
                    });
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        /// <summary>
        /// Generates and prints a specified number of prime numbers, prints the bit length of numbers, and
        /// time it took to generate the numbers.
        /// </summary>
        /// <param name="bitLen">The bit length of the BigInteger object(s) that will be generated</param>
        /// <param name="numOfPrime">The number of prime numbers that need to be generated, defaults to 1</param>
        public void SetupGen(int bitLen, int numOfPrime = 1)
        {
            Console.WriteLine("BitLength: {0} bits", bitLen);
            var timer = new Stopwatch();
            timer.Start();
            FindPrimeNum(bitLen, numOfPrime);
            timer.Stop();
            Console.WriteLine("Time to Generate: {0}", timer.Elapsed);
        }
        
        
    }

    
    public static class MyExtensions
    {
        /// <summary>
        /// Checks if the given value is prime using k number with the range of [2, value - 2] against value. 
        /// </summary>
        /// <param name="value"> The given BigInteger to check if it's prime</param>
        /// <param name="k">The number of checks done on the number to see if it's prime</param>
        /// <returns>Returns true if teh number is prime and returns false if number is not prime</returns>
        public static bool IsProbablyPrime(this BigInteger value, int k = 10)
        {
            var n = value;
            var d = n - 1;
            var r = 0;
            var bytes = new byte[value.ToByteArray().Length];
            while (d % 2 == 0)
            {
                d = d / 2;
                r++;
            }

            for (int i = 0; i < k; i++)
            {
                BigInteger a = default;
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

