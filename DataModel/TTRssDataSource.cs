using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web.Http;


namespace TinyTinyRss.Data
{
    /// <summary>
    /// Représente un article d'un flux RSS
    /// </summary>
    public class RssArticle : INotifyPropertyChanged
    {
        public enum RssArticleFlag 
        {
            Starred = 0, Read = 2, Published = 1
        }

        public RssArticle(int uniqueId, String title, String subtitle, String imagePath, String content, bool isRead, Uri link)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.ImagePath = imagePath;
            this.Content = content;
            this.IsRead = isRead;
            this.Link = link;
        }

        public int UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string ImagePath { get; private set; }
        public string Content { get; private set; }
        private bool _isRead;
        public bool IsRead
        {
            get { return _isRead; }
            set
            {
                this._isRead = value;
                this.onPropertyChanged("IsRead");
            }
        }
        public Uri Link { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void onPropertyChanged(string property)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }   
}

    /// <summary>
    /// Représente un flux RSS contenant des articles. Peut être une aggrégation de plusieurs flux.
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
    /// Datasource pour Tiny Tiny RSS
    /// </summary>
    public sealed class TTRssDataSource
    {
        /// <summary>
        /// Jeton d'accès au serveur, si déjà obtenu
        /// </summary>
        private static string _token = null;

        private static ObservableCollection<RssFeed> _groups = new ObservableCollection<RssFeed>();
        /// <summary>
        /// CSS permettant l'affichage du contenu des articles en blanc avec police Windows
        /// </summary>
        private static string kHead = "<head><style type=\"text/css\">*{color:white;} body{font-family:\"Segoe UI\"}</style></head>";
        public static ObservableCollection<RssFeed> Groups
        {
            get { return _groups; }
        }

        /// <summary>
        /// Effectue une requête vers TT-RSS.
        /// L'authentification est gérée en interne par cette méthode.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private static async Task<JsonObject> QueryApi(JsonObject json)
        {
            bool finished = false;
            while (!finished)
            {
                if(_token != null)
                {
                    // Ajout du session ID à la requête
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
                    // Si cette erreur est reçue, il faut simplement demander le jeton d'accès et réessayer
                    if (jsonRes.GetNamedObject("content").GetNamedString("error") == "NOT_LOGGED_IN")
                    {
                        await GetToken();
                        await QueryApi(json);
                        continue;
                    }
                    // Cette erreur indique que l'utilisateur a donné de faux renseignements
                    else if (jsonRes.GetNamedObject("content").GetNamedString("error") == "LOGIN_ERROR")
                        throw new InvalidConfigurationException("Login error");
                }
                else
                    finished = true;
                return jsonRes;
            }
            return null;
        }

        /// <summary>
        /// Bascule l'état d'un drapeau sur un article en particulier
        /// </summary>
        /// <param name="articleId">L'identifiant de l'article en question</param>
        /// <param name="flag">Le drapeau à changer</param>
        /// <returns></returns>
        public static async Task ToggleState(int articleId, RssArticle.RssArticleFlag flag)
        {
            JsonObject jsonRequest = new JsonObject();
            jsonRequest.Add("op", JsonValue.CreateStringValue("updateArticle"));
            jsonRequest.Add("article_ids", JsonValue.CreateNumberValue(articleId));
            jsonRequest.Add("mode", JsonValue.CreateNumberValue(2));
            jsonRequest.Add("field", JsonValue.CreateNumberValue((int) flag));

            await QueryApi(jsonRequest);
        }

        /// <summary>
        /// Effectue la connexion et stocke le jeton reçu dans Settings
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Va chercher la liste de tous les flux
        /// </summary>
        /// <returns>La liste des flux du serveur</returns>
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

                // Si le flux annonce qu'il possède une icône, on l'utilise
                if (feed.GetNamedBoolean("has_icon", false))
                    imageUri = Settings.GetInstance().InstanceUri + "/feed-icons/" + feed.GetNamedNumber("id").ToString() + ".ico";
                JsonValue unread = feed.GetNamedValue("unread");
                int unreadInt = -1;
                // Parfois, tt-rss retourne le nombre d'articles non-lus sous forme de string, parfois sous forme d'entier...
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

        /// <summary>
        /// Va chercher la liste des articles relatifs à un flux.
        /// Le contenu de l'article est inclus.
        /// </summary>
        /// <param name="uniqueId">L'identifiant du flux</param>
        /// <returns>L'objet flux contenant les articles</returns>
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
                // TT-RSS retourne parfois des nombres sous forme de string, allez savoir pourquoi...
                if (feed_id.ValueType.Equals(JsonValueType.Number))
                    feed_id_int = (int)feed_id.GetNumber();
                if (feed_id.ValueType.Equals(JsonValueType.String))
                    feed_id_int = int.Parse(feed_id.GetString());

                if (feed_id_int != -1 && Groups.Where((group) => group.UniqueId.Equals(feed_id_int)).First().ImagePath != imagePath)
                    imagePath = Settings.GetInstance().InstanceUri + "/feed-icons/" + feed_id_int + ".ico";

                // Ajout du style spécial Windows
                string content = kHead + "<body>" + article.GetNamedString("content") + "</body>";
                feed.Items.Add(new RssArticle((int)article.GetNamedNumber("id"), article.GetNamedString("title"), article.GetNamedString("feed_title"), imagePath, content, !article.GetNamedBoolean("unread"), new Uri(article.GetNamedString("link"))));
            }
            return feed;
        }

        /// <summary>
        /// Va chercher un article en particulier auprès du serveur
        /// </summary>
        /// <param name="uniqueId">L'identifiant de l'article</param>
        /// <returns>L'objet article correspondant</returns>
        public static async Task<RssArticle> GetItemAsync(int uniqueId)
        {
            JsonObject jsonRequest = new JsonObject();
            jsonRequest.Add("op", JsonValue.CreateStringValue("getArticle"));
            jsonRequest.Add("article_id", JsonValue.CreateNumberValue(uniqueId));

            JsonObject jsonResponse = await QueryApi(jsonRequest);
            JsonObject article = jsonResponse.GetNamedArray("content").First().GetObject();
            string content = kHead + "<body>" + article.GetNamedString("content") + "</body>";
            return new RssArticle((int)article.GetNamedNumber("id"), article.GetNamedString("title"), article.GetNamedString("author"), "Assets/DarkGray.png", content, !article.GetNamedBoolean("unread"), new Uri(article.GetNamedString("link")));
        }
       
    }
}