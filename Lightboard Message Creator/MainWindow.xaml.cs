using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using Winforms = System.Windows.Forms;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Runtime.InteropServices;


namespace Lightboard_Message_Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string TextAnchorMode;
        Button CurrentAnchorButton;
        List<List<Button>> CollectionLEDs;
        List<List<int>> MessageMap;
        int LEDBoardWidth = 72; // Target -> 72
        int LEDBoardHeight = 22; // Target -> 22

        DispatcherTimer PreviewTimer;
        int HorizontalScrollingSpeed = 50;
        int VerticalScrollingSpeed = 30;
        int VerticalPauseSpeed = 3000;
        int FlashSpeed = 3000;

        int EditingWidth = 12;
        int EditingHeight = 12;

        int ScrollPositionX = 0;
        int ScrollPositionY = 0;

        int RollCounter = 0;

        bool FinishedLoading = false;
        bool Scrolling = false;

        Dictionary<string, List<Point>> EnglishFont;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CollectionLEDs = new List<List<Button>>();
            for (int i = 0; i < LEDBoardWidth; i++)
            {
                CollectionLEDs.Add(new List<Button>());
            }

            int xPos = 0;
            int yPos = 0;

            foreach (Button myButton in RootGrid.Children.OfType<Button>())
            {
                if (myButton.Tag == null)
                    continue;

                if (myButton.Tag.ToString() == "LED")
                {
                    CollectionLEDs[xPos].Add(myButton);
                }

                yPos += 1;
                if (yPos >= LEDBoardHeight)
                {
                    xPos += 1;
                    yPos = 0;
                }
            }


            MessageMap = new List<List<int>>();
            SetMessageMapSize(LEDBoardWidth, LEDBoardHeight, true);

            MessageWidth.Text = LEDBoardWidth.ToString();
            MessageHeight.Text = LEDBoardHeight.ToString();

            ReloadNewDisplayMode();

            PreviewTimer = new DispatcherTimer();
            PreviewTimer.Tick += new EventHandler(PreviewTimer_Tick);
            PreviewTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);

            TextAnchorMode = "TopLeft";
            CurrentAnchorButton = AnchorTopLeftButton;
            CurrentAnchorButton.IsEnabled = false;

            EnglishFont = new Dictionary<string, List<Point>>();
            InitializeEnglishFont();

            FinishedLoading = true;
        }

        public List<Point> Convert2DArrayToPointList(int[,] charMap)
        {
            List<Point> reList = new List<Point>();
            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (charMap[y,x] == 1) // Fipped for easier use;
                    {
                        reList.Add(new Point(x, y));
                    }
                }
            }
            return reList;
        }

        //public static int[] BitmapToByteArray(Bitmap image)
        //{
        //    byte[] returns = null;
        //    if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
        //    {
        //        BitmapData bitmapData = image.LockBits(
        //                                        new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
        //                                        ImageLockMode.ReadWrite,
        //                                        image.PixelFormat);
        //        int noOfPixels = image.Width * image.Height;
        //        int colorDepth = Bitmap.GetPixelFormatSize(image.PixelFormat);
        //        int step = colorDepth / 8;
        //        byte[] bytes = new byte[noOfPixels * step];
        //        IntPtr address = bitmapData.Scan0;
        //        Marshal.Copy(address, bytes, 0, bytes.Length);
        //        ////////////////////////////////////////////////
        //        ///
        //        returns = (byte[])bytes.Clone();
        //        ///
        //        ////////////////////////////////////////////////
        //        Marshal.Copy(bytes, 0, address, bytes.Length);
        //        image.UnlockBits(bitmapData);
        //    }
        //    else
        //    {
        //        throw new Exception("8bpp indexed image required");
        //    }
        //    return returns.Select(x => (int)x).ToArray();
        //}
        //public int[,] ConvertArray(int[] Input, int size)
        //{
        //    int[,] Output = new int[(int)(Input.Length / size), size];
        //    for (int i = 0; i < Input.Length; i += size)
        //    {
        //        for (int j = 0; j < size; j++)
        //        {
        //            Output[(int)(i / size), j] = Input[i + j];
        //        }
        //    }
        //    return Output;
        //}

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
        }
        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {

        }
        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            Winforms.SaveFileDialog saveFileDialog = new Winforms.SaveFileDialog();

            saveFileDialog.Filter = "txt files (*.txt)|*.txt";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;


            if (saveFileDialog.ShowDialog() == Winforms.DialogResult.OK)
            {
                System.IO.Stream file = saveFileDialog.OpenFile();

                if (file != null)
                {
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(file);

                    int w = MessageMap.Count;
                    int h = MessageMap[0].Count;

                    string finalMessage = "";
                    finalMessage += DisplayModeSelect.SelectedIndex;
                    finalMessage += ", ";

                    for (int y = 0; y < h; y++)
                    {
                        double total = 0;
                        for (int x = 0; x < w; x++)
                        {
                            total += (MessageMap[x][y] * (Math.Pow(2, w - x - 2)));
                        }
                        finalMessage += total.ToString();
                        finalMessage += ", ";
                    }

                    finalMessage = finalMessage.Substring(0, finalMessage.Length - 2);
                    sw.WriteLine(finalMessage);

                    sw.Flush();
                    sw.Close();
                }
            }

            
        }

        private void AddTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (TextPositionX.Text == "" || TextPositionY.Text == "")
            {
                MessageBox.Show("Please enter a number.");
                return;
            }

            int xPos = int.Parse(TextPositionX.Text) - 1;
            int yPos = int.Parse(TextPositionY.Text) - 1;

            InputText inputText = ProcessInputText();

            if (inputText.PointMap.Count == 0)
                return;

            switch(TextAnchorMode)
            {
                case "TopLeft":
                    break;

                case "CenterTop":
                    xPos -= (int)(inputText.Width / 2);
                    break;

                case "TopRight":
                    xPos -= inputText.Width;
                    break;

                case "CenterRight":
                    xPos -= inputText.Width;
                    yPos -= (int)(inputText.Height / 2);
                    break;

                case "BottomRight":
                    xPos -= inputText.Width;
                    yPos -= inputText.Height;
                    break;

                case "CenterBottom":
                    xPos -= (int)(inputText.Width / 2);
                    yPos -= inputText.Height;
                    break;

                case "BottomLeft":
                    yPos -= inputText.Height;
                    break;

                case "CenterLeft":
                    yPos -= (int)(inputText.Height / 2);
                    break;

                case "Center":
                    xPos -= (int)(inputText.Width / 2);
                    yPos -= (int)(inputText.Height / 2);
                    break;

            }

            foreach (Point pixel in inputText.PointMap)
            {
                Point translatedPixel = Point.Add(pixel, new Vector(xPos, yPos));

                if (!(translatedPixel.X >= MessageMap.Count || translatedPixel.Y >= MessageMap[0].Count || translatedPixel.X < 0 || translatedPixel.Y < 0))
                    SetMap(translatedPixel, true);
            }
        }

        private InputText ProcessInputText()
        {
            List<Point> pointMap = new List<Point>();

            int distance = 8;
            string inputtedText = TextInput.Text;
            for(int counter = 0; counter < inputtedText.Length; counter++)
            {
                List<Point> getPoints = new List<Point>();
                EnglishFont.TryGetValue(inputtedText[counter].ToString(), out getPoints);

                if (getPoints == null)
                {
                    MessageBox.Show("Please enter a valid character.");
                    return new InputText(new List<Point>(), 0, 0);
                }

                foreach(Point pt in getPoints)
                {
                    pointMap.Add(Point.Add(pt, new Vector(counter * distance, 0)));
                }

            }

            int width = 0;
            int height = 0;

            foreach (Point point in pointMap)
            {
                if (point.X > width)
                {
                    width = (int)point.X;
                }
                if (point.Y > height)
                {
                    height = (int)point.Y;
                }
            }

            return new InputText(pointMap, width, height);
        }

        private void AnchorButton_Click(object sender, RoutedEventArgs e)
        {
            Button anchorButton = sender as Button;
            if (anchorButton != null)
            {
                string position = anchorButton.Name;
                TextAnchorMode = position.Substring(6, position.Length - 12);

                if (CurrentAnchorButton != null)
                    CurrentAnchorButton.IsEnabled = true;
                CurrentAnchorButton = anchorButton;
                CurrentAnchorButton.IsEnabled = false;
            }
        }
        

        private void PlayPreview_Click(object sender, RoutedEventArgs e)
        {
            if (Scrolling)
            {
                PlayPreview.IsEnabled = false;
                PausePreview.IsEnabled = true;

                PreviewTimer.Start();
                return;
            }

            EditingWidth = MessageMap.Count;
            EditingHeight = MessageMap[0].Count;

            int selection = DisplayModeSelect.SelectedIndex;

            if (selection == 0 || selection == 1)
            {
                SetMessageMapSize(EditingWidth + LEDBoardWidth * 2, EditingHeight, false);

                if (selection == 0)
                    ScrollPositionX = 0;
                else if (selection == 1)
                    ScrollPositionX = (MessageMap.Count - LEDBoardWidth);

                ScrollPositionY = 0;

                PreviewTimer.Interval = new TimeSpan(0, 0, 0, 0, HorizontalScrollingSpeed);
                for (int i = 0; i < LEDBoardWidth; i++)
                {
                    TranslateMap("Right");
                }
            }
            else if (selection == 4 || selection == 5)
            {
                SetMessageMapSize(EditingWidth, EditingHeight + LEDBoardHeight * 2, false);

                ScrollPositionX = 0;

                if (selection == 4)
                    ScrollPositionY = 0;
                else if (selection == 5)
                    ScrollPositionY = (MessageMap[0].Count - LEDBoardHeight);

                RollCounter = 0;

                PreviewTimer.Interval = new TimeSpan(0, 0, 0, 0, VerticalScrollingSpeed);
                for (int i = 0; i < LEDBoardHeight; i++)
                {
                    TranslateMap("Down");
                }
            }

            

            ResetLEDBoard();

            Scrolling = true;
            PreviewTimer.Start();




            PlayPreview.IsEnabled = false;
            PausePreview.IsEnabled = true;
            StopPreview.IsEnabled = true;

            DisplayModeSelect.IsEnabled = false;
            TranslateLeft.IsEnabled = false;
            TranslateRight.IsEnabled = false;
            TranslateUp.IsEnabled = false;
            TranslateDown.IsEnabled = false;

            AddTextButton.IsEnabled = false;
            TextPositionX.IsEnabled = false;
            TextPositionY.IsEnabled = false;

            AnchorTopLeftButton.IsEnabled = false;
            AnchorCenterTopButton.IsEnabled = false;
            AnchorTopRightButton.IsEnabled = false;
            AnchorCenterRightButton.IsEnabled = false;
            AnchorBottomRightButton.IsEnabled = false;
            AnchorCenterBottomButton.IsEnabled = false;
            AnchorBottomLeftButton.IsEnabled = false;
            AnchorCenterLeftButton.IsEnabled = false;
            AnchorCenterButton.IsEnabled = false;
            
            
            SizeConfirm.IsEnabled = false;
            ClearMessage.IsEnabled = false;
            MessageWidth.IsEnabled = false;
            MessageHeight.IsEnabled = false;

            ScrollBarX.IsEnabled = false;
            ScrollBarY.IsEnabled = false;

            

        }
        private void PausePreview_Click(object sender, RoutedEventArgs e)
        {
            PlayPreview.IsEnabled = true;
            PausePreview.IsEnabled = false;
            PreviewTimer.Stop();
        }
        private void StopPreview_Click(object sender, RoutedEventArgs e)
        {
            ScrollPositionX = 0;
            ScrollPositionY = 0;

            RollCounter = 0;

            int w = MessageMap.Count;
            int h = MessageMap[0].Count;

            int selection = DisplayModeSelect.SelectedIndex;

            if (selection == 0 || selection == 1)
            {
                for (int i = 0; i < LEDBoardWidth; i++)
                {
                    TranslateMap("Left");
                }
            }
            else if (selection == 4 || selection == 5)
            {
                for (int i = 0; i < LEDBoardHeight; i++)
                {
                    TranslateMap("Up");
                }
            }

            SetMessageMapSize(EditingWidth, EditingHeight, false);

            ResetLEDBoard();

            Scrolling = false;
            PreviewTimer.Stop();



            PlayPreview.IsEnabled = true;
            PausePreview.IsEnabled = false;
            StopPreview.IsEnabled = false;

            DisplayModeSelect.IsEnabled = true;
            TranslateLeft.IsEnabled = true;
            TranslateRight.IsEnabled = true;
            TranslateUp.IsEnabled = true;
            TranslateDown.IsEnabled = true;

            AddTextButton.IsEnabled = true;
            TextPositionX.IsEnabled = true;
            TextPositionY.IsEnabled = true;

            AnchorTopLeftButton.IsEnabled = true;
            AnchorCenterTopButton.IsEnabled = true;
            AnchorTopRightButton.IsEnabled = true;
            AnchorCenterRightButton.IsEnabled = true;
            AnchorBottomRightButton.IsEnabled = true;
            AnchorCenterBottomButton.IsEnabled = true;
            AnchorBottomLeftButton.IsEnabled = true;
            AnchorCenterLeftButton.IsEnabled = true;
            AnchorCenterButton.IsEnabled = true;


            SizeConfirm.IsEnabled = true;
            ClearMessage.IsEnabled = true;
            MessageWidth.IsEnabled = true;
            MessageHeight.IsEnabled = true;

            ScrollBarX.IsEnabled = true;
            ScrollBarY.IsEnabled = true;
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            int w = MessageMap.Count;
            int h = MessageMap[0].Count;

            int selection = DisplayModeSelect.SelectedIndex;

            if (selection == 0)
            {
                ScrollPositionX += 1;
                if (ScrollPositionX > (w - LEDBoardWidth))
                    ScrollPositionX = 0;
            }
            else if (selection == 1)
            {
                ScrollPositionX -= 1;
                if (ScrollPositionX < 0)
                    ScrollPositionX = (w - LEDBoardWidth);
            }
            else if (selection == 4 || selection == 5)
            {
                if (RollCounter == 0)
                {
                    PreviewTimer.Stop();
                    PreviewTimer.Interval = new TimeSpan(0, 0, 0, 0, VerticalScrollingSpeed);
                    PreviewTimer.Start();
                }
                RollCounter += 1;
                if (RollCounter == LEDBoardHeight)
                {
                    PreviewTimer.Stop();
                    PreviewTimer.Interval = new TimeSpan(0, 0, 0, 0, VerticalPauseSpeed);
                    PreviewTimer.Start();
                    RollCounter = 0;
                }

                bool condition = false;
                int setPosition = 0;
                if (selection == 4) 
                {
                    ScrollPositionY += 1;

                    condition = ScrollPositionY > (h - LEDBoardHeight);
                    setPosition = 0;
                }
                else if (selection == 5)
                {
                    ScrollPositionY -= 1;

                    condition = ScrollPositionY < 0;
                    setPosition = (h - LEDBoardHeight);
                }

                if (condition)
                {
                    RollCounter = 0;
                    ScrollPositionY = setPosition;
                }
            }

            ResetLEDBoard();
        }

        private void DisplayModeSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FinishedLoading)
                ReloadNewDisplayMode();
        }

        private void ReloadNewDisplayMode()
        {
            ScrollPositionX = 0;
            ScrollPositionY = 0;

            int selection = DisplayModeSelect.SelectedIndex;
            if (selection == 0 || selection == 1)
            {
                MessageHeight.Text = LEDBoardHeight.ToString();
                MessageHeight.IsEnabled = false;

                MessageWidth.IsEnabled = true;
            }
            else if (selection == 4 || selection == 5)
            {
                MessageWidth.Text = LEDBoardWidth.ToString();
                MessageWidth.IsEnabled = false;

                MessageHeight.IsEnabled = true;
            }
            else if (selection == 2 || selection == 3 || selection == 6)
            {
                MessageWidth.Text = LEDBoardWidth.ToString();
                MessageWidth.IsEnabled = false;

                MessageHeight.Text = LEDBoardHeight.ToString();
                MessageHeight.IsEnabled = false;
            }

            ResizeMessage();
        }

        private void SizeConfirm_Click(object sender, RoutedEventArgs e)
        {
            ScrollPositionX = 0;
            ScrollPositionY = 0;
            ResizeMessage();
        }

        private void ClearMessage_Click(object sender, RoutedEventArgs e)
        {
            SetMessageMapSize(MessageMap.Count, MessageMap[0].Count, true);
        }

        private void ResizeMessage()
        {
            if (MessageWidth.Text == "" || MessageHeight.Text == "")
            {
                MessageBox.Show("Please enter a number.");
                return;
            }

            int w = int.Parse(MessageWidth.Text);
            int h = int.Parse(MessageHeight.Text);

            SetMessageMapSize(w, h, false);

            ScrollBarX.Maximum = w - LEDBoardWidth;
            ScrollBarY.Maximum = h - LEDBoardHeight;
        }

        private void ScrollBarX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScrollPositionX = Convert.ToInt32(ScrollBarX.Value);
            ResetLEDBoard();
        }

        private void ScrollBarY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScrollPositionY = Convert.ToInt32(ScrollBarY.Value);
            ResetLEDBoard();
        }


        private void LED_Click(object sender, RoutedEventArgs e)
        {
            Button led = sender as Button;
            
            if (led != null && !Scrolling)
            {
                SwitchMap(FindLED(led));
            }
        }




        private void SetMessageMapSize(int width, int height, bool newMap)
        {
            if (newMap)
            {
                MessageMap.Clear();
                for (int x = 0; x < width; x++)
                {
                    MessageMap.Add(new List<int>());
                    for (int y = 0; y < height; y++)
                    {
                        MessageMap[x].Add(0);
                    }
                }
                ResetLEDBoard();
            }
            else if (width >= LEDBoardWidth && height >= LEDBoardHeight)
            {
                int currentWidth = MessageMap.Count;
                int currentHeight = MessageMap[0].Count;

                List<List<int>> createdMap = new List<List<int>>();
                for (int x = 0; x < width; x++)
                {
                    createdMap.Add(new List<int>());
                    for (int y = 0; y < height; y++)
                    {
                        if (x < currentWidth && y < currentHeight)
                            createdMap[x].Add(MessageMap[x][y]);
                        else
                            createdMap[x].Add(0);
                    }
                }
                MessageMap = createdMap;
                ResetLEDBoard();
            }
            else
            {
                MessageBox.Show(String.Format("Please enter a size of at least {0} wide and {1} tall.", LEDBoardWidth, LEDBoardHeight));
            }

        }

        private void TranslateLeft_Click(object sender, RoutedEventArgs e)
        {
            TranslateMap("Left");
        }

        private void TranslateRight_Click(object sender, RoutedEventArgs e)
        {
            TranslateMap("Right");
        }

        private void TranslateUp_Click(object sender, RoutedEventArgs e)
        {
            TranslateMap("Up");
        }

        private void TranslateDown_Click(object sender, RoutedEventArgs e)
        {
            TranslateMap("Down");
        }

        private void TranslateMap(string direction)
        {
            int width = MessageMap.Count;
            int height = MessageMap[0].Count;

            List<List<int>> translatedMap = new List<List<int>>();
            if (direction == "Left")
            {
                for (int x = 1; x < width; x++)
                {
                    translatedMap.Add(MessageMap[x]);
                }

                translatedMap.Add(new List<int>());
                for (int y = 0; y < height; y++)
                {
                    translatedMap[width - 1].Add(0);
                }
            }
            else if (direction == "Right")
            {
                translatedMap.Add(new List<int>());
                for (int y = 0; y < height; y++)
                {
                    translatedMap[0].Add(0);
                }

                for (int x = 0; x < (width-1); x++)
                {
                    translatedMap.Add(MessageMap[x]);
                }
            }
            else if (direction == "Up")
            {
                for (int x = 0; x < width; x++)
                {
                    translatedMap.Add(new List<int>());
                    for (int y = 1; y < height; y++)
                    {
                        translatedMap[x].Add(MessageMap[x][y]);
                    }
                    translatedMap[x].Add(0);
                }
            }
            else if (direction == "Down")
            {
                for (int x = 0; x < width; x++)
                {
                    translatedMap.Add(new List<int>());
                    translatedMap[x].Add(0);
                    for (int y = 0; y < (height - 1); y++)
                    {
                        translatedMap[x].Add(MessageMap[x][y]);
                    }
                }
            }

            MessageMap = translatedMap;

            ResetLEDBoard();
        }

        private void ResetLEDBoard()
        {
            for (int x = 0; x < LEDBoardWidth; x++)
            {
                for (int y = 0; y < LEDBoardHeight; y++) 
                {
                    SetMap(new Point(x + ScrollPositionX, y + ScrollPositionY), MessageMap[x + ScrollPositionX][y + ScrollPositionY] == 1);
                }
            }
        }

        private void SetMap(Point position, bool active)
        {
            MessageMap[(int)position.X][(int)position.Y] = active ? 1 : 0;

            if ((position.X - ScrollPositionX) < LEDBoardWidth
                && (position.X - ScrollPositionX) >= 0
                && (position.Y - ScrollPositionY) < LEDBoardHeight
                && (position.Y - ScrollPositionY) >= 0)
            {
                SetLED(CollectionLEDs[(int)position.X - ScrollPositionX][(int)position.Y - ScrollPositionY], active);
            }
            
        }

        private void SwitchMap(Point position)
        {
            int state = MessageMap[(int)position.X][(int)position.Y];
            if (state == 0)
            {
                SetMap(position, true);
            }
            else 
            {
                SetMap(position, false);
            }
        }

        private Point FindLED(Button led)
        {
            Point endPos = new Point(0, 0);

            for (int x = 0; x < CollectionLEDs.Count; x++)
            {
                int y = CollectionLEDs[x].IndexOf(led);

                if (y != -1)
                {
                    endPos = new Point(x + ScrollPositionX, y + ScrollPositionY);
                }
            }

            return endPos;
        }

        private void SetLED(Button led, bool active)
        {
            if (active)
            {
                led.Background = new SolidColorBrush(Colors.Red);
                led.BorderBrush = new SolidColorBrush(Colors.Firebrick);
            }
            else
            {
                led.Background = new SolidColorBrush(Colors.DarkGray);
                led.BorderBrush = new SolidColorBrush(Colors.DimGray);
            }
        }

        private void InitializeEnglishFont()
        {
            EnglishFont.Add(" ", Convert2DArrayToPointList(space));

            EnglishFont.Add("a", Convert2DArrayToPointList(letterA));
            EnglishFont.Add("b", Convert2DArrayToPointList(letterB));
            EnglishFont.Add("c", Convert2DArrayToPointList(letterC));
            EnglishFont.Add("d", Convert2DArrayToPointList(letterD));
            EnglishFont.Add("e", Convert2DArrayToPointList(letterE));
            EnglishFont.Add("f", Convert2DArrayToPointList(letterF));
            EnglishFont.Add("g", Convert2DArrayToPointList(letterG));
            EnglishFont.Add("h", Convert2DArrayToPointList(letterH));
            EnglishFont.Add("i", Convert2DArrayToPointList(letterI));
            EnglishFont.Add("j", Convert2DArrayToPointList(letterJ));
            EnglishFont.Add("k", Convert2DArrayToPointList(letterK));
            EnglishFont.Add("l", Convert2DArrayToPointList(letterL));
            EnglishFont.Add("m", Convert2DArrayToPointList(letterM));
            EnglishFont.Add("n", Convert2DArrayToPointList(letterN));
            EnglishFont.Add("o", Convert2DArrayToPointList(letterO));
            EnglishFont.Add("p", Convert2DArrayToPointList(letterP));
            EnglishFont.Add("q", Convert2DArrayToPointList(letterQ));
            EnglishFont.Add("r", Convert2DArrayToPointList(letterR));
            EnglishFont.Add("s", Convert2DArrayToPointList(letterS));
            EnglishFont.Add("t", Convert2DArrayToPointList(letterT));
            EnglishFont.Add("u", Convert2DArrayToPointList(letterU));
            EnglishFont.Add("v", Convert2DArrayToPointList(letterV));
            EnglishFont.Add("w", Convert2DArrayToPointList(letterW));
            EnglishFont.Add("x", Convert2DArrayToPointList(letterX));
            EnglishFont.Add("y", Convert2DArrayToPointList(letterY));
            EnglishFont.Add("z", Convert2DArrayToPointList(letterZ));


            EnglishFont.Add("A", Convert2DArrayToPointList(letterA));
            EnglishFont.Add("B", Convert2DArrayToPointList(letterB));
            EnglishFont.Add("C", Convert2DArrayToPointList(letterC));
            EnglishFont.Add("D", Convert2DArrayToPointList(letterD));
            EnglishFont.Add("E", Convert2DArrayToPointList(letterE));
            EnglishFont.Add("F", Convert2DArrayToPointList(letterF));
            EnglishFont.Add("G", Convert2DArrayToPointList(letterG));
            EnglishFont.Add("H", Convert2DArrayToPointList(letterH));
            EnglishFont.Add("I", Convert2DArrayToPointList(letterI));
            EnglishFont.Add("J", Convert2DArrayToPointList(letterJ));
            EnglishFont.Add("K", Convert2DArrayToPointList(letterK));
            EnglishFont.Add("L", Convert2DArrayToPointList(letterL));
            EnglishFont.Add("M", Convert2DArrayToPointList(letterM));
            EnglishFont.Add("N", Convert2DArrayToPointList(letterN));
            EnglishFont.Add("O", Convert2DArrayToPointList(letterO));
            EnglishFont.Add("P", Convert2DArrayToPointList(letterP));
            EnglishFont.Add("Q", Convert2DArrayToPointList(letterQ));
            EnglishFont.Add("R", Convert2DArrayToPointList(letterR));
            EnglishFont.Add("S", Convert2DArrayToPointList(letterS));
            EnglishFont.Add("T", Convert2DArrayToPointList(letterT));
            EnglishFont.Add("U", Convert2DArrayToPointList(letterU));
            EnglishFont.Add("V", Convert2DArrayToPointList(letterV));
            EnglishFont.Add("W", Convert2DArrayToPointList(letterW));
            EnglishFont.Add("X", Convert2DArrayToPointList(letterX));
            EnglishFont.Add("Y", Convert2DArrayToPointList(letterY));
            EnglishFont.Add("Z", Convert2DArrayToPointList(letterZ));
        }

        int[,] space = new int[10, 7]{
            {0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0}
        };

        int[,] letterA = new int[10,7]{
            {0,1,1,1,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,1,1,1,1,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0}
        };

        int[,] letterB = new int[10, 7]{
            {1,1,1,1,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,1,1,1,1,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,1,1,1,1,0,0}
        };

        int[,] letterC = new int[10, 7]{
            {0,0,1,1,1,0,0},
            {0,1,0,0,0,1,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {0,1,0,0,0,1,0},
            {0,0,1,1,1,0,0}
        };

        int[,] letterD = new int[10, 7]{
            {1,1,1,1,0,0,0},
            {1,0,0,0,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,1,0,0},
            {1,1,1,1,0,0,0}
        };

        int[,] letterE = new int[10, 7]{
            {1,1,1,1,1,1,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,1,1,1,1,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,1,1,1,1,1,0}
        };

        int[,] letterF = new int[10, 7]{
            {1,1,1,1,1,1,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,1,1,1,1,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0}
        };

        int[,] letterG = new int[10, 7]{
            {0,0,1,1,1,0,0},
            {0,1,0,0,0,1,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,1,1,0},
            {1,0,0,0,0,1,0},
            {0,1,0,0,0,1,0},
            {0,0,1,1,1,1,0}
        };

        int[,] letterH = new int[10, 7]{
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,1,1,1,1,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0}
        };

        int[,] letterI = new int[10, 7]{
            {0,1,1,1,1,1,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,1,1,1,1,1,0}
        };

        int[,] letterJ = new int[10, 7]{
            {0,0,0,1,1,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,1,0,0,1,0,0},
            {0,0,1,1,0,0,0}
        };

        int[,] letterK = new int[10, 7]{
            {1,0,0,0,0,1,0},
            {1,0,0,0,1,0,0},
            {1,0,0,1,0,0,0},
            {1,0,1,0,0,0,0},
            {1,1,0,0,0,0,0},
            {1,1,0,0,0,0,0},
            {1,0,1,0,0,0,0},
            {1,0,0,1,0,0,0},
            {1,0,0,0,1,0,0},
            {1,0,0,0,0,1,0}
        };

        int[,] letterL = new int[10, 7]{
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,1,1,1,1,1,0}
        };

        int[,] letterM = new int[10, 7]{
            {1,0,0,0,0,0,1},
            {1,1,0,0,0,1,1},
            {1,1,0,0,0,1,1},
            {1,0,1,0,1,0,1},
            {1,0,1,0,1,0,1},
            {1,0,0,1,0,0,1},
            {1,0,0,1,0,0,1},
            {1,0,0,0,0,0,1},
            {1,0,0,0,0,0,1},
            {1,0,0,0,0,0,1}
        };

        int[,] letterN = new int[10, 7]{
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,1,0,0,0,1,0},
            {1,0,1,0,0,1,0},
            {1,0,1,0,0,1,0},
            {1,0,0,1,0,1,0},
            {1,0,0,1,0,1,0},
            {1,0,0,0,1,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0}
        };

        int[,] letterO = new int[10, 7]{
            {0,0,1,1,0,0,0},
            {0,1,0,0,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {0,1,0,0,1,0,0},
            {0,0,1,1,0,0,0}
        };

        int[,] letterP = new int[10, 7]{
            {1,1,1,1,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,1,1,1,1,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0}
        };

        int[,] letterQ = new int[10, 7]{
            {0,0,1,1,0,0,0},
            {0,1,0,0,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,1,0,1,0},
            {0,1,0,0,1,0,0},
            {0,0,1,1,0,1,0}
        };

        int[,] letterR = new int[10, 7]{
            {1,1,1,1,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,1,1,1,1,0,0},
            {1,0,0,1,0,0,0},
            {1,0,0,0,1,0,0},
            {1,0,0,0,1,0,0},
            {1,0,0,0,0,1,0}
        };

        int[,] letterS = new int[10, 7]{
            {0,1,1,1,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {0,1,1,1,1,0,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {0,1,1,1,1,0,0}
        };

        int[,] letterT = new int[10, 7]{
            {1,1,1,1,1,1,1},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0}
        };

        int[,] letterU = new int[10, 7]{
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {0,1,1,1,1,0,0}
        };

        int[,] letterV = new int[10, 7]{
            {1,0,0,0,0,0,1},
            {1,0,0,0,0,0,1},
            {1,0,0,0,0,0,1},
            {0,1,0,0,0,1,0},
            {0,1,0,0,0,1,0},
            {0,1,0,0,0,1,0},
            {0,0,1,0,1,0,0},
            {0,0,1,0,1,0,0},
            {0,0,1,0,1,0,0},
            {0,0,0,1,0,0,0}
        };

        int[,] letterW = new int[10, 7]{
            {1,0,0,0,0,0,1},
            {1,0,0,0,0,0,1},
            {1,0,0,0,0,0,1},
            {1,0,0,0,0,0,1},
            {1,0,0,1,0,0,1},
            {1,0,0,1,0,0,1},
            {1,0,1,0,1,0,1},
            {1,0,1,0,1,0,1},
            {0,1,0,0,0,1,0},
            {0,1,0,0,0,1,0}
        };

        int[,] letterX = new int[10, 7]{
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {0,1,0,0,1,0,0},
            {0,1,0,0,1,0,0},
            {0,0,1,1,0,0,0},
            {0,0,1,1,0,0,0},
            {0,1,0,0,1,0,0},
            {0,1,0,0,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0}
        };

        int[,] letterY = new int[10, 7]{
            {1,0,0,0,0,0,1},
            {1,0,0,0,0,0,1},
            {0,1,0,0,0,1,0},
            {0,1,0,0,0,1,0},
            {0,0,1,0,1,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0}
        };

        int[,] letterZ = new int[10, 7]{
            {1,1,1,1,1,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,1,0,0},
            {0,0,0,1,0,0,0},
            {0,0,1,0,0,0,0},
            {0,1,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,1,1,1,1,1,0}
        };

    }

    public struct InputText
    {
        public List<Point> PointMap;
        public int Width;
        public int Height;

        public InputText(List<Point> pointMap, int width, int height)
        {
            PointMap = pointMap;
            Width = width;
            Height = height;
        }
    }

}