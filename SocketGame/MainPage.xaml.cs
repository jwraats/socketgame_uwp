using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Networking.Sockets;
using Windows.UI.Core;
using Windows.Storage.Streams;
using Windows.Web;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
// https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/StreamSocket/cpp/


namespace SocketGame
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MessageWebSocket messageWebSocket;
        private DataWriter messageWriter;


        public MainPage()
        {
            this.InitializeComponent();
        }

        private bool TryGetUri(string uriString, out Uri uri)
        {
            uri = null;

            Uri webSocketUri;
            if (!Uri.TryCreate(uriString.Trim(), UriKind.Absolute, out webSocketUri))
            {
                System.Diagnostics.Debug.WriteLine("Error: Invalid URI");
                return false;
            }

            // Fragments are not allowed in WebSocket URIs.
            if (!String.IsNullOrEmpty(webSocketUri.Fragment))
            {
                System.Diagnostics.Debug.WriteLine("Error: URI fragments not supported in WebSocket URIs.");
                return false;
            }

            // Uri.SchemeName returns the canonicalized scheme name so we can use case-sensitive, ordinal string
            // comparison.
            if ((webSocketUri.Scheme != "ws") && (webSocketUri.Scheme != "wss"))
            {
                System.Diagnostics.Debug.WriteLine("Error: WebSockets only support ws:// and wss:// schemes.");
                return false;
            }

            uri = webSocketUri;

            return true;
        }

        private void Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Closed; Code: " + args.Code + ", Reason: " + args.Reason);

            if (messageWebSocket != null)
            {
                messageWebSocket.Dispose();
                messageWebSocket = null;
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Starting... ");
            bool connecting = true;
            if (String.IsNullOrEmpty(playerName.Text))
            {
                System.Diagnostics.Debug.WriteLine("Speler naam is niet aangegeven!");
                return;
            }

            try
            {
                if (messageWebSocket == null) //Connection is not there yet.. lets make the bloody connection!
                {
                    Uri server;
                    if (!TryGetUri("ws://192.168.178.105:4141", out server))
                    {
                        return;
                    }

                    //Server is now build..
                    messageWebSocket = new MessageWebSocket();
                    messageWebSocket.Control.MessageType = SocketMessageType.Utf8;
                    messageWebSocket.MessageReceived += MessageReceived;

                    // Dispatch close event on UI thread. This allows us to avoid synchronizing access to messageWebSocket.
                    messageWebSocket.Closed += async (senderSocket, args) =>
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Closed(senderSocket, args));
                    };

                    await messageWebSocket.ConnectAsync(server);
                    messageWriter = new DataWriter(messageWebSocket.OutputStream);
                    messageWriter.WriteString(playerName.Text);


                    await messageWriter.StoreAsync();
                }
                else if (messageWriter != null)
                {
                    messageWriter.WriteString(playerName.Text);
                    await messageWriter.StoreAsync();
                }
            } catch (Exception er)
            {
                if (connecting && messageWebSocket != null)
                {
                    messageWebSocket.Dispose();
                    messageWebSocket = null;
                }
                WebErrorStatus status = WebSocketError.GetStatus(er.GetBaseException().HResult);
            }
            
        }

        public void CloseConnection()
        {
            try
            {
                if (messageWebSocket != null)
                {
                    System.Diagnostics.Debug.WriteLine("Closing");
                    messageWebSocket.Close(1000, "Closed due to user request.");
                    messageWebSocket = null;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No active WebSocket, send something first");
                }
            }
            catch (Exception er)
            {
                WebErrorStatus status = WebSocketError.GetStatus(er.GetBaseException().HResult);
            }
        }

        private void MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Message Received; Type: " + args.MessageType);
                using (DataReader reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                    string read = reader.ReadString(reader.UnconsumedBufferLength);

                    //Convert to JSON
                    if (read != "")
                    {

                    }
                    System.Diagnostics.Debug.WriteLine(read);
                }
            }
            catch (Exception ex) // For debugging
            {
                WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);

                if (status == WebErrorStatus.Unknown)
                {
                    throw;
                }

                // Normally we'd use the status to test for specific conditions we want to handle specially,
                // and only use ex.Message for display purposes.  In this sample, we'll just output the
                // status for debugging here, but still use ex.Message below.
                System.Diagnostics.Debug.WriteLine("Error: " + status);

                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}
