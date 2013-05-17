using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Facebook;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace WP8HF
{
    public partial class FacebookLoginPage : PhoneApplicationPage
    {
        private const string applicationId = "192967284188977";
        private const string applicationSecret = "7228f8317acf48f9b8a73cdea3c1f390";

        public FacebookLoginPage()
        {
            InitializeComponent();
        }

        private void webFacebookLogin_Loaded_1(object sender, RoutedEventArgs e)
        {
            var client = new FacebookClient { AppId = applicationId };
            var parameters = new Dictionary<string, object>
            {
                { "response_type", "token" },
                { "redirect_uri", "https://www.facebook.com/connect/login_success.html"},
                { "scope", "publish_stream"},
                { "display", "touch"}
            };
            var loginUri = client.GetLoginUrl(parameters);
            wbrFacebookLogin.Navigate(loginUri);
        }

        private void webFacebookLogin_Navigated_1(object sender, NavigationEventArgs e)
        {
            FacebookOAuthResult result;
            var client = new FacebookClient();
            if (client.TryParseOAuthCallbackUrl(e.Uri, out result))
            {
                // kulcs-ertek parok eltarolasa
                IsolatedStorageSettings.ApplicationSettings["FacebookToken"] = result.AccessToken;
                IsolatedStorageSettings.ApplicationSettings.Save();
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            }
        }
    }
}