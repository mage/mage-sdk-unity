using System;

using Wizcorp.MageSDK.MageClient.Message.Transports;

#if UNITY_EDITOR
using Wizcorp.MageSDK.Editor;
#endif

namespace Wizcorp.MageSDK.MageClient.Message
{
	public class MessageStream
	{
		private TransportClient transport;
		private const string MessageStreamPath = "msgstream";

		public MessageStream(TransportType type = TransportType.LONGPOLLING)
		{
			RegisterListener();

			switch (type)
			{
				case TransportType.LONGPOLLING:
					transport = new LongPolling();
					break;
				case TransportType.SHORTPOLLING:
					transport = new ShortPolling();
					break;
				default:
					throw new Exception("Unknown TransportType for the message stream");
			}
		}

		// Updates URI and credentials 
		public void SetEndpoint(string url, string login = null, string pass = null)
		{
			string streamUrl = string.Format("{0}/{1}", url, MessageStreamPath);
			transport.Config = new MessageStreamConfig(streamUrl, login, pass);
		}

		// Stop the transport client if it exists
		public void Dispose()
		{
			if (transport == null)
			{
				return;
			}

			transport.Stop();
			transport.Reset();
			transport.Config.Session = null;
		}


		private void RegisterListener()
		{
			// Start transport client when session is acquired
			Mage.Instance.EventManager.On("session.set", (sender, session) => {
				transport.Config.Session = UnityEngine.WWW.EscapeURL(session["key"].ToString());
				transport.Start();
			});

			// Stop the message client when session is lost
			Mage.Instance.EventManager.On("session.unset", (sender, reason) => {
				Dispose();
			});

			// Also stop the message client when the editor is stopped
			#if UNITY_EDITOR
			UnityEditorPlayMode.OnEditorModeChanged += newState => {
				if (newState == EditorPlayModeState.Stopped)
				{
					Dispose();
				}
			};
			#endif
		}
	}
}