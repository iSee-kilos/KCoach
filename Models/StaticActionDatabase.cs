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
            var squatStart = new Dictionary<JointType, int>()
            {
                {JointType.KneeLeft, 180 },
                {JointType.KneeRight, 180 },
            };

            var squatEnd = new Dictionary<JointType, int>()
            {
                {JointType.KneeLeft, 90 },
                {JointType.KneeRight, 90 },
                {JointType.SpineBase, 30 }
            };

            var lungeStart = new Dictionary<JointType, int>()
            {
                {JointType.KneeLeft, 180 },
                {JointType.KneeRight, 180 }
            };

            var lungeEnd = new Dictionary<JointType, int>()
            {
                {JointType.KneeLeft, 80 },
                {JointType.KneeRight, 110 },
                {JointType.SpineBase, 100 },
                {JointType.SpineMid, 140 }
            };

            items.Add(new Action("Squat",
                "Squat",
                new Uri("Images/icon_squat.png", UriKind.Relative),
                "No Desc",
                squatStart,
                squatEnd
                ));

            items.Add(new Action("Lunge",
                "Lunge",
                new Uri("Images/icon_lunge.png", UriKind.Relative),
                "No Desc",
                lungeStart,
                lungeEnd
                ));

            items.Add(new Action("Deadlift",
                "Deadlift",
                new Uri("Images/icon_deadlift.png", UriKind.Relative),
                "No Desc",
                lungeStart,
                lungeEnd
                ));

            items.Add(new Action("Crunch",
                "Crunch",
                new Uri("Images/icon_crunch.png", UriKind.Relative),
                "No Desc",
                lungeStart,
                lungeEnd
                ));

            items.Add(new Action("PushUp",
                "Push Up",
                new Uri("Images/icon_pushup.png", UriKind.Relative),
                "No Desc",
                lungeStart,
                lungeEnd
                ));

        }

        public ObservableCollection<Action> Actions
        {
            get { return this.items; }
        }
    }
}
