using System;
using System.Threading;
using System.Threading.Tasks;


namespace Zadatak1.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Task task = Task.Factory.StartNew(() =>
            {
               int i = 0;
               for (i = 0; i <= 10000; i++)
               {
               }
               Console.WriteLine("Finished. {0} iterations", i);
            });

            task.Wait();

            Task taskB = Task.Run(() => Thread.Sleep(2000));
            Console.WriteLine("taskB status {0}", taskB.Status);

            taskB.Wait();
            Console.WriteLine("taskB status {0}", taskB.Status);


        }
    }
}
