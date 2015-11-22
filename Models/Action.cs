using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KCoach
{
    public class Action : BindableBase
    {
        private static Uri baseUri = new Uri("pack://application:,,,/");

        private string uniqueId = string.Empty;

        private string title = string.Empty;

        private string description = string.Empty;

        private ImageSource image = null;

        private Uri imagePath = null;

        private IReadOnlyDictionary<JointType, int> template;

        public Action(string uniqueId, string title, Uri imagePath, string description, IReadOnlyDictionary<JointType, int> template)
        {
            this.uniqueId = uniqueId;
            this.title = title;
            this.imagePath = imagePath;
            this.description = description;
            this.template = template;
        }

        public string UniqueId
        {
            get { return this.uniqueId; }
            set { this.SetProperty(ref this.uniqueId, value); }
        }

        public string Title
        {
            get { return this.title; }
            set { this.SetProperty(ref this.title, value); }
        }

        public string Description
        {
            get { return this.description; }
            set { this.SetProperty(ref this.description, value); }
        }

        public IReadOnlyDictionary<JointType, int> Template
        {
            get { return this.template; }
        }

        public ImageSource Image
        {
            get
            {
                if (this.image == null && this.imagePath != null)
                {
                    this.image = new BitmapImage(new Uri(Action.baseUri, this.imagePath));
                }

                return this.image;
            }

            set
            {
                this.imagePath = null;
                this.SetProperty(ref this.image, value);
            }
        }

        public void SetImage(Uri path)
        {
            this.image = null;
            this.imagePath = path;
            this.OnPropertyChanged("Image");
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
