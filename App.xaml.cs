﻿using TinyTinyRss.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ApplicationSettings;

// Pour plus d'informations sur le modèle Application partagée, consultez la page http://go.microsoft.com/fwlink/?LinkId=234228

namespace TinyTinyRss
{
    /// <summary>
    /// Fournit un comportement spécifique à l'application afin de compléter la classe Application par défaut.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initialise l'objet Application singleton.  Il s'agit de la première ligne de code créé
        /// à être exécutée. Elle correspond donc à l'équivalent logique de main() ou WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoqué lorsque l'application est lancée normalement par l'utilisateur final.  D'autres points d'entrée
        /// seront utilisés par exemple au moment du lancement de l'application pour l'ouverture d'un fichier spécifique.
        /// </summary>
        /// <param name="e">Détails concernant la requête et le processus de lancement.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Ne répétez pas l'initialisation de l'application lorsque la fenêtre comporte déjà du contenu,
            // assurez-vous juste que la fenêtre est active

            if (rootFrame == null)
            {
                // Créez un Frame utilisable comme contexte de navigation et naviguez jusqu'à la première page
                rootFrame = new Frame();
                //Associez au frame une clé SuspensionManager                                
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                // Définir la page par défaut
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restaure l'état de session enregistré uniquement lorsque cela est approprié
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        //Un problème est survenu lors de la restauration de l'état.
                        //Partez du principe que l'état est absent et continuez
                    }
                }

                // Placez le frame dans la fenêtre active
                Window.Current.Content = rootFrame;
            }
            if (rootFrame.Content == null)
            {
                // Quand la pile de navigation n'est pas restaurée, accède à la première page,
                // puis configurez la nouvelle page en transmettant les informations requises en tant que
                // paramètre
                rootFrame.Navigate(typeof(ItemsPage), e.Arguments);
            }
            // Vérifiez que la fenêtre actuelle est active
            Window.Current.Activate();
        }
        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            // Enregistrement du SettingsFlyout auprès du système
            SettingsPane.GetForCurrentView().CommandsRequested += (s, e) =>
            {
                SettingsCommand defaultsCommand = new SettingsCommand("general", "General",
                    (handler) =>
                    {
                        GeneralSettingsFlyout sf = new GeneralSettingsFlyout();
                        sf.Unloaded += sf_Unloaded;
                        sf.Show();
                    });
                e.Request.ApplicationCommands.Add(defaultsCommand);
            };

            base.OnWindowCreated(args);
        }

        void sf_Unloaded(object sender, RoutedEventArgs e)
        {
            // Sauvegarde des paramètres lorsque le SettingsFlyout est fermé
            Settings.GetInstance().SaveState();
        }

        /// <summary>
        /// Appelé lorsque la navigation vers une page donnée échoue
        /// </summary>
        /// <param name="sender">Frame à l'origine de l'échec de navigation.</param>
        /// <param name="e">Détails relatifs à l'échec de navigation</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Appelé lorsque l'exécution de l'application est suspendue.  L'état de l'application est enregistré
        /// sans savoir si l'application pourra se fermer ou reprendre sans endommager
        /// le contenu de la mémoire.
        /// </summary>
        /// <param name="sender">Source de la requête de suspension.</param>
        /// <param name="e">Détails de la requête de suspension.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            Settings.GetInstance().SaveState();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}
