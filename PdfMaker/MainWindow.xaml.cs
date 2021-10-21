using PrintPdf_Console;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PdfMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage image;
        public MainWindow()
        {
            InitializeComponent();
            imagespath.ItemsSource = sourceImgs;
            script.Text = File.ReadAllText("Scripts.txt");
        }

        private void grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

            Grid g = (Grid)sender;
            var pos = e.GetPosition(g);

            Line line = CreateLine(pos.Y);

            g.Children.Add(line);

            UpdateList();
        }

        private void UpdateList()
        {
            List<Line> childPos = GetLines();

            breakPointsLb.ItemsSource = childPos.OrderBy(x => x.Margin.Top).ToList();
        }

        private List<Line> GetLines()
        {
            List<Line> childPos = new List<Line>();
            foreach (var child in grid.Children)
            {
                if (!(child is Line)) continue;

                Line l = (Line)child;

                l.Stroke = Brushes.Red;
                l.StrokeThickness = 1;

                childPos.Add(l);
            }

            return childPos;
        }

        private static Line CreateLine(double y)
        {
            y = Math.Round(y);
            var line = new Line();
            line.X1 = 0;
            line.X2 = 800;
            line.Y1 = y;
            line.Y2 = y;

            line.Stroke = Brushes.Red;
            line.StrokeThickness = 1;
            line.Stretch = Stretch.Uniform;
            line.VerticalAlignment = VerticalAlignment.Top;
            line.HorizontalAlignment = HorizontalAlignment.Left;

            line.Margin = new Thickness(0, y, 0, 0);
            return line;
        }

        private void Buttondelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var line = button.DataContext as Line;

                if (line == null) return;

                grid.Children.Remove(line);

                UpdateList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        int randomIndex = 0;
        private void ButtonHighlight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var line = button.DataContext as Line;

                if (line == null) return;
                var randomColor = new Brush[] { Brushes.Green, Brushes.Red };

                line.Stroke = randomColor[(randomIndex++) % 2];
                line.StrokeThickness = 3;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        ObservableCollection<string> sourceImgs = new ObservableCollection<string>();
        private void imagespath_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                sourceImgs.Add(files[0]);
            }
        }

        string oneFile = string.Empty;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists("segments"))
                Directory.Delete("segments", true);

            if (Directory.Exists("out"))
                Directory.Delete("out", true);


            Directory.CreateDirectory("segments");
            Directory.CreateDirectory("out");


            oneFile = Program.CombineImagesToSingle(sourceImgs.Select(x => new FileInfo(x)).ToArray());
           
            image = new BitmapImage(new Uri(oneFile));
            preview.Source = image;
            dpi = image.PixelWidth * 1.0f / image.Width;

            c1.Width = new GridLength(image.Width * 0.5);
        }
        double dpi = 0;

        private void GenPdf(object sender, RoutedEventArgs e)
        {
            var scale = this.preview.ActualHeight / image.Height;

            var breakingSources = GetLines().Select(x => dpi * x.Margin.Top / scale).Select(x => (int)Math.Round(x));

            File.WriteAllText("segment_point.txt", string.Join("\r\n", breakingSources));

            var splitToSegments = Program.SplitToSegments(oneFile);
            var smallerPages = Program.ConvertToSmallerPages(splitToSegments);
            Program.GeneratePdfFromImage(smallerPages);
            MessageBox.Show("Done");
        }
    }
}
