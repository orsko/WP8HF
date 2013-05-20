using System;
using System.ComponentModel;
using System.Device.Location;
using System.IO.IsolatedStorage;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using WP8HF.DataModel;
using System.Runtime.Serialization.Json;
using System.IO;
using WP8HF.Resources;
using Windows.Devices.Geolocation;
using Windows.Storage;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Windows.Storage.Streams;

namespace WP8HF
{
    public partial class MainPage : PhoneApplicationPage
    {

        private int imageId = 0;

        private static ObservableCollection<QuestionItem> allQuestions = new ObservableCollection<QuestionItem>();
        private static ObservableCollection<QuestionItem> newQuestions = new ObservableCollection<QuestionItem>();
        private static ObservableCollection<QuestionItem> currentQuestions = new ObservableCollection<QuestionItem>();

        //Minden kérdés
        public static ObservableCollection<QuestionItem> AllQuestions
        {
            get { return allQuestions; }
            set { allQuestions = value; }
        }

        //Új kérdések
        public static ObservableCollection<QuestionItem> NewQuestions
        {
            get { return newQuestions; }
            set { newQuestions = value; }
        }

        //Elérhető kérdések
        public static ObservableCollection<QuestionItem> CurrentQuestions
        {
            get { return currentQuestions; }
            set { currentQuestions = value; }
        }

        private bool start=true;
        private string completed;

        // Adatbázis
        private QuestionList questions = new QuestionList();

        public QuestionList MyQuestions
        {
            get { return questions; }
            set { questions = value; }
        }

        private static Geoposition geoposition;

        public static Geoposition MyGeoposition
        {
            get { return geoposition; } 
            set { geoposition = value; }
        }

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            BuildLocalizedApplicationBar();

            Loaded += MainPage_Loaded;

        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDatabase();
        }

        private void LoadDatabase()
        {           
            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;

            // DB feltöltése
            if (!questions.DatabaseExists())
                questions.CreateDatabase();
            else
            {
                if (start == true)
                {
                    questions.DeleteDatabase();
                    questions=new QuestionList();
                    questions.CreateDatabase();
                }
            }
            if (start)
            {
                SystemTray.IsVisible = true;

                SystemTray.ProgressIndicator = new ProgressIndicator();
                SystemTray.ProgressIndicator.IsIndeterminate = true;
                SystemTray.ProgressIndicator.IsVisible = true;

                LoadData();              

                start = false;
            }
        }

        public async void LoadData()
        {
            var httpClient = new HttpClient();
            try
            {
                completed = await httpClient.GetStringAsync("http://nfconlab.azurewebsites.net/Home/AllQuestions");                
                string[] jsons = completed.Split('\n');
                var serializer = new DataContractJsonSerializer(typeof(MyJSON));

                foreach (var element in jsons)
                {
                    if (element.Equals(""))
                        continue;
                    byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(element);
                    using (var stream = new MemoryStream(byteArray))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            reader.Read();
                            var myJson = (MyJSON)serializer.ReadObject(stream);
                            QuestionItem item = myJson.ToQuestionItem();
                            questions.QuestionItems.InsertOnSubmit(item);                           
                        }
                    }
                }

                questions.SubmitChanges();

                GetLocation();

                foreach (var item in questions.QuestionItems)
                {
                    var a = item.Position.Split(',');
                    double lat;
                    try
                    {
                        lat = double.Parse(a[0]);
                    }
                    catch
                    {
                        a[0] = a[0].Replace('.', ',');
                        a[1] = a[1].Replace('.', ',');
                    }
                    lat = double.Parse(a[0]);
                    double lon = double.Parse(a[1]);
                    //a pin kiteveset is itt hivjuk meg
                    var gc = new GeoCoordinate();
                    gc.Latitude = lat;
                    gc.Longitude = lon;
                    DrawMapMarker(gc, item.QuestionItemId);
                }

                LoadImages();

                allQuestions.Clear();
                newQuestions.Clear();
                currentQuestions.Clear();

                foreach (var x in questions.QuestionItems)
                {
                    allQuestions.Add(x);
                    var date = DateTime.ParseExact(x.Date, "yyyy-MM-dd HH:mm", null);
                    var delta = (DateTime.Now - date).TotalDays;
                    if (delta < 30)
                    {
                        newQuestions.Add(x);                    
                    }
                }

