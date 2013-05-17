using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WP8HF.DataModel
{
    [Table(Name="Questions")]
    public class QuestionItem : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private int _questionItemId;
        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int QuestionItemId
        {
            get { return _questionItemId; }
            set
            {
                if (value == _questionItemId) return;
                OnPropertyChanging();
                _questionItemId = value;
                OnPropertyChanged();
            }
        }

        private string _question;
        [Column]
        public string Question
        {
            get { return _question; }
            set
            {
                if (value == _question) return;
                OnPropertyChanging();
                _question = value;
                OnPropertyChanged();
            }
        }

        private string _position;
        [Column]
        public string Position
        {
            get { return _position; }
            set
            {
                if (value == _position) return;
                OnPropertyChanging();
                _position = value;
                OnPropertyChanged();
            }
        }

        private string _date;
        [Column]
        public string Date
        {
            get { return _date; }
            set
            {
                if (value == _date) return;
                OnPropertyChanging();
                _date = value;
                OnPropertyChanged();
            }
        }

        private string _answer1;
        [Column]
        public string Answer1
        {
            get { return _answer1; }
            set
            {
                if (value == _answer1) return;
                OnPropertyChanging();
                _answer1 = value;
                OnPropertyChanged();
            }
        }

        private string _answer2;
        [Column]
        public string Answer2
        {
            get { return _answer2; }
            set
            {
                if (value == _answer2) return;
                OnPropertyChanging();
                _answer2 = value;
                OnPropertyChanged();
            }
        }

        private string _answer3;
        [Column]
        public string Answer3
        {
            get { return _answer3; }
            set
            {
                if (value == _answer3) return;
                OnPropertyChanging();
                _answer3 = value;
                OnPropertyChanged();
            }
        }

        private string _answer4;
        [Column]
        public string Answer4
        {
            get { return _answer4; }
            set
            {
                if (value == _answer4) return;
                OnPropertyChanging();
                _answer4 = value;
                OnPropertyChanged();
            }
        }

        private string _rightAnswer;
        [Column]
        public string RightAnswer
        {
            get { return _rightAnswer; }
            set
            {
                if (value == _rightAnswer) return;
                OnPropertyChanging();
                _rightAnswer = value;
                OnPropertyChanged();
            }
        }

        private string _image;

        public string Image
        {
            get { return _image; }
            set { if (value == _image) return;
                OnPropertyChanging();
                _image = value;
                OnPropertyChanged(); }
        }
        

        private void OnPropertyChanging([CallerMemberName]string propName = null)
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(propName));
        }

        private void OnPropertyChanged([CallerMemberName]string propName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;
    }
}
