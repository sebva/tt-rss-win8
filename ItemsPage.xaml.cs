using TinyTinyRss.Common;
using TinyTinyRss.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Security.Credentials.UI;

// Pour en savoir plus sur le modèle d'élément Page Éléments, consultez la page http://go.microsoft.com/fwlink/?LinkId=234233

namespace TinyTinyRss
{
    /// <summary>
    /// Page affichant une collection d'aperçus d'éléments.  Dans le modèle Application fractionnée, cette page
    /// est utilisée pour afficher et sélectionner l'un des groupes disponibles.
    /// </summary>
    public sealed partial class ItemsPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// NavigationHelper est utilisé sur chaque page pour faciliter la navigation et 
        /// gestion de la durée de vie des processus
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Cela peut être remplacé par un modèle d'affichage fortement typé.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public ItemsPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
        }

        /// <summary>
        /// Remplit la page à l'aide du contenu passé lors de la navigation. Tout état enregistré est également
        /// fourni lorsqu'une page est recréée à partir d'une session antérieure.
        /// </summary>
        /// <param name="sender">
        /// La source de l'événement ; en général <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Données d'événement qui fournissent le paramètre de navigation transmis à
        /// <see cref="Frame.Navigate(Type, Object)"/> lors de la requête initiale de cette page et
        /// un dictionnaire d'état conservé par cette page durant une session
        /// antérieure.  L'état n'aura pas la valeur Null lors de la première visite de la page.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: créez un modèle de données approprié pour le domaine posant problème pour remplacer les exemples de données

            try
            {
                var sampleDataGroups = await TTRssDataSource.GetGroupsAsync();
                this.DefaultViewModel["Items"] = sampleDataGroups;
            }
            catch(InvalidConfigurationException ex)
            {
                // Si la configuration est incorrecte, alors le SettingsFlyout est ouvert pour que l'utilisateur corrige les informations
                GeneralSettingsFlyout sf = new GeneralSettingsFlyout();
                sf.Unloaded += sf_Unloaded;
                sf.ShowIndependent();
            }
        }

        void sf_Unloaded(object sender, RoutedEventArgs e)
        {
            // Quand le SettingsFlyout est fermé, on enregistre les paramètres et on actualise les flux
            Settings.GetInstance().SaveState();
            navigationHelper_LoadState(null, null);
        }

        /// <summary>
        /// Invoqué lorsqu'un utilisateur clique sur un élément.
        /// </summary>
        /// <param name="sender">GridView (ou ListView lorsque l'état d'affichage de l'application est Snapped)
        /// affichant l'élément sur lequel l'utilisateur a cliqué.</param>
        /// <param name="e">Données d'événement décrivant l'élément sur lequel l'utilisateur a cliqué.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Accédez à la page de destination souhaitée, puis configurez la nouvelle page
            // en transmettant les informations requises en tant que paramètre de navigation.
            var groupId = ((RssFeed)e.ClickedItem).UniqueId;
            this.Frame.Navigate(typeof(SplitPage), groupId);
        }

        #region Inscription de NavigationHelper

        /// Les méthodes fournies dans cette section sont utilisées simplement pour permettre
        /// NavigationHelper pour répondre aux méthodes de navigation de la page.
        /// 
        /// La logique spécifique à la page doit être placée dans les gestionnaires d'événements pour  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// et <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// Le paramètre de navigation est disponible dans la méthode LoadState 
        /// en plus de l'état de page conservé durant une session antérieure.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            navigationHelper_LoadState(null, null);
        }
    }
}
