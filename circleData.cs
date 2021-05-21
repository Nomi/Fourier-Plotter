using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Fourier_Plotter
{
    public class circleData
    {
        public pair RadiusFrequencyPair=new pair();
        public Point centerOfCircle=new Point();
        public Point radiusPosition=new Point();
        //public double rotationSpeedPPMs;
        public double anglePerMs;
        public circleData() { }
        public circleData(pair Radius_Frequency_Pair, Point center_point, Point radius_pos)
        {
            RadiusFrequencyPair = Radius_Frequency_Pair;
            centerOfCircle = center_point;
            radiusPosition = radius_pos;
            anglePerMs = 0;
        }
        public circleData(pair Radius_Frequency_Pair, Point center_point, Point radius_pos,double rotationSpeed)
        {
            RadiusFrequencyPair = Radius_Frequency_Pair;
            centerOfCircle = center_point;
            radiusPosition = radius_pos;
            anglePerMs = rotationSpeed;
        }
    }

    //public class CircleAnimationState
    //{ 

    //}
}
