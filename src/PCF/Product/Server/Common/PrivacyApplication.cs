namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime;
    using System.Security.Permissions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.FeatureManagement.FeatureFilters;
    using Microsoft.PrivacyServices.Common.Azure;

    using MS.Msn.Runtime;

    using PerformanceCounterType = Microsoft.Azure.ComplianceServices.Common.Instrumentation.PerformanceCounterType;

    /// <summary>
    /// Defines an application that can listen for control+c events.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")] // CancellationTokenSource
    [ExcludeFromCodeCoverage]
    public abstract class PrivacyApplication
    {
        private const int DefaultConnectionLimit = 500;
        private readonly CommandFeedService service;
        private readonly TaskCompletionSource<bool> stopEvent;
        private readonly List<Task> tasks;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the PrivacyApplication class.
        /// </summary>
        [SecurityPermission(SecurityAction.Demand, ControlAppDomain = true)]
        protected PrivacyApplication(CommandFeedService service)
        {
            this.service = service;

            DualLogger.AddTraceListener();
            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PCF);

            DualLogger.Instance.Information(nameof(PrivacyApplication), $"Create service ({this.ServiceName}).");

            // Explicitly set culture so we don't get any unexpected surprises.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            this.stopEvent = new TaskCompletionSource<bool>();

            // This is creating FXCop LinkDemand error. This is not an issue.
            // The same error is being reported for UnhandledException, ProcessExit, GetCurrentProcess, Kill and TraceLevel.Set
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            AppDomain.CurrentDomain.ProcessExit += this.OnProcessExit;

            EnvironmentInfo.Initialize(this.ServiceName);

            DualLogger.Instance.Information(nameof(PrivacyApplication), "Waiting for dependencies to install...");
            Task oneMinute = Task.Delay(TimeSpan.FromMinutes(1));
            Task firstCompleted = Task.WhenAny(oneMinute, EnvironmentInfo.HostingEnvironment.WaitForDependenciesInstalledAsync()).GetAwaiter().GetResult();
            if (firstCompleted == oneMinute)
            {
                DualLogger.Instance.Information(nameof(PrivacyApplication), "Dependencies not installed after one minute.");
                Environment.Exit(123); // non-zero exit code.
            }

            DualLogger.Instance.Information(nameof(PrivacyApplication), $"ServiceName: {EnvironmentInfo.ServiceName}");
            DualLogger.Instance.Information(nameof(PrivacyApplication), $"EnvironmentName: {EnvironmentInfo.EnvironmentName}");
            DualLogger.Instance.Information(nameof(PrivacyApplication), $"HostingEnvironment: {EnvironmentInfo.HostingEnvironment.GetType().Name}");
            DualLogger.Instance.Information(nameof(PrivacyApplication), $"IsDevBoxEnvironment: {EnvironmentInfo.IsDevBoxEnvironment}");
            DualLogger.Instance.Information(nameof(PrivacyApplication), $"IsHostedEnvironment: {EnvironmentInfo.IsHostedEnvironment}");
            DualLogger.Instance.Information(nameof(PrivacyApplication), $"IsUnitTest: {EnvironmentInfo.IsUnitTest}");
            DualLogger.Instance.Information(nameof(PrivacyApplication), $"NodeName: {EnvironmentInfo.NodeName}");

            // Initialize flighting.
            DualLogger.Instance.Information(nameof(PrivacyApplication), "Initializing flighting...");
            FlightingUtilities.Initialize(EnvironmentInfo.HostingEnvironment.CreateAppConfiguration(DualLogger.Instance));
            FlightingUtilities.IsEnabled("FakeFlight");

            CreateProcessStallDectionTimer();
            ConfigureThreadPool();
            MonitorThreadpoolSize();

            this.SetConsoleTitle();

            this.SetAppVersionCounter();
            SetInstancesAliveCounter();

            this.tasks = new List<Task>();

            PrivacyApplication.Instance = this;
        }

        /// <summary>
        /// The currently running application. This is a singleton that is representative of the current process.
        /// Be careful: In test contexts, this may be null.
        /// </summary>
        public static PrivacyApplication Instance { get; private set; }

        /// <summary>
        /// Cancellation Token
        /// </summary>
        public CancellationToken CancellationToken => this.cancellationTokenSource.Token;

        /// <summary>
        /// Command line options for the process.
        /// </summary>
        public CommandLineOptions CommandLineOptions
        {
            get;
            private set;
        }

        /// <summary>
        /// The name of the service.
        /// </summary>
        public string ServiceName => this.service.ToString();

        /// <summary>
        /// Add task into tasks pool
        /// </summary>
        public void AddTask(Task task)
        {
            this.tasks.Add(task);
        }

        /// <summary>
        /// Runs the service with the given command line arguments.
        /// </summary>
        public void Run(string[] args)
        {
            DualLogger.Instance.Information(nameof(PrivacyApplication), $"Start {this.ServiceName}.");

            CommandLineOptions options = new CommandLineOptions();

            MS.Msn.Runtime.CommandLine commandLine = new MS.Msn.Runtime.CommandLine(typeof(CommandLineOptions))
            {
                IgnoreCase = true
            };

            commandLine.Parse(options, args);

            this.CommandLineOptions = options;

            if (options.Help)
            {
                options.PrintUsage(null);
                return;
            }

            string[] invalidParameters = commandLine.GetNonParsedParameters();

            if (invalidParameters.Length > 0)
            {
                options.PrintUsage(invalidParameters[0]);
                return;
            }

            if (options.DelayStartSec > 0)
            {
                DualLogger.Instance.Information(
                    nameof(PrivacyApplication),
                    $"Waiting {options.DelayStartSec} seconds before start... Now is a good time to attach a debugger.");
                Thread.Sleep(options.DelayStartSec * 1000);
            }

            if (options.Debug)
            {
                System.Diagnostics.Debugger.Launch();
            }

            // Make sure we are running with server GC
            EnsureRunningOnServerGC();

            // This should avoid hard-to-debug timeouts with outgoing calls
            ServicePointManager.DefaultConnectionLimit = DefaultConnectionLimit;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;

            this.RunAsConsoleApp(false);
            DualLogger.Instance.Information(nameof(PrivacyApplication), $"{this.ServiceName} Done.");
        }

        /// <summary>
        /// Abstract method invoked when the app starts.
        /// </summary>
        protected virtual void OnStart()
        {
        }

        /// <summary>
        /// Abstract method invoked when the app stops.
        /// </summary>
        protected virtual void OnStop()
        {
        }

        /// <summary>
        /// Invoked when the app has received a control+c from autopilot.
        /// </summary>
        /// <returns>A task that completes when the app can safely stop.</returns>
        protected virtual Task OnPrepareToStop()
        {
            // Return a task that completes immediately.
            return Task.FromResult(false);
        }

        /// <summary>
        /// Stops the app without a control+c event.
        /// </summary>
        public void Shutdown()
        {
            this.stopEvent.TrySetResult(true);
        }

        #region private members

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogUnhandledException("AppDomain", e.ExceptionObject);

            // Wait a tick for the above exception to flush to log since we're crashing anyway.
            Task.Delay(2000).Wait();
        }

        private static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            LogUnhandledException("UnobservedTask", unobservedTaskExceptionEventArgs.Exception.Flatten().InnerExceptions.FirstOrDefault());
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "handledAt")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "exception")]
        private static void LogUnhandledException(string handledAt, object exception)
        {
            Logger.Instance?.UnexpectedException(exception as Exception);
        }

        private void Start()
        {
            lock (this)
            {
                this.OnStart();
            }
        }

        private Task PrepareToStop()
        {
            lock (this)
            {
                DualLogger.Instance.Information(nameof(PrivacyApplication), $"Shutdown {this.ServiceName} process.");
                DualLogger.Instance.Information(nameof(PrivacyApplication), "Cancel running tasks.");
                this.cancellationTokenSource.Cancel();

                System.Diagnostics.Trace.Flush();
                return this.OnPrepareToStop();
            }
        }

        private void Stop()
        {
            lock (this)
            {
                this.OnStop();
                DualLogger.Instance.Information(nameof(PrivacyApplication), $"Waiting 30 secs for ({this.tasks.Count}) running task(s).");
                var status = Task.WaitAll(this.tasks.ToArray(), TimeSpan.FromSeconds(30));
                DualLogger.Instance.Information(nameof(PrivacyApplication), $"All threads completed: {status}.");

                DualLogger.Instance.Information(nameof(PrivacyApplication), $"Wait for any pending log statements to flush to disk.");
                System.Diagnostics.Trace.Flush();
                Task.Delay(5000).Wait();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SecurityPermissionAttribute(SecurityAction.Demand, UnmanagedCode = true)]
        private void RunAsConsoleApp(bool quietMode)
        {
            try
            {

                using (ConsoleCtrlHandler ctrlHandler = new ConsoleCtrlHandler())
                {
                    DualLogger.Instance.Information(nameof(PrivacyApplication), $"Running \"{this.ServiceName}\" as a console application");
                    this.Start();

                    DualLogger.Instance.Information(nameof(PrivacyApplication), "Press Ctrl-C or Ctrl-Break to terminate the application");

                    Task checkForRestartRequested = CheckForRestartFlag();
                    Task.WhenAny(this.stopEvent.Task, WaitHandleToTask(ctrlHandler.WaitHandle), checkForRestartRequested).Wait();

                    DualLogger.Instance.Warning(nameof(PrivacyApplication), "The application has been signaled to stop.");
                    Task shutdownTask = this.PrepareToStop();
                    shutdownTask.Wait();

                    this.Stop();
                }
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(PrivacyApplication), ex, $"Fail to run: {this.ServiceName}");
                this.Stop();
            }
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
        }

        private void SetConsoleTitle()
        {
            string consoleTitle = string.Format(CultureInfo.InvariantCulture, "{0} (v{1})", this.ServiceName, Assembly.GetCallingAssembly().GetName().Version);

            try
            {
                Console.Title = consoleTitle;
            }
            catch (IOException)
            {
                // Ignore this, it just means that we weren't invoked from a console.
            }
        }

        private static Task WaitHandleToTask(WaitHandle handle)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            ThreadPool.RegisterWaitForSingleObject(
                handle,
                delegate { tcs.TrySetResult(true); },
                null,
                -1,
                true);

            return tcs.Task;
        }

        private static void EnsureRunningOnServerGC()
        {
            // Check that we are running in server-mode GC
            if (GCSettings.IsServerGC == false)
            {
                throw new InvalidOperationException("Service must run in server GC mode.");
            }
        }

        private static void ConfigureThreadPool()
        {
            // Set max threads to something reasonable to ease up on lock contention.
            ThreadPool.SetMaxThreads(100, 100);

            // Reflection Hack:
            // System.dll contains an internal property called "IsAspNetServer". This property governs two behaviors:
            // 1. When true, changes the default service point manager connection limit from 2 to 10. We don't care about this one very much because we set it elsewhere.
            //
            // 2. The second behavior is a bit more nuanced. HttpWebRequest has some logic to throw an invalid operation exception if it detects that the thread pool
            //    does not have many free threads. This check is disabled when "IsAspNetServer" is true. When it is false, any outbound Http requests fail instantly
            //    if the threadpool is near exhaustion.
            //
            // The reason that this flag is false by default in our configurations is that we are not running under IIS. We use HTTP Self Host server, which
            // does use ASP.NET without IIS in the picture. What follows is a small bit of reflection to twiddle this to be true so that we can
            // continue to make outbound HTTP requests even under heavy load.
            //
            // For more information, please see the following .NET sources:
            //   HttpWebRequest: https://referencesource.microsoft.com/#System/net/System/Net/HttpWebRequest.cs,1925
            //   IsThreadPoolLow: https://referencesource.microsoft.com/#System/net/System/Net/Internal.cs,b4f68cf6c395e516
            //   ComNetOS class: https://referencesource.microsoft.com/#System/net/System/Net/Internal.cs,413d016b6f806bf2
            Assembly systemAssembly = typeof(System.Net.HttpWebRequest).Assembly;
            Type comNetOs = systemAssembly.GetType("System.Net.ComNetOS", true);
            var isAspNetServerField = comNetOs.GetField("IsAspNetServer", BindingFlags.Static | BindingFlags.NonPublic);
            isAspNetServerField.SetValue(null, true);
        }

        /// <summary>
        /// Starts a high-priority background thread
        /// that writes the number of available threadpool threads
        /// to a perf counter every second.
        /// </summary>
        private static void MonitorThreadpoolSize()
        {
            ThreadStart callback = delegate
            {
                while (true)
                {
                    int workerThreads, iocpThreads;
                    ThreadPool.GetAvailableThreads(out workerThreads, out iocpThreads);

                    var counter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "ThreadpoolThreadsAvailable");
                    counter.Set("Worker", workerThreads);
                    counter.Set("IOCP", iocpThreads);

                    Thread.Sleep(1000);
                }
            };

            // The runtime keeps a reference to the thread as long as it is running.
            // It only becomes eligible for GC after it terminates:
            // https://stackoverflow.com/questions/81730/what-prevents-a-thread-in-c-sharp-from-being-collected
            Thread thread = new Thread(callback)
            {
                Name = "ThreadPoolMonitor",
                IsBackground = true,

                // High priority so this thread is sure to get scheduled regularly. We don't want a busy server to pre-empt this
                // from being run since we base alarming off of it.
                Priority = ThreadPriority.Highest
            };

            thread.Start();
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private static void CreateProcessStallDectionTimer()
        {
            const int SleepIntervalMilliseconds = 100;
            Timer timer = null;
            DateTimeOffset previousTime = DateTimeOffset.UtcNow;

            // Normalize the update time to be centered at the 15 second mark of each minute.
            DateTimeOffset lastCounterUpdatedTime = DateTimeOffset.UtcNow;
            int intervalCount = 0;

            TimerCallback callback = delegate
            {
                DateTimeOffset currentTime = DateTimeOffset.UtcNow;

                // Measure the amount of time we lost by subtracting the measured time and subtract the time we expected to elapse.
                int lostMilliseconds = (int)(currentTime - previousTime).TotalMilliseconds - SleepIntervalMilliseconds;

                if (lostMilliseconds >= 3 * SleepIntervalMilliseconds)
                {
                    // TODO: log
                }

                intervalCount++;
                if (currentTime - lastCounterUpdatedTime > TimeSpan.FromMinutes(1))
                {
                    PerfCounterUtility
                        .GetOrCreate(PerformanceCounterType.Number, "100MsIntervals")
                        .Set(intervalCount);

                    intervalCount = 0;
                    lastCounterUpdatedTime = currentTime;
                }

                previousTime = currentTime;

                // Reschedule for one interval in the future.
                timer.Change(SleepIntervalMilliseconds, Timeout.Infinite);
            };

            timer = new Timer(callback, null, SleepIntervalMilliseconds, Timeout.Infinite);
        }

        private static void SetInstancesAliveCounter()
        {
            // Set up liveliness perf counter
            var counter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "InstancesAlive");
            counter.Set(0);

            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(2));

                while (true)
                {
                    counter.Set(1);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            });
        }

        /// <summary>
        /// Periodically sets the app version counter to the current version of this assembly.
        /// </summary>
        private void SetAppVersionCounter()
        {
            // Carbon build only populates full assembly version in the file version, which is different than
            // the assembly version.
            var assemblyVersion = FileVersionInfo.GetVersionInfo(this.GetType().Assembly.Location);
            
            // Only use build and revision -- major and minor are always fixed to "9" and "0".
            // Build corresponds to the date "17305", and revision is the build number for that day ("11").
            int numericVersion = int.Parse($"{assemblyVersion.FileBuildPart}{assemblyVersion.FilePrivatePart}");

            var counter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "AppVersion");

            Task.Run(async () =>
            {
                while (true)
                {
                    counter.Set(numericVersion);
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            });
        }

        /// <summary>
        /// Polls for the restart flag to be set to true.
        /// </summary>
        private static async Task CheckForRestartFlag()
        {
            while (true)
            {
                if (ExecutionContext.RestartRequested)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        #endregion
    }
}
