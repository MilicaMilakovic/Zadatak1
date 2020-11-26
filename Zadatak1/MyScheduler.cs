using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;


namespace Zadatak1
{
    public delegate void TaskToExecute(int priority);

    public class MyTaskScheduler : TaskScheduler, IDisposable
    {
        //niti u bazenu
        private Thread[] myThreadPool;

        private int position;
        // svi taskovi 
        private List<(int, TaskToExecute)> allTasks = new List<(int, TaskToExecute)>();

        // taskovi koji treba da se izvrse
        public List<Task> pendingTasks = new List<Task>();

        // koliko taskova moze istovremeno da se izvrsava, broj niti u thread pool-u
        private int MaxDegreeOfParallelism;

        // broj taskova koji se trenutno izvrsavaju
        private int currentlyRunning = 0;

        private TaskToExecute addAfter;

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
                Console.WriteLine("thread"+myThreadPool[i].ManagedThreadId +" start " );
            }
        }

        public Task Run(Action action) => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, this);

        // override
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            Console.WriteLine("mgfhf");
            return pendingTasks;
        }


        protected override void QueueTask(Task task)
        {
            lock (pendingTasks)
            {
                pendingTasks.Insert(position, task);
            }
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

                        task = pendingTasks[0];
                        pendingTasks.RemoveAt(0);
                        allTasks.RemoveAt(0);
                    }


                    Console.WriteLine("thread:"+Thread.CurrentThread.ManagedThreadId+ " zadatak: "+task.Status);
                    //task.Start();
                    TryExecuteTask(task);
                    Console.WriteLine("thread:"+Thread.CurrentThread.ManagedThreadId+" zadatak:  "+task.Status);

                } catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
             Console.WriteLine("ne daj boze");
             return false;
        }


        public void AddTask(int priority, TaskToExecute task)
        {
            allTasks.Add((priority, task));
            allTasks.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            position = allTasks.FindIndex(a => a.Item2.Equals(task));
            
            Task t = new Task(() =>
            {
                task(priority);
            });
            t.Start(this);

            Console.WriteLine("Stanje liste:");

            foreach (var a in allTasks)
                Console.Write(a.Item1 + " ");

            Console.WriteLine("\n");

            //lock (pendingTasks)
            //{
            //    //pendingTasks.Clear();

            //    for (int i = 0; i < allTasks.Count; i++)
            //    {
            //        //if (!allTasks[i].Item2.Status.Equals(TaskStatus.Running))
            //       //     QueueTask(allTasks[i].Item2);
            //    }
            //}
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

    public class Data
    {
        public int ThreadID;
        public int TaskPriority;

        public bool isCancelled;

        public Data (int taskPriority, int threadID)
        {
            TaskPriority = taskPriority;
            ThreadID = threadID;
        }

        public void Cancel() => isCancelled = true;
    }


    public class LaneWriter
    {
        readonly Mutex laneMutex = new Mutex();

        public int NumLanes => lanes.Length;

        readonly List<int>[] lanes;

        public LaneWriter(int numLanes)
        {
            laneMutex.WaitOne();
            lanes = new List<int>[numLanes];
            for (int i = 0; i < numLanes; ++i)
                lanes[i] = new List<int>();
            laneMutex.ReleaseMutex();
        }

        public void WriteToLane(int lane, int value) => lanes[lane].Add(value);

        public void PrintLanes()
        {
            laneMutex.WaitOne();
            Console.Clear();
            int maxLength = lanes.Max(x => x.Count);
            for (int i = 0; i < maxLength; ++i)
            {
                Console.Write($"T{i}:\t");
                for (int j = 0; j < NumLanes; ++j)
                    if (i < lanes[j].Count)
                        Console.Write($"{lanes[j][i]}\t");
                    else
                        Console.Write("_\t");
                Console.WriteLine();
            }
            laneMutex.ReleaseMutex();
        }
    }
}
