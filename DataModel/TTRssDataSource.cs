using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web.Http;

// Le modèle de données défini par ce fichier sert d'exemple représentatif d'un modèle fortement typé
// modèle.  Les noms de propriétés choisis correspondent aux liaisons de données dans les modèles d'élément standard.
//
// Les applications peuvent utiliser ce modèle comme point de départ et le modifier à leur convenance, ou le supprimer complètement et
// le remplacer par un autre correspondant à leurs besoins. L'utilisation de ce modèle peut vous permettre d'améliorer votre application 
// réactivité en lançant la tâche de chargement des données dans le code associé à App.xaml lorsque l'application 
// est démarrée pour la première fois.

namespace TinyTinyRss.Data
{
    /// <summary>
    /// Modèle de données d'élément générique.
    /// </summary>
    public class RssArticle
    {
        public RssArticle(int uniqueId, String title, String subtitle, String imagePath, String content)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.ImagePath = imagePath;
            this.Content = content;
        }

        public int UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string ImagePath { get; private set; }
        public string Content { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Modèle de données de groupe générique.
    /// </summary>
    public class RssFeed
    {
        public RssFeed(int uniqueId, String title, String subtitle, String imagePath)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.ImagePath = imagePath;
            this.Items = new ObservableCollection<RssArticle>();
        }

        public int UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string ImagePath { get; private set; }
        public ObservableCollection<RssArticle> Items { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    public class InvalidConfigurationException : Exception
    {

        public InvalidConfigurationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Crée une collection de groupes et d'éléments dont le contenu est lu à partir d'un fichier json statique.
    /// 
    /// SampleDataSource initialise avec les données lues à partir d'un fichier json statique dans 
    /// projet.  Elle fournit des exemples de données à la fois au moment de la conception et de l'exécution.
    /// </summary>
    public sealed class TTRssDataSource
    {
        private static string _token = null;

        private static ObservableCollection<RssFeed> _groups = new ObservableCollection<RssFeed>();
        public static ObservableCollection<RssFeed> Groups
        {
            get { return _groups; }
        }

        private static async Task<JsonObject> QueryApi(JsonObject json)
        {
        reQuery:
            if(_token != null)
            {
                if(json.ContainsKey("sid"))
                    json.Remove("sid");
                json.Add("sid", JsonValue.CreateStringValue(_token));
            }
            Settings settings = Settings.GetInstance();

            if (settings.InstanceUri == null)
                throw new InvalidConfigurationException("URI not set");

            Uri uri = new Uri(settings.InstanceUri, "api/");
            HttpClient http = new HttpClient();
            HttpResponseMessage result = await http.PostAsync(uri, new HttpStringContent(json.Stringify()));
            JsonObject jsonRes;
            bool ok = JsonObject.TryParse(await result.Content.ReadAsStringAsync(), out jsonRes);
            if (!ok)
                throw new InvalidConfigurationException("Not JSON");

            if (jsonRes.GetNamedNumber("status") != 0)
            {
                if (jsonRes.GetNamedObject("content").GetNamedString("error") == "NOT_LOGGED_IN")
                {
                    await GetToken();
                    await QueryApi(json);
                    goto reQuery;
                }
                else if (jsonRes.GetNamedObject("content").GetNamedString("error") == "LOGIN_ERROR")
                    throw new InvalidConfigurationException("Login error");
            }
            return jsonRes;
        }

        private static async Task GetToken()
        {
            Settings settings = Settings.GetInstance();
            JsonObject json = new JsonObject();
            json.Add("op", JsonValue.CreateStringValue("login"));
            json.Add("user", JsonValue.CreateStringValue(settings.Username));
            json.Add("password", JsonValue.CreateStringValue(settings.Password));
            
            json = await QueryApi(json);

            _token = json.GetNamedObject("content").GetNamedString("session_id");
        }

        public static async Task<IEnumerable<RssFeed>> GetGroupsAsync()
        {
            JsonObject jsonRequest = new JsonObject();
            jsonRequest.Add("op", JsonValue.CreateStringValue("getFeeds"));
            jsonRequest.Add("cat_id", JsonValue.CreateNumberValue(-4));

            JsonObject jsonResponse = await QueryApi(jsonRequest);
            JsonArray feeds = jsonResponse.GetNamedArray("content");

            Groups.Clear();
            foreach(JsonValue val in feeds)
            {
                JsonObject feed = val.GetObject();
                string imageUri = "Assets/DarkGray.png";
                string unreadStr = "";

                if (feed.GetNamedBoolean("has_icon", false))
                    imageUri = Settings.GetInstance().InstanceUri + "/feed-icons/" + feed.GetNamedNumber("id").ToString() + ".ico";
                JsonValue unread = feed.GetNamedValue("unread");
                int unreadInt = -1;
                if (unread.ValueType.Equals(JsonValueType.Number))
                    unreadInt = (int) unread.GetNumber();
                if (unread.ValueType.Equals(JsonValueType.String))
                    unreadInt = int.Parse(unread.GetString());

                if (unreadInt != -1)
                    unreadStr = unreadInt + " unread";

                RssFeed rssFeed = new RssFeed((int) feed.GetNamedNumber("id"), feed.GetNamedString("title"), unreadStr, imageUri);
                Groups.Add(rssFeed);
            }
            return Groups;
        }

        public static async Task<RssFeed> GetGroupAsync(int uniqueId)
        {
            JsonObject jsonRequest = new JsonObject();
            jsonRequest.Add("op", JsonValue.CreateStringValue("getHeadlines"));
            jsonRequest.Add("feed_id", JsonValue.CreateNumberValue(uniqueId));
            jsonRequest.Add("show_content", JsonValue.CreateBooleanValue(true));

            JsonObject jsonResponse = await QueryApi(jsonRequest);
            JsonArray feeds = jsonResponse.GetNamedArray("content");

            var matches = Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            RssFeed feed = matches.First();
            feed.Items.Clear();

            foreach (JsonValue val in feeds)
            {
                JsonObject article = val.GetObject();
                string imagePath = "Assets/DarkGray.png";
                JsonValue feed_id = article.GetNamedValue("feed_id");
                int feed_id_int = -1;
                if (feed_id.ValueType.Equals(JsonValueType.Number))
                    feed_id_int = (int)feed_id.GetNumber();
                if (feed_id.ValueType.Equals(JsonValueType.String))
                    feed_id_int = int.Parse(feed_id.GetString());

                if (feed_id_int != -1 && Groups.Where((group) => group.UniqueId.Equals(feed_id_int)).First().ImagePath != imagePath)
                    imagePath = Settings.GetInstance().InstanceUri + "/feed-icons/" + feed_id_int + ".ico";

                feed.Items.Add(new RssArticle((int)article.GetNamedNumber("id"), article.GetNamedString("title"), article.GetNamedString("feed_title"), imagePath, article.GetNamedString("content")));
            }
            return feed;
        }

        public static async Task<RssArticle> GetItemAsync(int uniqueId)
        {
            JsonObject jsonRequest = new JsonObject();
            jsonRequest.Add("op", JsonValue.CreateStringValue("getArticle"));
            jsonRequest.Add("article_id", JsonValue.CreateNumberValue(uniqueId));

            JsonObject jsonResponse = await QueryApi(jsonRequest);
            JsonObject article = jsonResponse.GetNamedArray("content").First().GetObject();
            return new RssArticle((int) article.GetNamedNumber("id"), article.GetNamedString("title"), article.GetNamedString("author"), "Assets/DarkGray.png", article.GetNamedString("content"));
        }
       
    }
}