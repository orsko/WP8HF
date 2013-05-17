using System;
using System.ComponentModel;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WP8HF.DataModel;
using System.Runtime.Serialization.Json;
using System.IO;
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

        private ObservableCollection<QuestionItem> allQuestions = new ObservableCollection<QuestionItem>();
        private ObservableCollection<QuestionItem> newQuestions = new ObservableCollection<QuestionItem>();
        private ObservableCollection<QuestionItem> currentQuestions = new ObservableCollection<QuestionItem>();

        //Minden kérdés
        public ObservableCollection<QuestionItem> AllQuestions
        {
            get { return allQuestions; }
            set { allQuestions = value; }
        }

        //Új kérdések
        public ObservableCollection<QuestionItem> NewQuestions
        {
            get { return newQuestions; }
            set { newQuestions = value; }
        }

        //Elérhető kérdések
        public ObservableCollection<QuestionItem> CurrentQuestions
        {
            get { return currentQuestions; }
            set { currentQuestions = value; }
        }

        private bool start=true;
        private string completed;

        // Adatbázis
        private QuestionList questions = new QuestionList();

        private Geoposition geoposition;


        // Constructor
        public MainPage()
        {
            InitializeComponent();

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
                            questions.QuestionItems.InsertOnSubmit(myJson.ToQuestionItem());
                        }
                    }
                }

                questions.SubmitChanges();

                GetLocation();

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
                MessageBox.Show("Can't connect to server");
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
                    maximumAge: TimeSpan.FromMinutes(5),
                    timeout: TimeSpan.FromSeconds(25));

                foreach (var x in questions.QuestionItems)
                {
                    var lat = double.Parse(x.Position.Split(',')[0]);
                    var lon = double.Parse(x.Position.Split(',')[1]);

                    var curLat = geoposition.Coordinate.Latitude;
                    var curLon = geoposition.Coordinate.Longitude;

                    //Távolság
                    var delta = Math.Sqrt(Math.Pow(lat - curLat,2) + Math.Pow(lon - curLon,2));

                    if(delta < 0.31)
                        currentQuestions.Add(x);

                }
            }
            catch (Exception)
            {
                MessageBox.Show("Can't get GPS data");
            }
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

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            NavigationService.GoBack();
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