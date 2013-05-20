using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using Facebook;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using WP8HF.DataModel;
using WP8HF.Resources;
using Windows.Phone.Speech.Synthesis;
using Windows.Storage;

namespace WP8HF
{
    public partial class Page1 : PhoneApplicationPage
    {
        public string ThisImage { get; set; }
        public string Question { get; set; }
        public string Answer1 { get; set; }
        public string Answer2 { get; set; }
        public string Answer3 { get; set; }
        public string Answer4 { get; set; }
        public string RightAnswer { get; set; }

        public static bool ToPost = false;

        public Page1()
        {
            InitializeComponent();
        }

        // Fragment navigációban az id-t adja át
        protected override void OnFragmentNavigation(System.Windows.Navigation.FragmentNavigationEventArgs e)
        {
            QuestionItem question;
            var stringid = e.Fragment.Split('|')[0];
            var image = e.Fragment.Split('|')[1];
            int id = int.Parse(stringid);
            using (var ctx = new QuestionList())
            {
                question = ctx.QuestionItems.FirstOrDefault(q => q.QuestionItemId == id);
                try
                {
                    ThisImage = image;
                    if(image==null)
                        ThisImage = @"http://nfconlab.azurewebsites.net/Images/noimg.png";
                }
                catch
                {
                    ThisImage = @"http://nfconlab.azurewebsites.net/Images/noimg.png";
                }
                Question = question.Question;
                Answer1 = question.Answer1;
                Answer2 = question.Answer2;
                Answer3 = question.Answer3;
                Answer4 = question.Answer4;
                RightAnswer = question.RightAnswer;

                SayQuestion();
            }
        }

        private async void SayQuestion()
        {
            var voice = InstalledVoices.Default;

            using (var text2Speech = new SpeechSynthesizer())
            {
                text2Speech.SetVoice(voice);
                await text2Speech.SpeakTextAsync(Question);
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

        private void Answer1Click(object sender, RoutedEventArgs e)
        {
            if (Answer1.Equals(RightAnswer))
            {
                GoodAnswer(); 
            }
            else
            {
                MessageBox.Show(AppResources.FalseAnswer);
            }
        }
        private void Answer2Click(object sender, RoutedEventArgs e)
        {
            if (Answer2.Equals(RightAnswer))
            {
                GoodAnswer(); 
            }
            else
            {
                MessageBox.Show(AppResources.FalseAnswer);
            }
        }
        private void Answer3Click(object sender, RoutedEventArgs e)
        {
            if (Answer3.Equals(RightAnswer))
            {
                GoodAnswer(); 
            }
            else
            {
                MessageBox.Show(AppResources.FalseAnswer);
            }
        }
        private void Answer4Click(object sender, RoutedEventArgs e)
        {
            if (Answer4.Equals(RightAnswer))
            {
                GoodAnswer();                
            }
            else
            {
                MessageBox.Show(AppResources.FalseAnswer);
            }
        }
   
        private void GoodAnswer()
        {
            ToPost = true;
            MessageBoxResult result = MessageBox.Show(AppResources.Share, AppResources.CorrectAnswer, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                var cameraCaptureTask = new CameraCaptureTask();

                //esemenyre feliratkozas - minden choosernek van,
                //ha vegzett a felhasznalo a kivalasztassal
                cameraCaptureTask.Completed += CameraCaptureTaskCompleted;

                cameraCaptureTask.Show();
            }
            if (result == MessageBoxResult.Cancel)
            {
                NavigationService.GoBack();
            }
            
        }

        private async void CameraCaptureTaskCompleted(object sender, PhotoResult e)
        {
            ToPost = false;
            if (e == null)
            {
                NavigationService.GoBack();
            }
            //Ha nincs kép, csak feed-re posztol
            if (e.ChosenPhoto == null)
            {
                try
                {
                    var client =
                        new FacebookClient(IsolatedStorageSettings.ApplicationSettings["FacebookToken"].ToString());

                    var parameters = new Dictionary<string, object>
                        {
                            {"message", AppResources.ShareText + Question},
                        };

                    await client.PostTaskAsync("me/feed", parameters);
                    MessageBox.Show(AppResources.PostSuccess);
                    NavigationService.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(AppResources.PostError);
                    NavigationService.GoBack();
                }
            }

            //Ha volt kép, azt is postolja
            else
            {
                var image = new BitmapImage();
                image.SetSource(e.ChosenPhoto);

                var wb = new WriteableBitmap(image);

                //Egyedi név, fura karakterek nélkül
                string fileName = Question + DateTime.Now;
                fileName = fileName + ".jpg";
                fileName = fileName.Replace(" ", "");
                fileName = fileName.Replace("ö", "o");
                fileName = fileName.Replace("Ö", "O");
                fileName = fileName.Replace("ü", "u");
                fileName = fileName.Replace("Ü", "U");
                fileName = fileName.Replace("ó", "o");
                fileName = fileName.Replace("Ó", "O");
                fileName = fileName.Replace("ő", "o");
                fileName = fileName.Replace("Ő", "O");
                fileName = fileName.Replace("ú", "u");
                fileName = fileName.Replace("Ú", "U");
                fileName = fileName.Replace("ű", "u");
                fileName = fileName.Replace("Ű", "U");
                fileName = fileName.Replace("é", "e");
                fileName = fileName.Replace("É", "E");
                fileName = fileName.Replace("á", "a");
                fileName = fileName.Replace(":", "_");
                fileName = fileName.Replace("/", "_");
                fileName = fileName.Replace("?", "_");

                
                // Local Storage-ba mentés
                try
                {
                    //ez a mappa ugyanaz mint a GetUserStoreForApplication
                    StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                    StorageFile testFile = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                    using (var streamWriter = new StreamWriter(await testFile.OpenStreamForWriteAsync()))
                    //innentol ugyanaz mint elobb
                    {
                        wb.SaveJpeg(streamWriter.BaseStream, wb.PixelWidth, wb.PixelHeight, 0, 100);
                    }
                }
                catch
                {
                    MessageBox.Show(AppResources.LocalSaveError);
                }
                
                //Post facebookra
                var fbUpl = new Facebook.FacebookMediaObject
                    {
                        FileName = fileName,
                        ContentType = "image/jpg"
                    };
                using (MemoryStream ms = new MemoryStream())
                {
                    wb.SaveJpeg(ms, wb.PixelWidth, wb.PixelHeight, 0, 95);
                    ms.Seek(0, 0);
                    byte[] data = new byte[ms.Length];
                    ms.Read(data, 0, data.Length);
                    ms.Close();

                    fbUpl.SetValue(data);
                }


                try
                {
                    var client =
                        new FacebookClient(IsolatedStorageSettings.ApplicationSettings["FacebookToken"].ToString());

                    var parameters = new Dictionary<string, object>
                        {
                            {"message", AppResources.ShareText + Question},
                            {"file", fbUpl},
                        };

                    await client.PostTaskAsync("me/photos", parameters);

                    MessageBox.Show(AppResources.PostSuccess);
                    NavigationService.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(AppResources.PostError);
                    NavigationService.GoBack();
                }
            }
        }
        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            NavigationService.GoBack();
            base.OnBackKeyPress(e);
        }
    }
}