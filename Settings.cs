using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace TinyTinyRss
{
    class Settings
    {
        private static Settings instance = null;

        private Settings()
        {
            ApplicationDataContainer data = ApplicationData.Current.RoamingSettings;
            object uri = data.Values["url"];
            if (uri is string)
                InstanceUri = new Uri((string) uri);
            object user = data.Values["user"];
            if(user is string)
                Username = (string)user;
            object password = data.Values["password"];
            if(password is string)
                Password = (string)password;
        }

        ~Settings()
        {
            SaveState();
        }

        public void SaveState()
        {
            ApplicationDataContainer data = ApplicationData.Current.RoamingSettings;
            data.Values["url"] = InstanceUri.AbsoluteUri;
            data.Values["user"] = Username;
            data.Values["password"] = Password;
        }

        public static Settings GetInstance()
        {
            if (instance == null)
                instance = new Settings();

            return instance;
        }

        public Uri InstanceUri { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
