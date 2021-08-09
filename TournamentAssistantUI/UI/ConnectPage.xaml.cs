﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using TournamentAssistantUI.Misc;

namespace TournamentAssistantUI.UI
{
    /// <summary>
    /// Interaction logic for ConnectPage.xaml
    /// </summary>
    public partial class ConnectPage : Page
    {
        public ConnectPage()
        {
            InitializeComponent();

#if DEBUG
            //MockButton.Visibility = Visibility.Visible;
            WinConsole.Initialize();
#endif
        }

        private void Mock_Click(object sender, RoutedEventArgs e)
        {
            var navigationService = NavigationService.GetNavigationService(this);
            navigationService.Navigate(new MockPage());
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            var hostText = HostIP.Text.Split(':');

            var navigationService = NavigationService.GetNavigationService(this);
            navigationService.Navigate(new MainPage(hostText[0], hostText.Length > 1 ? int.Parse(hostText[1]) : 10156, string.IsNullOrEmpty(Username.Text) ? "Coordinator" : Username.Text, Password.Text));
        }
    }
}
