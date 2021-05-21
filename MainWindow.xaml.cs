using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Drawing;
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
        //private long msPassedBefore = 0;
        private long fixedDeltaTime = -1;
        Stopwatch stopwatch = new Stopwatch();

        private List<pair> Pairs = new List<pair>();
        private List<circleData> circlesAnimState = new List<circleData>();


        public MainWindow()
        {
            InitializeComponent();
            //            List<pair> Pairs = new List<pair>();
            Pairs.Add(new pair(100, 1));
            Pairs.Add(new pair(5, 1.54));
            Pairs.Add(new pair(100, 0.1));
            Pairs.Add(new pair(10, 1));
            this.DataContext = Pairs;
        }

        private void accumulateCirclesAndRadiiToDraw(List<circleData> Circles)
        {
            System.Windows.Point locationOfPreviousCenter = new System.Windows.Point(_plotImage.ActualWidth / 2, _plotImage.ActualHeight / 2);
            int previousRadius = -1;
            bool firstCircle = true;
            //if (drawCircles.IsChecked)
            //{
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
            //}
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
            //aGeometryDrawing.Brush = new SolidColorBrush(Colors.Black);
            aGeometryDrawing.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2);
            ///my experiment with adding red dot STARTS
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
            ///my experiment with adding red dot ENDS
            ///earlier it was:
            //DrawingImage geometryImage = new DrawingImage(aGeometryDrawing);
            //geometryImage.Freeze();
            //_plotImage.Source = geometryImage;
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
            //aGeometryDrawing.Brush = new SolidColorBrush(Colors.Black);
            aGeometryDrawing.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2);
            ///my experiment with adding red dot STARTS
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
            ////if(redDotPathPoints.Count!=0)
            ////{
            ////    //redDotPathLineSegments.Add(new LineSegment())
            ////    PATH.Children.Add(new PathFigure(redDotPath.First(), redDotPathLineSegments));
            ////    redDotPathPoints.Add(new Point(Circles.Last().radiusPosition.X, Circles.Last().radiusPosition.Y));
            ////}
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
            //geometryPath.Brush = new SolidColorBrush(Colors.Blue);
            geometryPath.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 2);



            //Composite drawing and conclusion:
            DrawingGroup drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(aGeometryDrawing);
            drawingGroup.Children.Add(geometry);
            //\\
            drawingGroup.Children.Add(geometryPath);
            //\\
            DrawingImage drawingImage = new DrawingImage(drawingGroup);
            _plotImage.Source = drawingImage;
        }

        private void animCircleAndRadii(long timeSinceLastAnim)
        {
            if(circlesAnimState.Count==0)   //should only happen on the first try or reset
            {
                accumulateCirclesAndRadiiToDraw(circlesAnimState);
            }
            //circlesAnimState.Last().radiusPosition.X += timeSinceLastAnim * circlesAnimState.Last().rotationSpeedPPMs / 2;
            //circlesAnimState.Last().radiusPosition.Y += timeSinceLastAnim * circlesAnimState.Last().rotationSpeedPPMs / 2;
            ////double AngleShift = circlesAnimState.Last().anglePerMs * timeSinceLastAnim;
            //////since the movement is anti-clockwise when angle is positive and vice-versa (which is the opposite of how it should be according to the task and where I found the formula), I fix it with the following:
            ////AngleShift *= -1;
            ////double s = circlesAnimState.Last().radiusPosition.X - circlesAnimState.Last().centerOfCircle.X;
            ////double t = circlesAnimState.Last().radiusPosition.Y - circlesAnimState.Last().centerOfCircle.Y;
            ////circlesAnimState.Last().radiusPosition.X = circlesAnimState.Last().centerOfCircle.X + (s * Math.Cos(AngleShift) + t* Math.Sin(AngleShift));
            ////circlesAnimState.Last().radiusPosition.Y = circlesAnimState.Last().centerOfCircle.Y + ((-s) * Math.Sin(AngleShift) + t * Math.Cos(AngleShift));
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
            ////Subtracting angle already moved frome frequency:
            //if(circlesAnimState.Last().RadiusFrequencyPair.B>0)
            //{
            //    circlesAnimState.Last().RadiusFrequencyPair.B -= AngleShift / Math.PI;
            //}
            //else if (circlesAnimState.Last().RadiusFrequencyPair.B<0)
            //{
            //    circlesAnimState.Last().RadiusFrequencyPair.B -= AngleShift / Math.PI;
            //}

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
            //pbStatus.Maximum = 100;
            if (isPbPaused||isResetOrFirstTry)
            {
                //System.Threading.Timer timer = new System.Threading.Timer()
                ////Stopwatch stopWatch=new Stopwatch();
                ////stopWatch.
                //pbStatus.Value = pbLastValue;
                dispatcherTimer = new System.Windows.Threading.DispatcherTimer(DispatcherPriority.Render); //(DispatcherPriority.Send);
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10); //100); //50); //10); ///10 milisecs because 0.1*1000=100 and 1 seconds = 10000 miliseconds and so on
                stopwatch = new Stopwatch();
                if (isResetOrFirstTry)
                {
                    //pbStatus.Maximum = 100;
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
                //animCircleAndRadii(stopwatch.ElapsedMilliseconds);
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            fixedDeltaTime = stopwatch.ElapsedMilliseconds - fixedDeltaTime - 10;
            if(!isPbPaused&&!isResetOrFirstTry)
            {
                //pbLastValue += 1; //0.160; //1 //0.5; //0.166 //0.1;
                //pbStatus.Value = pbLastValue;
                pbStatus.Value = pbLastValue + (int)stopwatch.ElapsedMilliseconds/100;    // we divide by 100 because 10 seconds = 10000ms and we want the counter to go from 0 to 100 and 10000/100=100
                pbStatus.Value = pbLastValue + stopwatch.ElapsedMilliseconds;
                //pbStatus.Value = pbstopwatch.ElapsedMilliseconds;
                if (WasPausedAtSomePointNotHandledYet)
                {
                    timeOfLastTick = pbStatus.Value;
                    WasPausedAtSomePointNotHandledYet = false;
                }
                    animCircleAndRadii((long)pbStatus.Value - (long)timeOfLastTick);
                //if (stopwatch.ElapsedMilliseconds>timeOfLastTick)//pbStatus.Value > timeOfLastTick)
                //{
                ///animCircleAndRadii((long)pbStatus.Value - (long)timeOfLastTick);
                //animCircleAndRadii(stopwatch.ElapsedMilliseconds - (long)timeOfLastTick);
                //timeOfLastTick = stopwatch.ElapsedMilliseconds;
                //}
                timeOfLastTick = pbStatus.Value;
                ///pbStatus.Value = pbLastValue + 0.1 + fixedDeltaTime / 100;
                //long elapsed = stopwatch.ElapsedMilliseconds;
                //Duration duration = new Duration(TimeSpan.FromMilliseconds(elapsed-msPassedBefore));
                //msPassedBefore = elapsed;
                //DoubleAnimation dAnim = new DoubleAnimation(pbLastValue + elapsed / 100, duration);
                //pbStatus.BeginAnimation(ProgressBar.ValueProperty,dAnim);
                if (pbStatus.Value>=10000)
                {
                    stopwatch.Stop();
                    stopwatch.Reset();
                    isResetOrFirstTry = true;
                    dispatcherTimer.Stop();
                    ////drawing circle for lab part stage 4
                    //GeometryGroup ellipses = new GeometryGroup();
                    //System.Windows.Point locationOfPreviousCenter = new System.Windows.Point(_plotImage.ActualWidth / 2, _plotImage.ActualHeight / 2);
                    //int previousRadius=-1;
                    //bool firstCircle = true;
                    ////if (int.Pairs.First().A!=null)
                    ////{
                    //if(drawCircles.IsChecked)
                    //{
                    //    foreach (var pair in Pairs)
                    //    {
                    //        if (firstCircle)
                    //        {
                    //            ellipses.Children.Add(new EllipseGeometry(locationOfPreviousCenter, pair.A, pair.A));
                    //            firstCircle = false;
                    //        }
                    //        else
                    //            ellipses.Children.Add(new EllipseGeometry(locationOfPreviousCenter = new System.Windows.Point(locationOfPreviousCenter.X + previousRadius, locationOfPreviousCenter.Y), pair.A, pair.A));
                    //        previousRadius = pair.A;
                    //    }
                    //}
                    ////}
                    //GeometryDrawing aGeometryDrawing = new GeometryDrawing();
                    //aGeometryDrawing.Geometry = ellipses;
                    ////aGeometryDrawing.Brush = new SolidColorBrush(Colors.Black);
                    //aGeometryDrawing.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2);
                    //DrawingImage geometryImage = new DrawingImage(aGeometryDrawing);
                    //geometryImage.Freeze();
                    //_plotImage.Source =geometryImage;

                    ////end of drawing circle
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
                //msPassedBeforePause = (long)pbStatus.Value * 100;
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
            //msPassedBeforePause = 0;
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
            //if(isComplete)
            //{
                ////_plotImage.Source = null;
                //////Graphics graphics;
                //////graphics.DrawEllipse(new System.Drawing.Pen(System.Drawing.Brushes.Black, 2),(int)_plotImage.ActualHeight/2,(int)_plotImage.ActualWidth/2, Pairs.First().A, Pairs.First().A);
                if(Pairs.Count!=0)
                {
                    ////GeometryGroup ellipses = new GeometryGroup();
                    ////ellipses.Children.Add(
                    ////    new EllipseGeometry(new System.Windows.Point(_plotImage.ActualWidth / 2, _plotImage.ActualHeight / 2), Pairs.First().A, Pairs.First().A)
                    ////    );
                    ////GeometryDrawing aGeometryDrawing = new GeometryDrawing();
                    ////aGeometryDrawing.Geometry = ellipses;
                    //////System.Windows.Shapes.Ellipse
                    //////aGeometryDrawing.Brush = new SolidColorBrush(Colors.Black);
                    ////aGeometryDrawing.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2);
                    ////DrawingImage geometryImage = new DrawingImage(aGeometryDrawing);
                    ////geometryImage.Freeze();
                    ////_plotImage.Source = geometryImage;
                }
                if(!isResetOrFirstTry)
                {
                    //reset_Click(null, null);
                    drawCirclesAndRadii();
                }
            //}
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
                    //List<List<circle>> CircleList = new List<List<circle
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
                //XmlReaderSettings settings = new XmlReaderSettings();
                //settings.Schemas.Add("http://www.contoso.com/books", "pairListSchema");
                //settings.ValidationType = ValidationType.Schema;

                //XmlReader reader = XmlReader.Create("contosoBooks.xml", settings);
                //XmlDocument document = new XmlDocument();
                //document.Load(reader);

                //ValidationEventHandler eventHandler = new ValidationEventHandler();

                //// the following call to Validate succeeds.
                //document.Validate(eventHandler);

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
