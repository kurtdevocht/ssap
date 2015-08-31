using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSAP.ScratchHelper
{
	class Arduino : IDisposable
	{
		private const string c_expectedFirmware = "SSAP_V01"; // The Arduino should return this string when a '?' is sent
		private string m_error; // If OpenAsync fails, this variable will contain a description of what went wrong
		private string m_comPort;
		private SerialPort m_serialPort;
		private object m_lockSerialPort = new object(); // Used to synchronize access to the serial port
		private object m_lockResponses = new object();
		private bool m_isOpen;

		private ManualResetEvent m_responseReceived = new ManualResetEvent( false );
		private bool m_dtr;
		private bool m_rts;
		Queue<string> m_responses = new Queue<string>();
		private StringBuilder m_responsePending = new StringBuilder();

		private object m_valuesLock = new object();
		private Dictionary<string, int> m_values = new Dictionary<string, int>();

		private object m_pollStringLock = new object();
		private string m_pollString = null;

		public Arduino( string comPort, bool dtr = false, bool rts = false )
		{
			m_comPort = comPort;
			m_dtr = dtr;
			m_rts = rts;
		}

		public string ComPort
		{
			get
			{
				return m_comPort;
			}
		}

		public bool Dtr
		{
			get
			{
				return m_dtr;
			}
		}

		public bool Rts
		{
			get
			{
				return m_rts;
			}
		}

		public bool IsOpen
		{
			get
			{
				return m_isOpen;
			}
		}

		public Task<bool> OpenAsync()
		{
			return Task<bool>.Factory.StartNew( () =>
				{
					// Step 1 - Try to open COM port
					try
					{
						m_serialPort = new SerialPort( m_comPort, 115200 );
						m_serialPort.DtrEnable = m_dtr;
						m_serialPort.RtsEnable = m_rts;
						m_serialPort.WriteTimeout = 1000;
                        m_serialPort.ReadTimeout = 100;
                        m_serialPort.DataBits = 8;
                        m_serialPort.StopBits = StopBits.One;
                        m_serialPort.Parity = Parity.None;
             
						m_serialPort.Open();
					}
					catch ( Exception ex )
					{
						this.Error = m_comPort + ": Unable to open - " + ex.Message;
						return m_isOpen;
					}

					// Step 2 - Send a '?' and check if the right firmware string is returned
					try
					{
						var buff = new byte[ 256 ];

						lock ( m_lockSerialPort )
						{

							m_serialPort.DataReceived += m_serialPort_DataReceived;
							m_responses.Clear();
							m_serialPort.DiscardInBuffer();
							m_serialPort.DiscardOutBuffer();

							var message = "WHO ARE YOU?\r";
							m_serialPort.Write( message.ToArray(), 0, message.Length );

						}

						if ( !m_responseReceived.WaitOne( TimeSpan.FromMilliseconds( 2000 ) ) )
						{
							m_serialPort.Close();
							m_serialPort.Dispose();
							this.Error = m_comPort + ": No response received";
							return m_isOpen;
						}

						string response = null;
						lock ( m_lockResponses )
						{
							response = m_responses.Dequeue();
							if ( m_responses.Count <= 0 )
							{
								m_responseReceived.Reset();
							}
						}


						if ( string.IsNullOrEmpty( response ) )
						{
							this.Error = m_comPort + ": No valid Arduino - empty firmware response";
							m_serialPort.Close();
							m_serialPort.Dispose();
							return m_isOpen;
						}

						if ( !response.StartsWith( c_expectedFirmware ) )
						{
							this.Error = m_comPort + ": No valid Arduino - expected firmware " + c_expectedFirmware + ", but Arduino returned" + response;
							m_serialPort.Close();
							m_serialPort.Dispose();
							return m_isOpen;
						}

						// Valid response received in time => All OK
						m_isOpen = true;
						return m_isOpen;
					}
					catch ( Exception ex )
					{
						this.Error = m_comPort + ": Unable to send data - " + ex.Message;
						m_serialPort.Close();
						m_serialPort.Dispose();
						return m_isOpen;
					}
				} );
		}

		public string Error
		{
			get
			{
				return m_error;
			}
			private set
			{
				m_error = value;
			}
		}

		void m_serialPort_DataReceived( object sender, SerialDataReceivedEventArgs e )
		{
            /*
            if (e.EventType != SerialData.Chars)
            {
                return;
            }
			*/
			try
            { 
                lock( m_lockSerialPort )
                {
                    var buffer = new char[1];
                    while (m_serialPort.Read(buffer, 0, 1) > 0)
                    {
                        var c = buffer[0];
                        if ("\r\n".Contains(c))
                        {
                            if (m_responsePending.Length > 0)
                            {
                                lock (m_lockResponses)
                                {
                                    m_responses.Enqueue(m_responsePending.ToString());
                                    m_responseReceived.Set();
                                    m_responsePending.Clear();
                                }
                            }
                        }
                        else
                        {
                            m_responsePending.Append(c);
                        }
                    }
                }
			}
			catch
			{
				// Can't do anything here...
			}
		}

		// Starts polling the values of the specified inputs
		public void StartPoll( IEnumerable<string> inputs )
		{
			// Build a string to request all inputs
			lock ( m_pollStringLock )
			{
				// Check if this has been done before => In that case there's no need to restart the tasks
				bool wasPolling = m_pollString != null;

				var builder = new StringBuilder();
				foreach ( var s in inputs )
				{
					builder.Append( s );
					builder.Append( "?\r" );
				}

				m_pollString = builder.ToString();

				if ( wasPolling )
				{
					return;
				}
			}

            // Start a task to do the actual polling
			Task.Run(
				() =>
				{
					while ( true )
					{
                        
						lock ( m_lockSerialPort )
						{
							lock ( m_pollStringLock )
							{
								try
								{
									m_serialPort.Write( m_pollString );
								}
								catch
								{
									// We'll try again...
								}
							}
						}
                        

						Thread.Sleep( 30 );
					}
				}
			);


			// Start a task to process the return values
			Task.Run(
				() =>
				{
					while ( true )
					{
						m_responseReceived.WaitOne( -1 );
						string response = null;
						lock ( m_lockResponses )
						{
							response = m_responses.Dequeue();
							if ( m_responses.Count <= 0 )
							{
								m_responseReceived.Reset();
							}
						}

						if ( string.IsNullOrEmpty( response ) )
						{
							continue;
						}

						var parts = response.Split( ':' );
						if ( parts.Length != 2 )
						{
							continue;
						}

						if ( string.IsNullOrEmpty( parts[ 0 ] ) || string.IsNullOrEmpty( parts[ 1 ] ) )
						{
							continue;
						}

						int value = 0;
						if ( !int.TryParse( parts[ 1 ], out value ) )
						{
							continue;
						}

						lock ( m_values )
						{
							m_values[ parts[ 0 ] ] = value;
						}
					}
				}
				);
		}

		public bool TryGetValue( string key, out int value )
		{
			lock ( m_values )
			{
				return m_values.TryGetValue( key, out value );
			}
		}

		public void SetOutputPercent( string pin, int valuePercent )
		{
			var builder = new StringBuilder();
			builder.Append( pin );
			builder.Append( ":" );
			builder.Append( valuePercent );
			builder.Append( "%\r" );
			lock ( m_lockSerialPort )
			{
				m_serialPort.Write( builder.ToString() );
			}
		}

		public void SetMotor( string pin, int angle )
		{
			var builder = new StringBuilder();
			builder.Append( pin );
			builder.Append( ":" );
			builder.Append( angle );
			builder.Append( "*\r" );
			lock ( m_lockSerialPort )
			{
				m_serialPort.Write( builder.ToString() );
			}
		}

		public void Dispose()
		{
			m_serialPort.Dispose();
			m_responseReceived.Dispose();
		}
	}
}
