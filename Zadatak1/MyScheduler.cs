using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Zadatak1
{
    public delegate void TaskToExecute(MyTask mt);

    public class MyTaskScheduler : TaskScheduler
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
        public bool preemption = true;

        private Dictionary<Task, MyTask> mapa = new Dictionary<Task, MyTask>();
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
                Console.WriteLine("Thread"+myThreadPool[i].ManagedThreadId +" start. " );
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return pendingTasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            lock (pendingTasks)
            {
                pendingTasks.Insert(position, task);

               /* if (preemption)
                {
                    Thread t = new Thread(() =>
                     {
                         // sve niti trenutno izvrsavaju neki task
                         if (pendingTasks.Count > MaxDegreeOfParallelism)
                         {
                             Console.WriteLine("test1");
                             if (mapa.TryGetValue(task, out MyTask mt))
                             {
                                 Console.WriteLine("test");
                                 // task - koji je zadnji stigao 
                                 // iz mape treba naci task koji ima najmanji prioritet,
                                 // i ako je njegov prioritet manji od zadnjeg dodatog, brise se iz liste
                                 // ubacuje se ovaj novi i kad 

                                 MyTask p = mapa.Values.Aggregate((i1, i2) => i1.taskPriority < i2.taskPriority ? i1 : i2);

                                 if (p.taskPriority < mt.taskPriority)
                                 {
                                     p.Pause();
                                     Console.WriteLine(mt.taskPriority+" PAUZIRAN");

                                 }
                             }

                         }
                     }); t.Start();
                }*/
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
                            continue;
                        }

                        task = pendingTasks[0];
                        pendingTasks.RemoveAt(0);
                        allTasks.RemoveAt(0);
                    }

                    if (mapa.TryGetValue(task, out MyTask taskToTerminate))
                    {
                        Thread callback = new Thread(() =>
                        {                            
                            Thread.Sleep(TimeSpan.FromSeconds(taskToTerminate.maxTime));
                            taskToTerminate.Cancel();
                        });
                        callback.Start();
                    }

                    TryExecuteTask(task);                
                    
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

        public void AddTask(MyTask myTask)
        {
            allTasks.Add((myTask.taskPriority, myTask.taskToExecute));
            allTasks.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            position = allTasks.FindIndex(a => a.Item2.Equals(myTask.taskToExecute));

            myTask.task.Start(this);

            mapa.Add(myTask.task, myTask);
                       
            Console.WriteLine("Stanje liste:");

            foreach (var a in allTasks)
                Console.Write(a.Item1 + " ");
            Console.WriteLine("\n");        
        }                
    }

    public class MyTask
    {
        public int taskPriority;
        public bool isCancelled;
        public bool isPaused;
        public bool isDone;

        public Task task;
        public TaskToExecute taskToExecute;

        public int maxTime;

        public MyTask (int taskPriority, TaskToExecute taskToExecute, int maxTime)
        {
            this.taskPriority = taskPriority;
            this.maxTime = maxTime;
            this.taskToExecute = taskToExecute;

           task = new Task(() =>
           {
               taskToExecute(this);
           });            
        }

        public void Cancel() => isCancelled = true;
        public void Pause() => isPaused = true;

        public  string toSting()
        {
            return "Prioritet: " + taskPriority;
        }
    }

   
}
