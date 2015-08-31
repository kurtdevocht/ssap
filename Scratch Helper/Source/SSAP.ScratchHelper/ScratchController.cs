using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace SSAP.ScratchHelper
{
	public class ScratchController : ApiController
	{
		static object s_lock = new object();
		static List<string> s_pollInputs = new List<string>();

		[Route( "poll" )]
		[HttpGet]
		public HttpResponseMessage Poll()
		{
			var resp = new HttpResponseMessage( HttpStatusCode.OK );
			var content = new StringBuilder();

			lock ( s_pollInputs )
			{
				foreach ( var i in s_pollInputs )
				{
					int value;
					if ( Globals.Arduino.TryGetValue( i, out value ) )
					{
						if ( i.ToUpper().StartsWith( "A" ) )
						{
							content.Append( i );
							content.Append( " " );
							content.Append( value );
							content.Append( '\n' );
						}
						else
						{
							content.Append( i );
							content.Append( " " );
							var valueString = value == 0 ? "false" : "true";
							content.Append( valueString );
							content.Append( '\n' );
                            
							content.Append( "~" );
							content.Append( i );
							content.Append( " " );
							valueString = value == 0 ? "true" : "false";
							content.Append( valueString );
							content.Append( '\n' );
						}
						
					}
				}
			}
			resp.Content = new StringContent( content.ToString(), Encoding.UTF8, "text/plain" );
			return resp;
		}

		[Route( "startPolling/{inputs}" )]
		[HttpGet]
		public HttpResponseMessage StartPolling( string inputs )
		{
			Console.WriteLine( "StartPolling: " + inputs );
            
            lock ( s_lock )
			{
				s_pollInputs.Clear();

				if ( string.IsNullOrEmpty( inputs ) )
				{
					return this.OkResponse;
				}

				inputs = inputs.Replace( ',', '|' );
				inputs = inputs.Replace( ';', '|' );
				inputs = inputs.Replace( '\t', '|' );
				inputs = inputs.Replace( ' ', '|' );
				inputs = inputs.Replace( " ", "" );

				s_pollInputs.AddRange(
					inputs.Split( '|' ).Where( s => !string.IsNullOrEmpty( s ) ) );

				Globals.Arduino.StartPoll( new List<string>(s_pollInputs) );
			}

			return this.OkResponse;
		}

		[Route( "setOutput/{output}/{valuePercent}" )]
		[HttpGet]
		public HttpResponseMessage SetOutput( string output, string valuePercent )
		{
			Console.WriteLine( string.Format( "SetOutput {0}: {1}%", output, valuePercent ) );

			var valuePercentInt = this.StringToInt( valuePercent );
			valuePercentInt = Math.Max( valuePercentInt, 0 );
			valuePercentInt = Math.Min( valuePercentInt, 100 );
			Globals.Arduino.SetOutputPercent( output, valuePercentInt );

			return this.OkResponse;
		}

		[Route( "setMotor/{output}/{angle}" )]
		[HttpGet]
		public HttpResponseMessage SetMotor( string output, string angle )
		{
			Console.WriteLine( string.Format( "SetMotor {0}: {1}°", output, angle ) );
			var angleInt = this.StringToInt( angle );
			Globals.Arduino.SetMotor( output, angleInt );

			return this.OkResponse;
		}

		private int StringToInt( string s )
		{
			var c = CultureInfo.CurrentCulture;
			s = s.Replace( ".", c.NumberFormat.NumberDecimalSeparator );
			s = s.Replace( ",", c.NumberFormat.NumberDecimalSeparator );

			double d;
			if ( double.TryParse( s, out d ) )
			{
				return (int)( d + 0.5 );
			}
			else
			{
				return 0;
			}
		}

		private HttpResponseMessage OkResponse
		{
			get
			{
				var resp = new HttpResponseMessage( HttpStatusCode.OK );
				resp.Content = new StringContent( string.Empty, Encoding.UTF8, "text/plain" );
				return resp;
			}
		}
	}
}
