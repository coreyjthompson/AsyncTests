using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //var app = new BreakfastApplication();
            //var app = new Application();
            //var app = new ProcessingTasksAsTheyCompleteApplication();
            var app = new TaskWhenAllApplication();

            app.Execute();

            Console.Read();
        }

        public class Application
        {
            public async void Execute()
            {
                Console.WriteLine("Running with await");
                await RunTasksAwait();
                Console.WriteLine("Running with Task.WaitAll()");
                await RunTasksWaitAll();
                Console.WriteLine("Running with Task.WhenAll()");
                await RunTasksWhenAll();
                Console.WriteLine("Running with Task.Run()");
                await RunTasksWithTaskRun();
                Console.WriteLine("Running with Parallel");
                RunTasksWithParallel();
            }

            /// <summary>
            /// Pros: It works
            /// Cons: The tasks are NOT run in parallel.
            ///       Code after the await is not run while the await is awaited
            ///       **If you want parallelism, this isn't even an option.**
            ///       Slowest. Because of no parallelism.
            /// </summary>
            public async Task RunTasksAwait()
            {
                var group = "await";
                var watcher = new Stopwatch();
                watcher.Start();

                // You just assign the return variables as normal.
                int result1 = await MyTaskAsync(1, 500, group);
                int result2 = await MyTaskAsync(2, 300, group);
                int result3 = await MyTaskAsync(3, 100, group);

                Console.WriteLine("Code immediately after tasks.");

                watcher.Stop();

                // You now have access to the return objects directly.
                Console.WriteLine($"{group} runtime: {watcher.ElapsedMilliseconds}");
            }

            /// <summary>
            /// WaitAll behaves quite differently from WhenAll
            /// Pros: It works
            ///       The tasks run in parallel
            /// Cons: It isn't clear whether the code is parallel here, but it is.
            ///       It isn't clear whether the code  is async here, but it is NOT.
            ///       There is a Visual Studio usage warning. You can remove async to get rid of it because it isn't an Async method.
            ///       The return value is wrapped in the Result property of the task
            ///       Breaks Async end-to-end
            ///       Note: I can't foresee a use case where WaitAll would be preferred over WhenAll.
            /// </summary>
            public async Task RunTasksWaitAll()
            {
                var group = "WaitAll";
                var watcher = new Stopwatch();
                watcher.Start();

                Task<int> task1 = MyTaskAsync(1, 500, group);
                Task<int> task2 = MyTaskAsync(2, 300, group);
                Task<int> task3 = MyTaskAsync(3, 100, group);

                Console.WriteLine("Code immediately after tasks.");

                Task.WaitAll(task1, task2, task3);

                watcher.Stop();

                // You now have access to the return object using the Result property.
                int result1 = task1.Result;
                int result2 = task2.Result;
                int result3 = task3.Result;

                Console.WriteLine($"{group} runtime: {watcher.ElapsedMilliseconds}");
            }

            /// <summary>
            /// WhenAll gives you the best of all worlds. The code is both parallel and async.
            /// Pros: It works
            ///       The tasks run in parallel
            ///       Code after the tasks run while the task is running
            ///       Doesn't break end-to-end async
            /// Cons: It isn't clear you are doing parallelism here, but you are.
            ///       There is a Visual Studio usage warning
            ///       The return value is wrapped in the Result property of the task
            /// </summary>
            public async Task RunTasksWhenAll()
            {
                var group = "WaitAll";
                var watcher = new Stopwatch();
                watcher.Start();

                Task<int> task1 = MyTaskAsync(1, 500, group); // You can't use await if you want parallelism
                Task<int> task2 = MyTaskAsync(2, 300, group);
                Task<int> task3 = MyTaskAsync(3, 100, group);

                Console.WriteLine("Code immediately after tasks.");

                await Task.WhenAll(task1, task2, task3); // But now you are calling await, so you are sort of still awaiting

                watcher.Stop();

                Console.WriteLine($"{group} runtime: {watcher.ElapsedMilliseconds}");
            }

            /// <summary>
            /// Pros: It works
            ///       The tasks run in parallel
            ///       Code can run immediately after the tasks but before the tasks complete
            ///       Allows for running non-async code asynchronously
            /// Cons: It isn't clear whether the code is doing parallelism here. It isn't.
            ///       The lambda syntax affects readability
            ///       Breaks Async end-to-end
            /// </summary>
            public async Task RunTasksWithTaskRun()
            {
                var group = "Task.Run()";
                var watcher = new Stopwatch();
                watcher.Start();

                int result1 = await Task.Run(() => MyTask(1, 500, group));
                int result2 = await Task.Run(() => MyTask(2, 300, group));
                int result3 = await Task.Run(() => MyTask(3, 100, group));

                Console.WriteLine("Code immediately after tasks.");

                watcher.Stop();

                // You now have access to the return objects directly.
                Console.WriteLine($"{group} runtime: {watcher.ElapsedMilliseconds}");
            }

            /// <summary>
            /// Pros: It works
            ///       It is clear in the code you want to run these tasks in parallel.
            ///       Code can run immediately after the tasks but before the tasks complete
            ///       Fastest
            /// Cons: There is no async or await.
            ///       Breaks Async end-to-end. You can workaround this by wrapping Parallel.Invoke in a Task.Run method. See commented code.
            /// </summary>
            public /* async */ void RunTasksWithParallel()
            {
                var group = "Parallel";
                var watcher = new Stopwatch();
                watcher.Start();

                // You have to declare your return objects before hand.
                //await Task.Run(() => 
                int result1, result2, result3;
                Parallel.Invoke(() => result1 = MyTask(1, 500, group),
                    () => result2 = MyTask(2, 300, group),
                    () => result3 = MyTask(3, 100, group),
                    () => Console.WriteLine("Code immediately after tasks."));

                //);
                // You now have access to the return objects directly.
                watcher.Stop();

                Console.WriteLine($"{group} runtime: {watcher.ElapsedMilliseconds}");
            }

            public async Task<int> MyTaskAsync(int i, int milliseconds, string group)
            {
                await Task.Delay(milliseconds);

                Console.WriteLine($"{group}: {i}");

                return i;
            }

            public int MyTask(int i, int milliseconds, string group)
            {
                var task = Task.Delay(milliseconds);
                task.Wait();

                Console.WriteLine($"{group}: {i}");

                return i;
            }
        }

        public class ProcessingTasksAsTheyCompleteApplication
        {
            // http://hamidmosalla.com/2018/04/27/using-task-whenany-and-task-whenall/

            public async void Execute()
            {
                await ProcessTasksAsync();
            }

            static async Task<int> ExampleTaskAsync(int val)
            {
                await Task.Delay(TimeSpan.FromSeconds(val));

                return val;
            }

            static async Task AwaitAndProcessAsync(Task<int> task)
            {
                var result = await task;
                Console.WriteLine(result);
            }

            static async Task ProcessTasksAsync()
            {
                Task<int> task1 = ExampleTaskAsync(2);
                Task<int> task2 = ExampleTaskAsync(3);
                Task<int> task3 = ExampleTaskAsync(1);

                var tasks = new[] { task1, task2, task3 };

                var processingTasks = tasks.Select(AwaitAndProcessAsync).ToList();

                await Task.WhenAll(processingTasks);
            }

        }

        public class TaskWhenAllApplication
        {
            // http://hamidmosalla.com/2018/04/27/using-task-whenany-and-task-whenall/

            public async void Execute()
            {
                //await ProcessTasksAsync();
                await ProcessTasksAsync2();
            }

            static async Task ExampleTaskAsync(int val)
            {
                await Task.Delay(TimeSpan.FromSeconds(val));
                Console.WriteLine(val);
            }

            /// <summary>
            /// Pros: It works
            /// Cons: Inefficient because we have to dispatch the tasks one at a time.
            /// </summary>
            static async Task ProcessTasksAsync()
            {
                Task task1 = ExampleTaskAsync(2);
                Task task2 = ExampleTaskAsync(3);
                Task task3 = ExampleTaskAsync(1);

                var tasks = new[] { task1, task2, task3 };
                foreach (var task in tasks)
                {
                    await task;
                }
            }

            /// <summary>
            /// Pros: It works
            /// Cons: Only first exception would be thrown.
            /// </summary>
            static async Task ProcessTasksAsync2()
            {
                Task task1 = ExampleTaskAsync(2);
                Task task2 = ExampleTaskAsync(3);
                Task task3 = ExampleTaskAsync(1);

                var taskList = new List<Task>();
                var taskArray = new[] { task1, task2, task3 };

                foreach (var task in taskArray)
                {
                    taskList.Add(task);
                }

                await Task.WhenAll(taskList);
            }

            private static Task ThrowInvalidOperationExceptionAsync() => throw new NotImplementedException();
            private static Task ThrowNotImplementedExceptionAsync() => throw new InvalidOperationException();

            static async Task AllExceptionsAsync()
            {
                var task1 = ThrowNotImplementedExceptionAsync();
                var task2 = ThrowInvalidOperationExceptionAsync();

                Task allTasks = Task.WhenAll(task1, task2);
                try
                {
                    await allTasks;
                }
                catch
                {
                    AggregateException allExceptions = allTasks.Exception;
                }
            }
        }

        public class BreakfastApplication
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/
            public async void Execute()
            {
                Coffee cup = PourCoffee();
                Console.WriteLine("coffee is ready");
                Task<Egg> eggTask = FryEggs(2);
                Task<Bacon> baconTask = FryBacon(3);
                Task<Toast> toastTask = MakeToastWithButterAndJam(2);

                Thread.Sleep(2000);

                await Task.WhenAll(eggTask, baconTask, toastTask);
                Console.WriteLine("Starting awaits.");
                Console.WriteLine("eggs are ready");
                Console.WriteLine("bacon is ready");
                Console.WriteLine("toast is ready");

                Juice oj = PourOJ();
                Console.WriteLine("oj is ready");

                Console.WriteLine("Breakfast is ready!");
            }

            private async Task<Toast> MakeToastWithButterAndJam(int number)
            {
                var plainToast = await ToastBread(number);
                ApplyButter(plainToast);
                ApplyJam(plainToast);

                return plainToast;
            }

            private async Task<Toast> ToastBread(int i)
            {
                return await Task.Run(() =>
                {
                    Thread.Sleep(2000);

                    return new Toast();
                });
            }

            private async Task<Bacon> FryBacon(int i)
            {
                return await Task.Run(() =>
                {
                    Thread.Sleep(2000);
                    Console.WriteLine("Frying bacon.");

                    return new Bacon();
                });
            }

            private async Task<Egg> FryEggs(int i)
            {
                return await Task.Run(() =>
                {
                    Console.WriteLine("Warming up pan.");
                    Thread.Sleep(2000);
                    Console.WriteLine("Frying eggs.");
                    Thread.Sleep(2000);

                    return new Egg();
                });
            }

            private Coffee PourCoffee()
            {
                Thread.Sleep(1000);

                return new Coffee();
            }

            private Juice PourOJ()
            {
                Thread.Sleep(1000);

                return new Juice();
            }

            private void ApplyJam(Toast toast)
            {
                Thread.Sleep(2000);
            }

            private void ApplyButter(Toast toast)
            {
                Thread.Sleep(2000);
            }

            internal class Juice
            { }

            internal class Toast
            { }

            internal class Bacon
            { }

            internal class Egg
            { }

            internal class Coffee
            { }

        }
    }

}
