using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zadatak1.Tests
{
    [TestClass]
    public class MyTaskSchedulerTest
    {
        [TestMethod]
        public void ScheduleTasks()
        {
            const int numOfThreads = 4;
            MyTaskScheduler myTaskScheduler = new MyTaskScheduler(numOfThreads, false);

            Assert.AreEqual(numOfThreads, myTaskScheduler.GetNumberOfThreadsInThreadPool());           
            
        }
        [TestMethod]
        public void AllTasksExecutedNP()
        {
            Demo.Demo.NonPreemptiveDemo();
            Assert.AreEqual(7 , Demo.Demo.mts.taskovi.Count);
        }

        [TestMethod]
        public void AllTasksExecutedP()
        {
            Demo.Demo.PreemptiveDemo();
            Assert.IsTrue(Demo.Demo.mts.preemption);
            Assert.AreEqual(7 , Demo.Demo.mts.taskovi.Count); 
        } 
              
    }
}
