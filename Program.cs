using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lab16
{
  
    class Program
    {

        public static List<uint> SieveEratosthenes(uint n) // Решето Эратосфена прямиком из тырнетиков
        {
            var numbers = new List<uint>();
            //заполнение списка числами от 2 до n-1
            for (var i = 2u; i < n; i++)
            {
                numbers.Add(i);
            }

            for (var i = 0; i < numbers.Count; i++)
            {
                for (var j = 2u; j < n; j++)
                {
                    //удаляем кратные числа из списка
                    numbers.Remove(numbers[i] * j);
                }
            }

            return numbers;
        }

        public static ulong Factorial(uint number)
        {

            if (number == 1 || number == 0)
                return 1;
            else
                return (ulong)(number * Factorial(number - 1));

        }

        public static double Power(double number, int power)
        {
            double rez = number;
            if (power == 0) return 1;
            for (int i = 0; i < power-1; i++)
            {
                rez *= number;
            }
            return rez;
        }

        public static double Cos(double x)
        {
            double cos = 0;
            short sign;
            for (uint i = 0; i < 31; i++)
            {
                sign = (i) % 2  == 0 ? (short)1 : (short)-1;
                var fact = new Task<ulong>(() => Factorial(2 * i));
                var pow = new Task<double>(() => Power(x, 2 * (int)i));
                //Console.Write($"{i} tasks started\t");
                fact.Start();
                pow.Start();
                Task.WaitAll(fact, pow);
                cos += sign * pow.Result/fact.Result;
                if (i%10 == 0) 
                    Console.WriteLine($"{i}: " + cos);
            }
            
            return cos;

        }

        public static async Task<double> CosAsync(double x)
        {
            double cos = 0;
            short sign;
            for (uint i = 0; i < 31; i++)
            {
                sign = (i) % 2 == 0 ? (short)1 : (short)-1;
                var fact = new Task<ulong>(() => Factorial(2 * i));
                var pow = new Task<double>(() => Power(x, 2 * (int)i));
                
                fact.Start();
                pow.Start();

                var factRez = await fact;
                var powRez = await pow;
                cos += sign * (powRez/factRez);
                
            }

            return cos;
            //Console.WriteLine(cos);

        }
        static void Main(string[] args)
        {

            var primeNums = new Task<List<uint>>(() => SieveEratosthenes(15000));
            var watch = new Stopwatch();
            watch.Start();
            primeNums.Start();
            Console.WriteLine(primeNums.Id + " status: " + primeNums.Status.ToString());
            Task.WaitAll(primeNums);
            watch.Stop();
            Console.WriteLine(primeNums.Id + " status: " + primeNums.Status.ToString());
            File.WriteAllText("PrimeNumbers.txt", string.Join(" ", primeNums.Result));
            Console.WriteLine(watch.Elapsed.TotalSeconds);

            watch.Restart();
            SieveEratosthenes(15000);
            watch.Stop();
            Console.WriteLine(watch.Elapsed.TotalSeconds);

            var cancellationTokenSource = new CancellationTokenSource();
            primeNums = new Task<List<uint>>(() => SieveEratosthenes(30000), cancellationTokenSource.Token);
            primeNums.Start();
            Thread.Sleep(1000);
            Console.WriteLine(primeNums.Id + " status: " + primeNums.Status.ToString());
            cancellationTokenSource.Cancel();
            Thread.Sleep(1000);
            Console.WriteLine(primeNums.Id + " status: " + primeNums.Status.ToString());


            var cos = new Task<double>(() => Cos(1.3)); // На бОльших числах почему-то выдает неправильный результат.

            cos.Start();
            Task.WaitAll(cos);
            Console.WriteLine(cos.Result);
            Console.WriteLine(Math.Cos(1.3));

            var pow = new Task<double>(() => Power(1.3, 2));
            var wait = pow.GetAwaiter();
            wait.OnCompleted(() => { Console.WriteLine(wait.GetResult()); });

            //Action<double> act = delegate (double i){ Console.WriteLine(i); };
            var lists = new List<List<int>>();
            pow.ContinueWith((i) => cos);

            Console.WriteLine("\n====\n");
            var rand = new Random();
            for (int j = 0; j < 10; j++)
            {
                lists.Add(new List<int>());
            }
            watch.Restart();
            Parallel.For(0, 1000000, (i) =>
            {
                for (int j = 0; j < 10; j++)
                {
                    lists[j].Add(rand.Next());
                }
            });
            watch.Stop();

            Console.WriteLine(watch.Elapsed.TotalSeconds.ToString());


            lists.Clear();
            for (int j = 0; j < 10; j++)
            {
                lists.Add(new List<int>());
            }
            watch.Restart();
            for(int i = 0; i < 1000000; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    lists[j].Add(rand.Next());
                }
            }
            watch.Stop();
            
            Console.WriteLine(watch.Elapsed.TotalSeconds.ToString());
            Console.WriteLine("\n====\n");

            Parallel.Invoke(
            ()=>
                {
                    for (int i = 0; i < 5; i++)
                        Console.WriteLine($"Method 1: {i}");
                },
            () =>
                {
                    for (int i = 0; i < 5; i++)
                        Console.WriteLine($"Method 2: {i}");
                },
            () =>
                {
                for (int i = 0; i < 5; i++)
                    Console.WriteLine($"Method 3: {i}");
                }
            );
            Console.WriteLine("\n====\n");

            var warehouse = new BlockingCollection<int>();

            for (int i = 0; i < 5; i++)
            {
                new Thread(() => Producer(i, warehouse)).Start();
                Thread.Sleep(10);
            }
            for (int i = 0; i < 10; i++)
            {
                new Thread(() => Consumer(i, warehouse)).Start();
                Thread.Sleep(10);
            }

            Thread.Sleep(15000);
            
            Console.WriteLine("\n====\n");
            Console.WriteLine(CosAsync(1.3).Result);
            Thread.Sleep(2000);
            Console.WriteLine("Press any key to finish...");
            Console.ReadKey();
        }
        // Опять методами разбрасываешься? Давай, давай, мы же миллионеры, закажем горничную
        static public void Producer(int id,BlockingCollection<int> warehouse)
        {
            var rand = new Random();
            var sleepTime = rand.Next(0, 3000);
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(sleepTime);
                warehouse.Add(i + 10*id);
                Console.WriteLine($"Producer {id} produced goods {i + 10 * id}");
            }    

        }

        static public void Consumer(int id, BlockingCollection<int> warehouse)
        {
            var rand = new Random();
            var sleepTime = rand.Next(0, 7000);
            Thread.Sleep(sleepTime);
            if (warehouse.Count == 0)
            {
                Console.WriteLine($"Consumer {id} has left unhappy");
                return;
            }    
            foreach(var item in warehouse)
            {
                int _item = item;
                if (warehouse.TryTake(out _item))
                {
                    Console.WriteLine($"Consumer {id} bought goods {item}");
                }
            }

        }
    }
   
}

