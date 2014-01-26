using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace TinyTinyRss
{
    /// <summary>
    /// Classe singleton qui permet l'enregistrement et la restoration des paramètres généraux de l'application.
    /// Se synchronise entre les différents PC via RoamingSettings
    /// </summary>
    class Settings
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static Settings instance = null;

        private Settings()
        {
            // Restauration des valeurs si applicable
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

        /// <summary>
        /// Enregistre les valeurs dans RoamingSettings
        /// </summary>
        public void SaveState()
        {
            ApplicationDataContainer data = ApplicationData.Current.RoamingSettings;
            data.Values["url"] = InstanceUri.AbsoluteUri;
            data.Values["user"] = Username;
            data.Values["password"] = Password;
        }

        /// <summary>
        /// Retourne l'instance de Settings
        /// </summary>
        /// <returns>L'objet Settings global</returns>
        public static Settings GetInstance()
        {
            if (instance == null)
                instance = new Settings();

            return instance;
        }

        /// <summary>
        /// L'URL du serveur Tiny Tiny RSS
        /// </summary>
        public Uri InstanceUri { get; set; }
        /// <summary>
        /// Le nom d'utilisateur pour accéder à TT-RSS
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Le mot de passe permettant l'accès à TT-RSS
        /// </summary>
        public string Password { get; set; }
    }
}
