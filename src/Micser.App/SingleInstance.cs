using Newtonsoft.Json;
using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows;

namespace Microsoft.Shell
{
    public interface ISingleInstanceApp
    {
        void SignalExternalCommandLineArgs(string[] args);
    }

    /// <summary>
    /// This class checks to make sure that only one instance of
    /// this application is running at a time.
    /// </summary>
    /// <remarks>
    /// Note: this class should be used with some caution, because it does no
    /// security checking. For example, if one instance of an app that uses this class
    /// is running as Administrator, any other instance, even if it is not
    /// running as Administrator, can activate it with command line arguments.
    /// For most apps, this will not be much of an issue.
    /// </remarks>
    public static class SingleInstance<TApplication>
        where TApplication : Application, ISingleInstanceApp
    {
        private const string PipeName = "SingleInstanceApplication";

        private static NamedPipeServerStream _serverPipe;

        /// <summary>
        /// Application mutex.
        /// </summary>
        private static Mutex _singleInstanceMutex;

        /// <summary>
        /// Cleans up single-instance code, clearing shared resources, mutexes, etc.
        /// </summary>
        public static void Cleanup()
        {
            if (_singleInstanceMutex != null)
            {
                _singleInstanceMutex.Close();
                _singleInstanceMutex = null;
            }

            if (_serverPipe != null)
            {
                _serverPipe.Close();
                _serverPipe = null;
            }
        }

        /// <summary>
        /// Checks if the instance of the application attempting to start is the first instance.
        /// If not, activates the first instance.
        /// </summary>
        /// <returns>True if this is the first instance of the application.</returns>
        public static bool InitializeAsFirstInstance(string uniqueName, string[] args)
        {
            var fullPipeName = uniqueName + Environment.UserName + PipeName;

            // Create mutex based on unique application Id to check if this is the first instance of the application.
            _singleInstanceMutex = new Mutex(true, fullPipeName, out var firstInstance);

            if (firstInstance)
            {
                _serverPipe = new NamedPipeServerStream(fullPipeName, PipeDirection.In);
                _serverPipe.BeginWaitForConnection(OnPipeConnected, null);
            }
            else
            {
                SignalFirstInstance(fullPipeName, args);
            }

            return firstInstance;
        }

        /// <summary>
        /// Activates the first instance of the application with arguments from a second instance.
        /// </summary>
        /// <param name="args">List of arguments to supply the first instance of the application.</param>
        private static void ActivateFirstInstance(string[] args)
        {
            // Set main window state and process command line args
            if (Application.Current == null)
            {
                return;
            }

            ((TApplication)Application.Current).SignalExternalCommandLineArgs(args);
        }

        private static void OnPipeConnected(IAsyncResult ar)
        {
            _serverPipe.EndWaitForConnection(ar);

            var data = new byte[1024];
            var offset = 0;
            int read;
            var content = new StringBuilder();

            while ((read = _serverPipe.Read(data, offset, data.Length)) > 0)
            {
                content.Append(Encoding.UTF8.GetString(data, offset, read));
                offset += read;
            }

            var args = JsonConvert.DeserializeObject<string[]>(content.ToString());
            ActivateFirstInstance(args);
        }

        /// <summary>
        /// Creates a client channel and obtains a reference to the remoting service exposed by the server -
        /// in this case, the remoting service exposed by the first instance. Calls a function of the remoting service
        /// class to pass on command line arguments from the second instance to the first and cause it to activate itself.
        /// </summary>
        private static void SignalFirstInstance(string pipeName, string[] args)
        {
            using (var client = new NamedPipeClientStream(pipeName))
            {
                try
                {
                    client.Connect(100);
                    var json = JsonConvert.SerializeObject(args);
                    var data = Encoding.UTF8.GetBytes(json);
                    client.Write(data, 0, data.Length);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}