using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCoach
{
    public sealed class StaticActionDatabase
    {
        public static StaticActionDatabase Instance = new StaticActionDatabase();

        private ObservableCollection<Action> items = new ObservableCollection<Action>();

        public StaticActionDatabase()
        {
            var squatTemplate = new Dictionary<JointType, int>()
            {
                {JointType.KneeLeft, 180 },
                {JointType.KneeRight, 180 },
                {JointType.SpineBase, 180 },
                {JointType.SpineMid, 180 }
            };

            items.Add(new Action("Squat",
                "Squat",
                new Uri("Images/icon_squat.png", UriKind.Relative),
                "No Desc",
                squatTemplate,
                squatTemplate
                ));

            items.Add(new Action("Lunge",
                "Lunge",
                new Uri("Images/icon_lunge.png", UriKind.Relative),
                "No Desc",
                squatTemplate,
                squatTemplate
                ));

        }

        public ObservableCollection<Action> Actions
        {
            get { return this.items; }
        }
    }
}
