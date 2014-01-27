using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushTestApp
{
	public static class ConnectionSettings
	{
		/// <summary>
		/// Input your API key below to connect to your own app.
		/// </summary>
		public static string EverliveApiKey = "your-api-key-here";

		public static void ThrowError()
		{
			throw new Exception(
				"Please fill in your API key above and restart the app."
			);
		}

		#region Additional settings (no changes required)

		/// <summary>
		/// The host and port for the Everlive service.
		/// </summary>
		public static string EverliveHost = null;

		/// <summary>
		/// Specified whether to use HTTPS when communicating with Everlive.
		/// </summary>
		public static bool EverliveUseHttps = false;

		/// <summary>
		/// The ServiceName parameter used when creating the notification channel. Only used when sending authenticated push notifications.
		/// </summary>
		public static string PushServiceName = null;

		#endregion
	}
}
