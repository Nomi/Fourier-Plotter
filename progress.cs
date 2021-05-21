//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Fourier_Plotter
//{
//	public partial class ProgressBarTaskOnUiThread : MainWindow
//	{
//		public ProgressBarTaskOnUiThread()
//		{
//			InitializeComponent();
//		}

//		private void Window_ContentRendered(object sender, EventArgs e)
//		{
//			for (int i = 0; i < 100; i++)
//			{
//				pbStatus.Value++;
//				Thread.Sleep(100);
//			}
//		}
//	}
//}
