using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telerik.Everlive.Sdk.Core;
using Telerik.Everlive.Sdk.WindowsPhone;

namespace PushTestApp
{
	public partial class MainPage : PhoneApplicationPage
	{

		// Constructor
		public MainPage()
		{
			InitializeComponent();

			if (string.IsNullOrWhiteSpace(ConnectionSettings.EverliveApiKey) || ConnectionSettings.EverliveApiKey == "your-api-key-here")
			{
				MessageBox.Show("Hi there!\n\nBefore you can use this demo, you must insert your API key in the code.\n\nPlease go to ConnectionSettings.cs and put the API key for your Everlive application.", "API Key needed", MessageBoxButton.OK);
				this.ContentPanel.Visibility = Visibility.Collapsed;
				ConnectionSettings.ThrowError();
			}
			else
			{
				//Initialize the connection to the Everlive service
				var settings = new EverliveAppSettings()
				{
					ApiKey = ConnectionSettings.EverliveApiKey,
					UseHttps = ConnectionSettings.EverliveUseHttps,
					DateTimeUnspecifiedHandling = DateTimeUnspecifiedHandling.TreatAsUtc
				};
				if (!string.IsNullOrWhiteSpace(ConnectionSettings.EverliveHost)) settings.ServiceUrl = ConnectionSettings.EverliveHost;
				this.app = new EverliveApp(settings);

				//initialize progress indicator
				prog = new ProgressIndicator();
				prog.IsVisible = false;
				prog.IsIndeterminate = true;
				SystemTray.SetProgressIndicator(this, prog);

				this.OnPushDisabled();
			}
		}

		private async void InitializeNotificationChannel()
		{
			#region UI logic
			this.InitializePushButton.IsEnabled = false;
			this.PushStatusTextBlock.Text = "initializing...";
			this.PushStatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
			this.ShowProgress();
			#endregion

			try
			{
				//initialize push functionality
				var pushSettings = new PushSettings()
				{
					AutoBindToShellTile = true,
					AutoBindToShellToast = true,
					ServiceName = ConnectionSettings.PushServiceName
				};
				this.initializationResult = await this.app.WorkWith().Push().CurrentDevice.Initialize(pushSettings).ExecuteAsync();

				this.OnPushTokenAvailable();
			}
			catch (Exception ex)
			{
				MessageBox.Show("There was an error initializing the push functionality.");
				this.InitializePushButton.IsEnabled = true;

				this.PushStatusTextBlock.Text = "error";
				this.PushStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
			}

		}

		private async Task DisablePushNotifications()
		{
			//Unregister the device from Everlive
			await this.UnregisterDevice();

			//Invalidate the notification channel
			this.app.WorkWith().Push().CurrentDevice.DisableNotifications();

			#region UI logic

			this.OnPushDisabled();

			#endregion
		}

		private async Task OnPushTokenAvailable()
		{
			#region UI logic
			this.Dispatcher.BeginInvoke(delegate()
			{
				this.PushStatusTextBlock.Text = "ready";
				this.PushStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

				this.PushTokenTextBlock.Text = this.initializationResult.NotificationChannel.ChannelUri.ToString();
				this.PushTokenTextBlock.Foreground = new SolidColorBrush(Colors.Green);

				this.DeviceIdTextBlock.Text = WindowsPhonePushHelper.GetDeviceHardwareId();
				this.DeviceIdTextBlock.Foreground = new SolidColorBrush(Colors.Green);

			});
			#endregion

			//Get the registration for the current device
			var deviceRegistration = await this.app.WorkWith().Push().CurrentDevice.GetRegistration().ExecuteAsync();

			#region UI logic

			this.OnPushEnabled();

			if (deviceRegistration == null)
			{
				//The device is not registered
				this.OnDeviceUnregistered();
			}
			else 
			{
				//The device is already registered
				this.OnDeviceRegistered();
			}

			this.HideProgress();

			#endregion
		}

		private async Task OnDeviceRegistered()
		{
			this.Dispatcher.BeginInvoke(delegate()
			{
				this.RegistrationStatusTextBlock.Text = "active, will receive notifications";
				this.RegistrationStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

				this.RegisterDeviceButton.IsEnabled = false;
				this.UnregisterDeviceButton.IsEnabled = true;
			});
		}

		private async Task OnDeviceUnregistered()
		{
			this.Dispatcher.BeginInvoke(delegate()
			{
				this.RegistrationStatusTextBlock.Text = "inactive, will not receive notifications";
				this.RegistrationStatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);

				this.RegisterDeviceButton.IsEnabled = true;
				this.UnregisterDeviceButton.IsEnabled = false;
			});
		}

		private async Task OnPushEnabled()
		{
			this.Dispatcher.BeginInvoke(delegate()
			{
				this.InitializePushButton.IsEnabled = false;
				this.InitializePushButton.Visibility = Visibility.Collapsed;

				this.DisablePushButton.IsEnabled = true;
				this.DisablePushButton.Visibility = Visibility.Visible;
			});
		}

