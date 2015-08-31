using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSAP.ScratchHelper
{
	class Program
	{
		static void Main( string[] args )
		{
			/*
			Console.WindowWidth = 80;
			Console.WindowHeight = 60;
			 * */
			Console.Clear();
			Console.Title = "SSAP <> Scratch";
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine( @" ____  ____   __   ____     ____     ____   ___  ____   __  ____  ___  _  _ " );
			Console.WriteLine( @"/ ___)/ ___) / _\ (  _ \   / /\ \   / ___) / __)(  _ \ / _\(_  _)/ __)/ )( \" );
			Console.WriteLine( @"\___ \\___ \/    \ ) __/  ( (  ) )  \___ \( (__  )   //    \ )( ( (__ ) __ (" );
			Console.WriteLine( @"(____/(____/\_/\_/(__)     \_\/_/   (____/ \___)(__\_)\_/\_/(__) \___)\_)(_/" );
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine( "                      Load the SSAP sketch on an Arduino" );
			Console.WriteLine( "                         connect it to your computer" );
			Console.WriteLine( "                  and this application will make it possible" );
			Console.WriteLine("                     to control the Arduino in- and outputs");
			Console.WriteLine( "                        with Scratch 2 (offline edition)" );
			Console.WriteLine();
			Console.WriteLine();

			while ( !FindArduino() )
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine( "***************************************************************************" );
				Console.WriteLine( @" ______ _____  _____   ____  _____  _ " );
				Console.WriteLine( @"|  ____|  __ \|  __ \ / __ \|  __ \| |" );
				Console.WriteLine( @"| |__  | |__) | |__) | |  | | |__) | |" );
				Console.WriteLine( @"|  __| |  _  /|  _  /| |  | |  _  /| |" );
				Console.WriteLine( @"| |____| | \ \| | \ \| |__| | | \ \|_|" );
				Console.WriteLine( @"|______|_|  \_\_|  \_\\____/|_|  \_(_)" );
				Console.WriteLine();
				Console.WriteLine( "\tI didn't find an Arduino on any COM port :-(" );
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine();
				Console.WriteLine( "\t  - Verify all connections" );
				Console.WriteLine( "\t  - Make sure no other program is using the COM port" );
				Console.WriteLine( "\t  - Make sure the right sketch is loaded on your Arduino" );
				Console.WriteLine();
				Console.WriteLine( "\tI'll try again in 5 seconds..." );
				Console.WriteLine();
				Console.WriteLine();
				Thread.Sleep( 5000 );
			}

			Console.WriteLine();
			Console.WriteLine();

            var port = 15001;

			try
			{
				var startOptions = new StartOptions()
				{
                    Port = port,
                    ServerFactory = "Nowin"
				};

				// Start OWIN host 
				using ( var app = WebApp.Start<Startup>( startOptions ) )
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine( "***************************************************************************" );
					Console.WriteLine( @" __          __         _                         _ " );
					Console.WriteLine( @" \ \        / /        | |                       | |" );
					Console.WriteLine( @"  \ \  /\  / /__   ___ | |__   ___   _____      _| |" );
					Console.WriteLine( @"   \ \/  \/ / _ \ / _ \| '_ \ / _ \ / _ \ \ /\ / / |" );
					Console.WriteLine( @"    \  /\  / (_) | (_) | | | | (_) | (_) \ V  V /|_|" );
					Console.WriteLine( @"     \/  \/ \___/ \___/|_| |_|\___/ \___/ \_/\_/ (_)" );
					Console.WriteLine();
					Console.WriteLine( "\tAll is ok!" );
					Console.WriteLine( "\tListening on port " + port.ToString() + " for Scratch requests." );
					Console.WriteLine();
					Console.WriteLine( "\tPress enter to stop..." );
					Console.WriteLine();
					Console.WriteLine( "***************************************************************************" );
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.ReadLine();
					
				}
			}
			catch ( Exception ex )
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine( "***************************************************************************" );
				Console.WriteLine( "ERROR - Cannot listen on port " + port + " for Scratch requests: " + ex.ToString() );
				Console.WriteLine();
				Console.WriteLine( "Press enter to stop..." );
				Console.WriteLine();
				Console.WriteLine( "***************************************************************************" );
				Console.ReadLine();
			}
		}

		private static bool FindArduino()
		{

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine( "***************************************************************************" );
			Console.WriteLine( "Looking for Arduino's on all COM ports..." );
			Console.WriteLine();

            // Issue: When a "Intel Active Management Technology - SOL" COM port is available on the system,
            // discovery doesn't seem to work when a debugger is attached
            // (no response received from Arduino serial port if data has been sent to the Intel port)

            // var arduinos = new[] { new Arduino("COM7", true, true) };
            var arduinos =
                SerialPort.GetPortNames()
                .Select( p => new Arduino( p, true, true )) // RTS & DTR Should be enabled for Leonardo
                .ToList();

			foreach ( var a in arduinos )
			{
				Console.WriteLine( "\t" + a.ComPort + ": Wait a few seconds, I'm checking if there's an Arduino..." );
			}
			
			Task.WaitAll( arduinos.Select( a => a.OpenAsync() ).ToArray() );

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine( "***************************************************************************" );
			Console.WriteLine( "Result of all serial ports:" );
			Console.WriteLine();

			Arduino arduino = null;
			foreach ( var a in arduinos.OrderBy( a => a.ComPort ) )
			{
				if ( a.IsOpen )
				{
					if ( arduino == null )
					{
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine( "\t" + a.ComPort + ": Good Arduino! Will take that one..." );
						arduino = a;
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.WriteLine( "\t" + a.ComPort + ": Good Arduino too, but already took " + arduino.ComPort );
						a.Dispose();
					}
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine( "\t" + a.Error );
				}
			}

			if ( arduino == null )
			{
				return false;
			}

			Globals.Arduino = arduino;
			return true;
		}
	}
}