                SystemTray.ProgressIndicator.IsIndeterminate = false;
            }
            catch
            {
                MessageBox.Show(AppResources.ConnectionError);
                SystemTray.ProgressIndicator.IsIndeterminate = false;
            }
        }

        public async void GetLocation()
        {
            //Capability: ID_CAP_LOCATION
            var geolocator = new Geolocator();
            geolocator.DesiredAccuracyInMeters = 10;

            

            try
            {
                geoposition = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromSeconds(5),
                    timeout: TimeSpan.FromSeconds(25));

                DrawMap();               
            }
            catch (Exception)
            {
                MessageBox.Show(AppResources.GpsError);
            }
        }

        private void DrawMap()
        {
            //Térképen saját pozívió bejelölése
            MapGps.Center = new GeoCoordinate(geoposition.Coordinate.Latitude, geoposition.Coordinate.Longitude);

            //a pin kiteveset is itt hivjuk meg
            DrawMapMarker(MapGps.Center, 0);

            foreach (var x in questions.QuestionItems)
            {
                double lat;
                double lon;
                try
                {
                    lat = double.Parse(x.Position.Split(',')[0]);
                    lon = double.Parse(x.Position.Split(',')[1]);
                }
                catch
                {
                    lat = double.Parse(x.Position.Split(',')[0].Replace('.', ','));
                    lon = double.Parse(x.Position.Split(',')[1].Replace('.', ','));
                }


                var curLat = geoposition.Coordinate.Latitude;
                var curLon = geoposition.Coordinate.Longitude;

                //Távolság
                var delta = Math.Sqrt(Math.Pow(lat - curLat, 2) + Math.Pow(lon - curLon, 2));

                if (delta < 0.31)
                    currentQuestions.Add(x);

            }
        }

        //Jel rajzolása
        private void DrawMapMarker(GeoCoordinate coordinate, int currentQuestionId)
        {
            //MapGps.Layers.Clear(); //tobb reteg is lehet a terkepen, ezert ezeket eloszor toroljuk
            MapLayer mapLayer = new MapLayer();

            Polygon polygon = new Polygon();
            polygon.Points.Add(new Point(0, 0));
            polygon.Points.Add(new Point(0, 75));
            polygon.Points.Add(new Point(25, 0));
            if (currentQuestionId > 0)
            {
                polygon.Fill = new SolidColorBrush(Colors.Yellow);
                //ha rakattintunk a kis pin-re, akkor messageboxot jelenit meg
                polygon.MouseLeftButtonUp += polygon_MouseLeftButtonUp;
            }
            else
            {
                polygon.Fill = new SolidColorBrush(Colors.Blue);
                //ha rakattintunk a kis pin-re, akkor messageboxot jelenit meg
                polygon.MouseLeftButtonUp += polygon_MouseLeftButtonUpMe;
            }

            MapOverlay overlay = new MapOverlay();
            overlay.Content = polygon;
            overlay.GeoCoordinate = coordinate;
            //rateszi a terkepre
            overlay.PositionOrigin = new Point(0, 1);

            mapLayer.Add(overlay);
            MapGps.Layers.Add(mapLayer);
        }

        void polygon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(AppResources.QuestionPosition);
        }

        void polygon_MouseLeftButtonUpMe(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(AppResources.CurrentPosition);
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();                
            }
        }

        private void LongListSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var q = ((QuestionItem) e.AddedItems[0]);
                int id = q.QuestionItemId;               
                NavigationService.Navigate(new Uri("/AnswerQuestion.xaml#"+id+"|"+q.Image, UriKind.Relative));
            }
            catch (Exception)
            {                
                throw;
            }
            
        }

        public ObservableCollection<StorageFile> ImageFiles { get; set; }
        public StorageFile ImgFile { get; set; }

        private async void LoadImages()
        {
            //var image = new BitmapImage();

            //var wb = new WriteableBitmap(image);

            // Local Storage-ba mentés
            try
            {
                //képek beolvasása
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                var testFiles = await storageFolder.GetFilesAsync();
                ImgFile = testFiles.First();
                //ImageFiles = testFiles;
                ImageFiles=new ObservableCollection<StorageFile>();
                foreach (var storageFile in testFiles)
                {
                    if(storageFile.Path.Contains(".jpg"))
                        ImageFiles.Add(storageFile);
                    using (var streamReader = new StreamReader(await storageFile.OpenStreamForReadAsync()))                       
                    {
                       // wb.LoadJpeg(streamReader.BaseStream);
                    }
                }
            }
            catch
            {
                MessageBox.Show(AppResources.LocalLoadError);
            }
        }

        //Sample code for building a localized ApplicationBar
        private void BuildLocalizedApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();

            ApplicationBar.Mode=ApplicationBarMode.Minimized;

            // Create a new button and set the text value to the localized string from AppResources.
            ApplicationBarIconButton appBarRefreshButton = new ApplicationBarIconButton(new Uri("/Assets/Images/refresh.png", UriKind.Relative));
            //appBarButton.Text = AppResources.AppBarButtonText;
            appBarRefreshButton.Text = "refresh";
            appBarRefreshButton.Click += appBarRefreshButton_Click;
            ApplicationBar.Buttons.Add(appBarRefreshButton);

        }

        private void appBarRefreshButton_Click(object sender, EventArgs e)
        {
            //Hogy újra betöltse az adatbázist
            start = true;
            //Pin-ek eltűntetése
            MapGps.Layers.Clear();
            //Adatok betöltése
            LoadDatabase();
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings.Save();
            Application.Current.Terminate();
        }
    
    }

    public class MyJSON
    {
        public class AnswersJSON
        {
            public string Answer1 { get; set; }
            public string Answer2 { get; set; }
            public string Answer3 { get; set; }
            public string Answer4 { get; set; }
            public string RightAnswer { get; set; }
        }

        public string Date { get; set; }
        public string Question { get; set; }
        public AnswersJSON Answers { get; set; }
        public string Image { get; set; }
        public int QuestionItemId { get; set; }
        public string Position { get; set; }

        public QuestionItem ToQuestionItem()
        {
            var q = new QuestionItem
                {
                    Answer1 = Answers.Answer1,
                    Answer2 = Answers.Answer2,
                    Answer3 = Answers.Answer3,
                    Answer4 = Answers.Answer4,
                    RightAnswer = Answers.RightAnswer,
                    Date = this.Date,
                    Image = this.Image,
                    Position = Position,
                    Question = Question,
                    QuestionItemId = QuestionItemId
                };
            return q;
        }
    }
}