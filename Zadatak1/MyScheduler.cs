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
        public static List<MyTask> allTasks = new List<MyTask>();

        public static List<Task> taskovi = new List<Task>();
        // taskovi koji treba da se izvrse
        public static List<Task> pendingTasks = new List<Task>();
        // taskovi koji se trenutno izvrsavaju
        public static List<MyTask> currentlyRunning=new List<MyTask>();
        // koliko taskova moze istovremeno da se izvrsava, broj niti u thread pool-u
        private int MaxDegreeOfParallelism;

        private TaskToExecute addAfter;
        private static bool preemption = false;
        Task novi;

        private Dictionary<Task, MyTask> mapa = new Dictionary<Task, MyTask>();
        private Dictionary<MyTask, Task> mapaInverted = new Dictionary<MyTask, Task>();

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

            if (preemption)
            {
                Thread pauseTask = new Thread(() =>
                {
                    Console.WriteLine("pauseTask started");

                    while (true)
                    {
                        Thread.Sleep(500);
                        // sve niti trenutno izvrsavaju neki task
                       
                        if (currentlyRunning.Count == maxDegreeOfParallelism)
                        {                           
                            if (novi != null)
                            {
                                if (mapa.TryGetValue(novi, out MyTask mt))
                                {
                                    Console.WriteLine("novi  {0}", mt.taskPriority);

                                    // novi - koji je zadnji stigao 
                                    
                                    Console.WriteLine("currently running: "+currentlyRunning.Count );

                                    List<MyTask> miniList = new List<MyTask>();
                                    for(int i=0;i<currentlyRunning.Count;i++)
                                    {
                                        if (!currentlyRunning[i].isPaused)
                                            miniList.Add(currentlyRunning[i]);
                                    }

                                    MyTask min = miniList.Aggregate((i1, i2) => (i1.taskPriority < i2.taskPriority) ? i1 : i2);
                                                                       
                                    novi = null; 
                                    if (min.isPaused)
                                        continue;
                                    
                                    if (min.taskPriority < mt.taskPriority)
                                    {
                                        Console.WriteLine(" mt{0} => min{1}", mt.taskPriority, min.taskPriority);
                                        min.executeNextInfo = mt;
                                        min.executeNext = mt.taskToExecute;
                                        min.Pause();
                                        lock (pendingTasks)
                                        {
                                            pendingTasks.RemoveAt(mt.position);
                                            allTasks.RemoveAt(mt.position);
                                        }
                                    }

                                }

                            }

                        }
                    }
                }); pauseTask.Start();
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
                        currentlyRunning.Add(taskToTerminate);
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
             //Console.WriteLine("ne daj boze");
             return false;
        }        

        public void AddTask(MyTask myTask)
        {
            allTasks.Add(myTask);
            allTasks.Sort((x, y) => x.taskPriority.CompareTo(y.taskPriority));
            position = allTasks.FindIndex(a => a.task.Equals(myTask.task));

           // Console.WriteLine(position);
            myTask.position = position;

            if(currentlyRunning.Count == MaxDegreeOfParallelism)
                novi = myTask.task;

            myTask.task.Start(this);

            mapa.Add(myTask.task, myTask);
            mapaInverted.Add(myTask, myTask.task);
            taskovi.Add(myTask.task);


            //Console.WriteLine("allTasks:");
            //foreach (var x in allTasks)
            //    Console.Write(x.taskPriority  + " | ");

            //Console.WriteLine("\n");


            //Console.WriteLine("pendingTasks:");

            //Console.WriteLine("pending tasks count:" + pendingTasks.Count);
        }

        public static void setPreemption()
        {
            preemption = true;
        }
    }

    public class MyTask
    {
        public int taskPriority;
        public bool isCancelled;
        public bool isPaused;
        public bool isDone;

        public int position;
        public Task task;

        public TaskToExecute taskToExecute;

        public TaskToExecute executeNext;
        public MyTask executeNextInfo;

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
        public void Resume() => isPaused = false;

        public  string toSting()
        {
            return "Prioritet: " + taskPriority;
        }
    }      
}