		private async Task OnPushDisabled()
		{
			this.Dispatcher.BeginInvoke(delegate()
			{
				this.RegistrationStatusTextBlock.Text = "inactive, will not receive notifications";
				this.RegistrationStatusTextBlock.Foreground = new SolidColorBrush(Colors.Gray);

				this.PushStatusTextBlock.Text = "not initialized";
				this.PushStatusTextBlock.Foreground = new SolidColorBrush(Colors.Gray);

				this.PushTokenTextBlock.Text = "not available";
				this.PushTokenTextBlock.Foreground = new SolidColorBrush(Colors.Gray);

				this.InitializePushButton.IsEnabled = true;
				this.InitializePushButton.Visibility = Visibility.Visible;
				

				this.DisablePushButton.IsEnabled = false;
				this.DisablePushButton.Visibility = Visibility.Collapsed;
			});
		}

		private async Task RegisterDevice()
		{
			this.ShowProgress();

			var result = await this.app.WorkWith().Push().CurrentDevice.Register(null).TryExecuteAsync();
			if (result.Success)
			{
				this.OnDeviceRegistered();
			}
			else
			{
				this.ShowErrorMessage("Error registering device", result.Error.Message);
			}

			this.HideProgress();
		}

		private async Task UnregisterDevice()
		{
			this.ShowProgress();

			var result = await this.app.WorkWith().Push().CurrentDevice.Unregister().TryExecuteAsync();

			if (result.Success)
			{
				this.OnDeviceUnregistered();
			}
			else
			{
				this.ShowErrorMessage("Error unregistering device", result.Error.Message);
			}

			this.HideProgress();
		}

		#region UI logic

		private void ShowProgress()
		{
			Dispatcher.BeginInvoke(delegate()
			{
				prog.IsVisible = true;
			});
		}

		private void HideProgress()
		{
			Dispatcher.BeginInvoke(delegate()
			{
				prog.IsVisible = false;
			});
		}

		private void ShowErrorMessage(string title, string message)
		{
			Dispatcher.BeginInvoke(delegate() {
				MessageBox.Show(message, title, MessageBoxButton.OK);
			});
		}

		#endregion

		#region Event Handlers

		private void InitializePushButton_Click(object sender, RoutedEventArgs e)
		{
			this.InitializeNotificationChannel();
		}

		private void DisablePushButton_Click(object sender, RoutedEventArgs e)
		{
			this.DisablePushNotifications();
		}

		private void RegisterDeviceButton_Click(object sender, RoutedEventArgs e)
		{
			this.RegisterDeviceButton.IsEnabled = false;
			this.RegisterDevice();
		}

		private void UnregisterDeviceButton_Click(object sender, RoutedEventArgs e)
		{
			this.UnregisterDeviceButton.IsEnabled = false;
			this.UnregisterDevice();
		}

		private void ApplicationBarIconButton_Click(object sender, EventArgs e)
		{
			StandardTileData standardTileData = new StandardTileData();
			standardTileData.BackgroundImage = new Uri("/Images/Everlive.png", UriKind.Relative);
			standardTileData.Title = "Secondary tile";
			standardTileData.Count = 0;
			standardTileData.BackTitle = "";
			standardTileData.BackContent = "";
			standardTileData.BackBackgroundImage = null;
			ShellTile tiletopin = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("LiveTilePage1.xaml"));
			if (tiletopin == null)
			{
				MessageBox.Show("We will now pin a secondary tile for you.\n\n" + secondaryPushHelpMessage, "Secondary tile", MessageBoxButton.OK);
				ShellTile.Create(new Uri("/LiveTilePage1.xaml", UriKind.Relative), standardTileData);
			}
			else
			{
				MessageBox.Show("Hey, it seems that the secondary tile is already pinned.\n\n" + secondaryPushHelpMessage, "Tile already pinned", MessageBoxButton.OK);
			}
		}

		private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
		{
			MessageBox.Show("The primary tile can only be pinned from outside the app. Find 'Everlive Push Sample' in the apps list, tap-and-hold over it, then choose 'pin to start'.", "Primary tile", MessageBoxButton.OK);
		}

		private void ApplicationBarMenuItem_Click_1(object sender, EventArgs e)
		{
			MessageBox.Show("You can pin a secondary tile by tapping the pin icon in the app bar.\n\n" + secondaryPushHelpMessage, "Secondary tile", MessageBoxButton.OK);
		}

		#endregion

		#region Private Fields and Constants

		/// <summary>
		/// The entry point to the Everlive service
		/// </summary>
		private EverliveApp app;

		/// Holds the active push channel.
		private InitializationResult initializationResult;

		/// <summary>
		/// Progress indicator for the UI
		/// </summary>
		private ProgressIndicator prog;

		private string secondaryPushHelpMessage = "You can update it with push notification by specifying:\n\nTileId=/LiveTilePage1.xaml";

		#endregion




	}
}