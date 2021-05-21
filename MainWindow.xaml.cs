using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace Fourier_Plotter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double pbLastValue = 0;
        private bool isPbPaused = false;
        private bool isResetOrFirstTry = true;
        private bool isComplete = false;
        private bool WasPausedAtSomePointNotHandledYet = false;
        private double timeOfLastTick = 0;
        
        private List<Point> redDotPathPoints=new List<Point>();
        private List<LineSegment> redDotPathLineSegments=new List<LineSegment>();
        private DispatcherTimer dispatcherTimer;
        private long fixedDeltaTime = -1;
        Stopwatch stopwatch = new Stopwatch();

        private List<pair> Pairs = new List<pair>();
        private List<circleData> circlesAnimState = new List<circleData>();


        public MainWindow()
        {
            this.DataContext = Pairs;
        }

        private void accumulateCirclesAndRadiiToDraw(List<circleData> Circles)
        {
            System.Windows.Point locationOfPreviousCenter = new System.Windows.Point(_plotImage.ActualWidth / 2, _plotImage.ActualHeight / 2);
            int previousRadius = -1;
            bool firstCircle = true;
            foreach (var pair in Pairs)
            {
                double anglePerMs = (2 * Math.PI * pair.B) / 10000; //Frequency==(2pi)revolutions in 1 sec (full revolutions per second). //=>Frequency/10000==fullrevolutions in 1ms//=> 2pi*fullrevPerMs= Total Angle Shift in 1ms;
                if (pair.B != 0)//if frequency=0, rotationSpeed stays 0;
                {
                    double timePeriodForEachRevolution = 10000 / pair.B;     //even if the frequency is negative making the time periodnegative, it doesn't matter as we just want the speed to be a somewhat accurate representation.
                    //we know total time should be 10 seconds (10000ms). (dimensional analyisis tells us we need speed in terms of pixels/ms) SO:
                }
                if (firstCircle)
                {
                    Circles.Add(new circleData(pair, locationOfPreviousCenter, new Point(locationOfPreviousCenter.X + pair.A, locationOfPreviousCenter.Y),anglePerMs));
                    firstCircle = false;
                }
                else
                {
                    locationOfPreviousCenter = new Point(locationOfPreviousCenter.X + previousRadius, locationOfPreviousCenter.Y);
                    Circles.Add(new circleData(pair, locationOfPreviousCenter, new Point(locationOfPreviousCenter.X + pair.A, locationOfPreviousCenter.Y),anglePerMs));
                }
                previousRadius = pair.A;
            }
        }

        private void drawCirclesAndRadii()
        {
            List<circleData> Circles = new List<circleData>();
            accumulateCirclesAndRadiiToDraw(Circles);
            GeometryGroup shapesToDraw = new GeometryGroup();
            System.Windows.Point locationOfPreviousCenter = new System.Windows.Point(_plotImage.ActualWidth / 2, _plotImage.ActualHeight / 2);
            foreach (var circle in Circles)
            {
                if (drawCircles.IsChecked)
                {
                    shapesToDraw.Children.Add(new EllipseGeometry(circle.centerOfCircle, circle.RadiusFrequencyPair.A, circle.RadiusFrequencyPair.A));
                }
                if (drawLines.IsChecked)
                {
                    shapesToDraw.Children.Add(new LineGeometry(circle.centerOfCircle, circle.radiusPosition));
                }
            }
            GeometryDrawing aGeometryDrawing = new GeometryDrawing();
            aGeometryDrawing.Geometry = shapesToDraw;
            aGeometryDrawing.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2);
            ///Drawing RED DOT:
            EllipseGeometry redDot = new EllipseGeometry(Circles.Last().radiusPosition, 3, 3);
            GeometryGroup geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(redDot);
            GeometryDrawing geometry= new GeometryDrawing();
            geometry.Geometry = geometryGroup;
            geometry.Brush = new SolidColorBrush(Colors.Red);
            DrawingGroup drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(aGeometryDrawing);
            drawingGroup.Children.Add(geometry);
            DrawingImage drawingImage = new DrawingImage(drawingGroup);
            _plotImage.Source = drawingImage;
            ///red dot ends:
        }

        private void drawInAnim(List<circleData> Circles)
        {
            GeometryGroup shapesToDraw = new GeometryGroup();
            System.Windows.Point locationOfPreviousCenter = new System.Windows.Point(_plotImage.ActualWidth / 2, _plotImage.ActualHeight / 2);
            foreach (var circle in Circles)
            {
                if (drawCircles.IsChecked)
                {
                    shapesToDraw.Children.Add(new EllipseGeometry(circle.centerOfCircle, circle.RadiusFrequencyPair.A, circle.RadiusFrequencyPair.A));
                }
                if (drawLines.IsChecked)
                {
                    shapesToDraw.Children.Add(new LineGeometry(circle.centerOfCircle, circle.radiusPosition));
                }
            }
            GeometryDrawing aGeometryDrawing = new GeometryDrawing();
            aGeometryDrawing.Geometry = shapesToDraw;
            aGeometryDrawing.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2);
            ///RED DOT:
            EllipseGeometry redDot = new EllipseGeometry(Circles.Last().radiusPosition, 3, 3);
            GeometryGroup geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(redDot);
            GeometryDrawing geometry = new GeometryDrawing();
            geometry.Geometry = geometryGroup;
            geometry.Brush = new SolidColorBrush(Colors.Red);
            //
            //--drawing polyline---
            //
            GeometryGroup PATH = new GeometryGroup();
            redDotPathPoints.Add(new Point(Circles.Last().radiusPosition.X, Circles.Last().radiusPosition.Y));
            if (redDotPathPoints.Count() >1)
            {
                redDotPathLineSegments.Add(new LineSegment(redDotPathPoints.Last(), true));
                List<PathFigure> pfL = new List<PathFigure>();
                pfL.Add(new PathFigure(redDotPathPoints.First(), redDotPathLineSegments, false));
                PATH.Children.Add(new PathGeometry(pfL));
            }
            GeometryDrawing geometryPath = new GeometryDrawing();
            geometryPath.Geometry = PATH;
            geometryPath.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 2);


            //Composite drawing and conclusion:
            DrawingGroup drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(aGeometryDrawing);
            drawingGroup.Children.Add(geometry);
            drawingGroup.Children.Add(geometryPath);

            DrawingImage drawingImage = new DrawingImage(drawingGroup);
            _plotImage.Source = drawingImage;
        }

        private void animCircleAndRadii(long timeSinceLastAnim)
        {
            if(circlesAnimState.Count==0)   //should only happen on the first try or reset
            {
                accumulateCirclesAndRadiiToDraw(circlesAnimState);
            }
            bool firstCircle = true;
            Point previousRadiusPos;
            foreach(var cir in circlesAnimState)
            {
                if(firstCircle)
                {
                    double AngleShift = cir.anglePerMs * timeSinceLastAnim;
                    //since the movement is anti-clockwise when angle is positive and vice-versa (which is the opposite of how it should be according to the task and where I found the formula), I fix it with the following:
                    AngleShift *= -1;
                    double s = cir.radiusPosition.X - cir.centerOfCircle.X;
                    double t = cir.radiusPosition.Y - cir.centerOfCircle.Y;
                    cir.radiusPosition.X = cir.centerOfCircle.X + (s * Math.Cos(AngleShift) + t * Math.Sin(AngleShift));
                    cir.radiusPosition.Y = cir.centerOfCircle.Y + ((-s) * Math.Sin(AngleShift) + t * Math.Cos(AngleShift));
                    //misc:
                    firstCircle = false;
                    previousRadiusPos = new Point(cir.radiusPosition.X, cir.radiusPosition.Y);
                }
                else
                {
                    double rxDispFromCenter =cir.radiusPosition.X-cir.centerOfCircle.X; double ryDispFromCenter= cir.radiusPosition.Y - cir.centerOfCircle.Y;
                    cir.centerOfCircle = previousRadiusPos;
                    cir.radiusPosition.X = previousRadiusPos.X + rxDispFromCenter;
                    cir.radiusPosition.Y = previousRadiusPos.Y + ryDispFromCenter;
                    double AngleShift = cir.anglePerMs * timeSinceLastAnim;
                    //since the movement is anti-clockwise when angle is positive and vice-versa (which is the opposite of how it should be according to the task and where I found the formula), I fix it with the following:
                    AngleShift *= -1;
                    double s = cir.radiusPosition.X - cir.centerOfCircle.X;
                    double t = cir.radiusPosition.Y - cir.centerOfCircle.Y;
                    cir.radiusPosition.X = cir.centerOfCircle.X + (s * Math.Cos(AngleShift) + t * Math.Sin(AngleShift));
                    cir.radiusPosition.Y = cir.centerOfCircle.Y + ((-s) * Math.Sin(AngleShift) + t * Math.Cos(AngleShift));
                    //misc:
                    previousRadiusPos = new Point(cir.radiusPosition.X, cir.radiusPosition.Y);
                }
            }
            drawInAnim(circlesAnimState);
        }
        private void exit_Click(object sender, RoutedEventArgs e)
        {
            //System.Windows.Application.Current.Shutdown();  // from : https://stackoverflow.com/questions/2820357/how-do-i-exit-a-wpf-application-programmatically
            MessageBoxResult answer = MessageBox.Show("Would you like to leave?", "Exit", MessageBoxButton.YesNo);
            if(answer==MessageBoxResult.Yes)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            if(isResetOrFirstTry)
            {
                reset_Click(null, null);
                drawCirclesAndRadii();
            }
            if (isPbPaused||isResetOrFirstTry)
            {
                dispatcherTimer = new System.Windows.Threading.DispatcherTimer(DispatcherPriority.Render); //(DispatcherPriority.Send);
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10); //100); //50); //10); ///10 milisecs because 0.1*1000=100 and 1 seconds = 10000 miliseconds and so on
                stopwatch = new Stopwatch();
                if (isResetOrFirstTry)
                {
                    pbStatus.Maximum = 10000;
                    pbStatus.Minimum = 0;
                    pbStatus.Value = 0;
                }
                else
                {
                    pbStatus.Value = pbLastValue;
                }
                isPbPaused = false;
                isResetOrFirstTry = false;
                stopwatch.Start();
                timeOfLastTick = fixedDeltaTime = stopwatch.ElapsedMilliseconds;
                dispatcherTimer.Start();
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            fixedDeltaTime = stopwatch.ElapsedMilliseconds - fixedDeltaTime - 10;
            if(!isPbPaused&&!isResetOrFirstTry)
            {
                pbStatus.Value = pbLastValue + stopwatch.ElapsedMilliseconds;
                if (WasPausedAtSomePointNotHandledYet)
                {
                    timeOfLastTick = pbStatus.Value;
                    WasPausedAtSomePointNotHandledYet = false;
                }
                animCircleAndRadii((long)pbStatus.Value - (long)timeOfLastTick);
                timeOfLastTick = pbStatus.Value;
                if (pbStatus.Value>=10000)
                {
                    stopwatch.Stop();
                    stopwatch.Reset();
                    isResetOrFirstTry = true;
                    dispatcherTimer.Stop();
                    isComplete = true;
                    pbStatus.Value = 0;
                    circlesAnimState.Clear();
                    redDotPathLineSegments.Clear();
                    redDotPathPoints.Clear();
                }
            }
            else
            {
                dispatcherTimer.Stop();
            }
        }

        private void pause_Click(object sender, RoutedEventArgs e)
        {
            if(!isResetOrFirstTry)
            {
                isPbPaused = true;
                WasPausedAtSomePointNotHandledYet = true;
                dispatcherTimer.Stop();
                stopwatch.Stop();
                stopwatch.Reset();
                pbLastValue = pbStatus.Value;
            }
        }

        private void reset_Click(object sender, RoutedEventArgs e)
        {
            isComplete = false;
            isPbPaused = false;
            isResetOrFirstTry = true;
            WasPausedAtSomePointNotHandledYet = false;
            if(dispatcherTimer!=null)
                dispatcherTimer.Stop();
            stopwatch.Stop();
            stopwatch.Reset();
            pbLastValue = 0;
            pbStatus.Value = 0;
            //pbStatus.SetValue(ProgressBar.ValueProperty, pbLastValue);
            _plotImage.Source = null;
            circlesAnimState.Clear();
            redDotPathLineSegments.Clear();
            redDotPathPoints.Clear();
        }

        private void circlesDataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            if(!isResetOrFirstTry)
            {
                reset_Click(null, null);
                drawCirclesAndRadii();
            }
        }

        private void new_Click(object sender, RoutedEventArgs e)
        {
            reset_Click(null, null);
            Pairs.RemoveRange(0, Pairs.Count);
            circlesDataGrid.ItemsSource = null;
            circlesDataGrid.ItemsSource = Pairs;
        }

        private void circlesDataGrid_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            circlesDataGrid.ItemsSource = null;
            circlesDataGrid.ItemsSource = Pairs;
            reset_Click(null,null);
            drawCirclesAndRadii();
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = ".xml";
            saveFileDialog.Filter = "XML|*.xml";
            if(saveFileDialog.ShowDialog()==true)
            {
                XmlSerializer pairList_Serializer = new XmlSerializer(typeof(List<pair>));
                using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                {
                    pairList_Serializer.Serialize(writer, Pairs);
                }
            }
        }
        private void open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter= "XML Files (.xml)|*.xml";
            if (openFileDialog.ShowDialog() == true)
            {
                XmlSerializer pairList_Serializer = new XmlSerializer(typeof(List<pair>));
                using (StreamReader reader = new StreamReader(openFileDialog.FileName))
                {
                    try
                    {
                        Pairs = (List<pair>)pairList_Serializer.Deserialize(reader);
                        reset_Click(null, null);
                        circlesDataGrid.ItemsSource = null;
                        circlesDataGrid.ItemsSource = Pairs;
                        drawCirclesAndRadii();
                    }
                    catch (InvalidOperationException)
                    {
                        MessageBox.Show("Selected file is erroneous. Doing nothing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
            }
            }
        }
    }
}
