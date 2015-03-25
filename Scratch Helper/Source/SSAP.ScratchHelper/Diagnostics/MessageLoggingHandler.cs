using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSAP.ScratchHelper.Diagnostics
{
	public class MessageLoggingHandler : MessageHandler
	{
		protected override async Task IncommingMessageAsync( string correlationId, string requestInfo, byte[] message )
		{
			await Task.Run( () =>
				Console.WriteLine( string.Format( "{0} - Request: {1}\r\n{2}", correlationId, requestInfo, Encoding.UTF8.GetString( message ) ) ) );
		}

		protected override async Task OutgoingMessageAsync( string correlationId, string requestInfo, byte[] message )
		{
			await Task.Run( () =>
				Console.WriteLine( string.Format( "{0} - Response: {1}\r\n{2}", correlationId, requestInfo, Encoding.UTF8.GetString( message ) ) ) );
		}
	}
}
