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

// Pour en savoir plus sur le modèle d'élément du menu volant des paramètres, consultez la page http://go.microsoft.com/fwlink/?LinkId=273769

namespace TinyTinyRss
{
    /// <summary>
    /// SettingsFlyout général de l'application
    /// Lié à l'objet Settings global
    /// </summary>
    public sealed partial class GeneralSettingsFlyout : SettingsFlyout
    {
        private Settings settings;

        public GeneralSettingsFlyout()
        {
            this.InitializeComponent();

            settings = Settings.GetInstance();

            if(settings.InstanceUri != null)
                tbxUrl.Text = settings.InstanceUri.AbsoluteUri;
            if(settings.Username != null)
                tbxUser.Text = settings.Username;
            if(settings.Password != null)
                tbxPassword.Password = settings.Password;
        }

        private void saveSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.InstanceUri = new Uri(tbxUrl.Text);
            }
            catch
            {
            }
            settings.Username = tbxUser.Text;
            settings.Password = tbxPassword.Password;
        }
    }
}
