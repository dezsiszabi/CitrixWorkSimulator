using Microsoft.Win32;
using System.Timers;

namespace CitrixWorkSimulator
{
    class Program
    {
        private static WFICALib.ICAClientClass? _icaClient = null;
        private static bool _isSendingKeys = false;

        private static int _enumHandle;
        private static string? _sessionId;
        
        private static System.Timers.Timer _timer = new System.Timers.Timer(1_000);

        static void Main(string[] args)
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine("ERROR: Only Windows platform is supported! Exiting...");
                return;
            }

            var subkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Citrix\ICA Client\CCM", false);

            if (subkey == null)
            {
                Console.WriteLine(@"ERROR: HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Citrix\ICA Client\CCM doesn't exist! Exiting...");
                return;
            }

            var allowSimulationApiValue = subkey.GetValue("AllowSimulationAPI");

            if (allowSimulationApiValue == null)
            {
                Console.WriteLine(@"ERROR: HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Citrix\ICA Client\CCM\AllowSimulationAPI doesn't exist! Exiting...");
                return;
            }

            if (allowSimulationApiValue is not int || (int)allowSimulationApiValue != 1)
            {
                Console.WriteLine(@"ERROR: HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Citrix\ICA Client\CCM\AllowSimulationAPI has to be a DWORD with value 1! Exiting...");
                return;
            }

            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: No .ica path was provided! Exiting...");
                return;
            }

            var icaPath = args[0];

            if (!File.Exists(icaPath))
            {
                Console.WriteLine("ERROR: The .ica file provided doesn't exist! Exiting...");
                return;
            }

            _timer.Elapsed += SimulationLoop;

            _icaClient = new WFICALib.ICAClientClass();
            _icaClient.CacheICAFile = false;
            _icaClient.ICAFile = Path.GetFullPath(icaPath);
            _icaClient.OutputMode = WFICALib.OutputMode.OutputModeNormal;
            _icaClient.Launch = true;
            _icaClient.TWIMode = true;

            _icaClient.OnConnect += IcaClient_OnConnect;

            _icaClient.Connect();

            ConsoleKeyInfo? key = null;

            Console.WriteLine("Press Escape to exit the program");
            Console.WriteLine("Press Space to start/pause the simulation (currently paused)");
            Console.WriteLine("Press Up/Down to change the simulation interval (currently 1000 ms)");

            do
            {
                key = Console.ReadKey();
                switch (key.Value.Key) {
                    case ConsoleKey.Spacebar:
                        {
                            _isSendingKeys = !_isSendingKeys;

                            if (_isSendingKeys)
                            {
                                Console.WriteLine("Simulation started...");
                                _timer.Start();
                            }
                            else
                            {
                                Console.WriteLine("Simulation stopped...");
                                _timer.Stop();
                            }
                            break;
                        }
                    case ConsoleKey.UpArrow:
                        {
                            _timer.Interval = Math.Min(_timer.Interval + 1000, 60_000);
                            Console.WriteLine($"Interval set to: {_timer.Interval} ms");
                            break;
                        }
                    case ConsoleKey.DownArrow:
                        {
                            _timer.Interval = Math.Max(_timer.Interval - 1000, 1_000);
                            Console.WriteLine($"Interval set to: {_timer.Interval} ms");
                            break;
                        }
                }
            } while (key?.Key != ConsoleKey.Escape);

            _timer.Stop();
            _timer.Elapsed -= SimulationLoop;
            _timer.Dispose();

            _icaClient.StopMonitoringCCMSession(_sessionId);
            _icaClient.CloseEnumHandle(_enumHandle);
        }

        private static void IcaClient_OnConnect()
        {
            _enumHandle = _icaClient!.EnumerateCCMSessions();
            _sessionId = _icaClient!.GetEnumNameByIndex(_enumHandle, 0);
            _icaClient.StartMonitoringCCMSession(_sessionId, true);
        }

        private static void SimulationLoop(object? sender, ElapsedEventArgs e)
        {
            _icaClient!.Session.Keyboard.SendKeyDown(65);
            _icaClient!.Session.Keyboard.SendKeyUp(65);
        }
    }
}
