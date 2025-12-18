using Grayjay.ClientServer;
using Grayjay.ClientServer.Constants;
using Grayjay.ClientServer.Settings;
using Grayjay.ClientServer.States;
using Grayjay.Engine;
using System.Diagnostics;

using Logger = Grayjay.Desktop.POC.Logger;
using LogLevel = Grayjay.Desktop.POC.LogLevel;

namespace Grayjay.Desktop.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await EntryPoint(args);
            }
            catch (Exception e)
            {
                Logger.e<Program>($"Unhandled exception occurred: {e}");
            }
        }

        static async Task EntryPoint(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();

            if (args.Length > 0 && args[0] == "version")
            {
                Console.WriteLine(App.Version.ToString());
                return;
            }

            bool isServer = true;
            bool isHeadless = true;

            Console.WriteLine(Logger.FormatLogMessage(LogLevel.Info, nameof(Program), $"AppContext.BaseDirectory: {AppContext.BaseDirectory}"));
            Console.WriteLine(Logger.FormatLogMessage(LogLevel.Info, nameof(Program), $"Base Directory: {Directories.Base}"));
            Console.WriteLine(Logger.FormatLogMessage(LogLevel.Info, nameof(Program), $"Temporary Directory: {Directories.Temporary}"));
            Console.WriteLine(Logger.FormatLogMessage(LogLevel.Info, nameof(Program), $"Log Level: {(LogLevel)GrayjaySettings.Instance.Logging.LogLevel}"));
            Console.WriteLine(Logger.FormatLogMessage(LogLevel.Info, nameof(Program), $"Log file path: {Path.Combine(Directories.Base, "log.txt")}"));
            Logger.LoadFromSettings();

            FUTO.MDNS.Logger.LogCallback = (level, tag, message, ex) => Logger.Log((LogLevel)level, tag, message, ex);
            FUTO.MDNS.Logger.WillLog = (level) => Logger.WillLog((LogLevel)level);
            Grayjay.Engine.Logger.LogCallback = (level, tag, message, ex) => Logger.Log((LogLevel)level, tag, message, ex);
            Grayjay.Engine.Logger.WillLog = (level) => Logger.WillLog((LogLevel)level);
            SyncShared.Logger.WillLog = (level) => Logger.WillLog((LogLevel)level);
            SyncShared.Logger.LogCallback = (level, tag, message, ex) => Logger.Log((LogLevel)level, tag, message, ex);

            GrayjayDevSettings.Instance.DeveloperMode = File.Exists(Path.Combine(Directories.Base, "DEV"));

            foreach (var arg in args)
                Console.WriteLine(Logger.FormatLogMessage(LogLevel.Info, nameof(Program), "Arg: " + arg));

            Updater.SetStartupArguments(string.Join(" ", args.Select(x => (x.Contains(" ") ? $"\"{x}\"" : x))));

            Logger.i<Program>($"Initialize {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            Stopwatch watch = Stopwatch.StartNew();
            Logger.i(nameof(Program), "Main: StateApp.Startup");
            await StateApp.Startup();
            Logger.i(nameof(Program), $"Main: StateApp.Startup finished ({watch.ElapsedMilliseconds}ms)");

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            GrayjayServer server = new GrayjayServer(null, isHeadless, isServer);
            _ = Task.Run(async () =>
            {
                try
                {
                    await server.RunServerAsync(null, cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Logger.e(nameof(Program), $"Main: Unhandled error in RunServerAsync.", ex);
                }
                finally
                {
                    Logger.i(nameof(Program), "Application graceful exit requested.");
                    cancellationTokenSource.Cancel();
                }
            });

            watch.Restart();

            Logger.i(nameof(Program), "Main: Waiting for ASP to start.");
            server.StartedResetEvent.Wait();
            Logger.i(nameof(Program), $"Main: Waiting for ASP to start finished ({watch.ElapsedMilliseconds}ms)");

            sw.Stop();
            Logger.i(nameof(Program), $"Main: Readytime: {sw.ElapsedMilliseconds}ms");
            Logger.i(nameof(Program), $"Main: Server running at {server.BaseUrl}");

            cancellationTokenSource.Token.WaitHandle.WaitOne();

            cancellationTokenSource.Cancel();
            await server.StopServer();
            StateApp.Shutdown();
            Logger.DisposeStaticLogger();
        }
    }
}
