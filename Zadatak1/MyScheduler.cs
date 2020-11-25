using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zadatak1
{

    public class MyTaskScheduler : TaskScheduler, IDisposable
    {
        //niti u bazenu
        private Thread[] myThreadPool;

        // svi taskovi 
        private List<(int, Task)> allTasks = new List<(int, Task)>();

        // taskovi koji treba da se izvrse
        public LinkedList<Task> pendingTasks = new LinkedList<Task>();

        // koliko taskova moze istovremeno da se izvrsava, broj niti u thread pool-u
        private int MaxDegreeOfParallelism;

        // broj taskova koji se trenutno izvrsavaju
        private int currentlyRunning = 0;

        public MyTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) throw new Exception();
            MaxDegreeOfParallelism = maxDegreeOfParallelism;

            myThreadPool = new Thread[maxDegreeOfParallelism];
            for (int i = 0; i < maxDegreeOfParallelism; i++)
            {
                myThreadPool[i] = new Thread(RunTask)
                {
                    IsBackground = false
                 };
                myThreadPool[i].Start();
                Console.WriteLine(myThreadPool[i].ManagedThreadId +" start " );
            }
        }


        // override
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            Console.WriteLine("mgfhf");
            return pendingTasks;
        }


        protected override void QueueTask(Task task)
        {
                pendingTasks.AddLast(task);
        }

        private void RunTask()
        {
            while (true)
            {
                Thread.Sleep(500);

                try
                {
                    Task task;

                    lock (pendingTasks)
                    {

                        if (pendingTasks.Count == 0)
                        {
                            //Console.WriteLine(" a");
                            continue;
                        }

                        task = pendingTasks.First.Value;
                        pendingTasks.RemoveFirst();
                        allTasks.RemoveAt(0);
                    }


                    Console.WriteLine(task.Status);
                    task.Start();

                } catch (Exception e)
                {
                    Console.WriteLine(" ee");
                    Console.WriteLine(e.Message);
                }
            }

        }



            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                Console.WriteLine("ne daj boze");
                return false;
            }


            public void AddTask(int priority, Task task)
            {
                allTasks.Add((priority, task));
                allTasks.Sort((x, y) => x.Item1.CompareTo(y.Item1));

                lock (pendingTasks)
                {
                    pendingTasks.Clear();
                    for (int i=0; i<allTasks.Count; i++)
                    {
                        //if (!allTasks[i].Item2.Status.Equals(TaskStatus.Running)) 
                            QueueTask(allTasks[i].Item2);
                    }
                }
            }


            public static void printMethod (int priority)
            {
                for(int i=0; i<10; i++)
                {
                    Console.WriteLine(priority);
                    Thread.Sleep(1000);
                }
           
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
}
