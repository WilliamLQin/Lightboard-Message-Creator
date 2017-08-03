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
//using SysDraw = System.Drawing;
//using System.Drawing.Imaging;
//using System.Runtime.InteropServices;


namespace Lightboard_Message_Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //string TextAnchorMode;
        //Button CurrentAnchorButton;
        string DrawMode;
        Point CurrentTextModePosition;
        bool TextModeOn = false;
        DispatcherTimer TextPositionFlashingTimer;
        int TextPositionInitialState = 0;
        List<Point> PreviousLetterPositions = new List<Point>();
        List<List<Point>> PreviousLetters = new List<List<Point>>();

        List<List<Button>> CollectionLEDs;
        List<List<int>> MessageMap;

        List<List<int>> CenterOutStorage = new List<List<int>>();
        int LEDBoardWidth = 72; // Target -> 72
        int LEDBoardHeight = 22; // Target -> 22

        DispatcherTimer PreviewTimer;
        int HorizontalScrollingSpeed = 50;
        int VerticalScrollingSpeed = 30;
        int VerticalPauseSpeed = 3000;
        int FlashSpeed = 1000;

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
                    yPos += 1;
                }

                
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

            //TextAnchorMode = "TopLeft";
            //CurrentAnchorButton = AnchorTopLeftButton;
            //CurrentAnchorButton.IsEnabled = false;

            DrawModePencil.IsEnabled = false;
            DrawMode = "Pencil";

            TextPositionFlashingTimer = new DispatcherTimer();
            TextPositionFlashingTimer.Tick += new EventHandler(TextPositionFlashingTimer_Tick);
            TextPositionFlashingTimer.Interval = new TimeSpan(0, 0, 0, 0, 300);

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

        //private void AddTextButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (TextPositionX.Text == "" || TextPositionY.Text == "")
        //    {
        //        MessageBox.Show("Please enter a number.");
        //        return;
        //    }

        //    int xPos = int.Parse(TextPositionX.Text) - 1;
        //    int yPos = int.Parse(TextPositionY.Text) - 1;

        //    InputText inputText = ProcessInputText(TextInput.Text);

        //    if (inputText.PointMap.Count == 0)
        //        return;

        //    switch(TextAnchorMode)
        //    {
        //        case "TopLeft":
        //            break;

        //        case "CenterTop":
        //            xPos -= (int)(inputText.Width / 2);
        //            break;

        //        case "TopRight":
        //            xPos -= inputText.Width;
        //            break;

        //        case "CenterRight":
        //            xPos -= inputText.Width;
        //            yPos -= (int)(inputText.Height / 2);
        //            break;

        //        case "BottomRight":
        //            xPos -= inputText.Width;
        //            yPos -= inputText.Height;
        //            break;

        //        case "CenterBottom":
        //            xPos -= (int)(inputText.Width / 2);
        //            yPos -= inputText.Height;
        //            break;

        //        case "BottomLeft":
        //            yPos -= inputText.Height;
        //            break;

        //        case "CenterLeft":
        //            yPos -= (int)(inputText.Height / 2);
        //            break;

        //        case "Center":
        //            xPos -= (int)(inputText.Width / 2);
        //            yPos -= (int)(inputText.Height / 2);
        //            break;

        //    }

        //    foreach (Point pixel in inputText.PointMap)
        //    {
        //        Point translatedPixel = Point.Add(pixel, new Vector(xPos, yPos));

        //        if (!(translatedPixel.X >= MessageMap.Count || translatedPixel.Y >= MessageMap[0].Count || translatedPixel.X < 0 || translatedPixel.Y < 0))
        //            SetMap(translatedPixel, true);
        //    }
        //}

        //private InputText ProcessInputText(string inputtedText)
        //{
        //    List<Point> pointMap = new List<Point>();

        //    int distance = 8;
        //    for (int counter = 0; counter < inputtedText.Length; counter++)
        //    {
        //        List<Point> getPoints = new List<Point>();
        //        EnglishFont.TryGetValue(inputtedText[counter].ToString(), out getPoints);

        //        if (getPoints == null)
        //        {
        //            MessageBox.Show("Please enter a valid character (A-Z, 0-9).");
        //            return new InputText(new List<Point>(), 0, 0);
        //        }

        //        foreach (Point pt in getPoints)
        //        {
        //            pointMap.Add(Point.Add(pt, new Vector(counter * distance, 0)));
        //        }

        //    }

        //    int width = 0;
        //    int height = 0;

        //    foreach (Point point in pointMap)
        //    {
        //        if (point.X > width)
        //        {
        //            width = (int)point.X;
        //        }
        //        if (point.Y > height)
        //        {
        //            height = (int)point.Y;
        //        }
        //    }

        //    return new InputText(pointMap, width, height);
        //}

        private InputText ProcessInputText(string inputtedText) // variant with no loop
        {
            List<Point> pointMap = new List<Point>();

            EnglishFont.TryGetValue(inputtedText.ToString(), out pointMap);
            
            if (inputtedText == "Space")
            {
                return new InputText(new List<Point>(), 5, 0);
            }
            else if (pointMap == null)
            {
                MessageBox.Show("Please enter a valid character (A-Z, 0-9).");
                return new InputText(new List<Point>(), 0, 0);
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

        //private void AnchorButton_Click(object sender, RoutedEventArgs e)
        //{
        //    Button anchorButton = sender as Button;
        //    if (anchorButton != null)
        //    {
        //        string position = anchorButton.Name;
        //        TextAnchorMode = position.Substring(6, position.Length - 12);

        //        if (CurrentAnchorButton != null)
        //            CurrentAnchorButton.IsEnabled = true;
        //        CurrentAnchorButton = anchorButton;
        //        CurrentAnchorButton.IsEnabled = false;
        //    }
        //}



        private void DrawMode_Click(object sender, RoutedEventArgs e)
        {
            Button mode = sender as Button;
            if (mode != null)
            {
                DrawModePencil.IsEnabled = true;
                DrawModeEraser.IsEnabled = true;
                DrawModeText.IsEnabled = true;
                DrawModeSwitch.IsEnabled = true;
                ResetTextPosition();
                mode.IsEnabled = false;
                DrawMode = mode.Tag.ToString();

                if (DrawMode == "Text")
                {
                    TextModeOn = true;
                }
                else
                {
                    TextModeOn = false;
                }
            }
        }

        private void TextPositionFlashingTimer_Tick(object sender, EventArgs e)
        {
            if (!(CurrentTextModePosition.X >= MessageMap.Count || CurrentTextModePosition.Y >= MessageMap[0].Count || CurrentTextModePosition.X < 0 || CurrentTextModePosition.Y < 0))
                SwitchMap(CurrentTextModePosition);
        }

        private void ResetTextPosition()
        {
            if (!(CurrentTextModePosition.X >= MessageMap.Count || CurrentTextModePosition.Y >= MessageMap[0].Count || CurrentTextModePosition.X < 0 || CurrentTextModePosition.Y < 0))
                SetMap(CurrentTextModePosition, TextPositionInitialState == 1);
            TextPositionFlashingTimer.Stop();
        }

        private void LED_Click(object sender, RoutedEventArgs e)
        {
            Button led = sender as Button;

            if (led != null && !Scrolling)
            {
                switch (DrawMode)
                {
                    case "Switch":
                        SwitchMap(FindLED(led));
                        ResetTextPosition();
                        break;

                    case "Pencil":
                        SetMap(FindLED(led), true);
                        ResetTextPosition();
                        break;

                    case "Eraser":
                        SetMap(FindLED(led), false);
                        ResetTextPosition();
                        break;

                    case "Text":
                        ResetTextPosition();
                        Point pos = FindLED(led);
                        TextPositionInitialState = MessageMap[(int)pos.X][(int)pos.Y];
                        CurrentTextModePosition = pos;
                        TextPositionFlashingTimer.Start();
                        break;
                }

            }

            Unfocus.IsEnabled = true;
            Unfocus.Focus();
            Unfocus.IsEnabled = false;
        }

        private void TextKey_Pressed (object sender, KeyEventArgs e)
        {
            string pressedKey = e.Key.ToString();

            if (e.Key == Key.Back && PreviousLetters != null && PreviousLetters.Count > 0)
            {
                ResetTextPosition();

                foreach (Point pixel in PreviousLetters[PreviousLetters.Count - 1])
                {
                    Point translatedPixel = Point.Add(pixel, new Vector(PreviousLetterPositions[PreviousLetterPositions.Count - 1].X, PreviousLetterPositions[PreviousLetterPositions.Count - 1].Y));

                    if (!(translatedPixel.X >= MessageMap.Count || translatedPixel.Y >= MessageMap[0].Count || translatedPixel.X < 0 || translatedPixel.Y < 0))
                        SetMap(translatedPixel, false);
                }

                CurrentTextModePosition = PreviousLetterPositions[PreviousLetterPositions.Count - 1];
                TextPositionFlashingTimer.Start();
                PreviousLetters.RemoveAt(PreviousLetters.Count - 1);
                PreviousLetterPositions.RemoveAt(PreviousLetterPositions.Count - 1);
            }
            else if (TextModeOn && !(CurrentTextModePosition.X >= MessageMap.Count || CurrentTextModePosition.Y >= MessageMap[0].Count || CurrentTextModePosition.X < 0 || CurrentTextModePosition.Y < 0))
            {
                InputText inputText = ProcessInputText(pressedKey);

                if (inputText.Width <= 0)
                    return;

                ResetTextPosition();

                foreach (Point pixel in inputText.PointMap)
                {
                    Point translatedPixel = Point.Add(pixel, new Vector(CurrentTextModePosition.X, CurrentTextModePosition.Y));

                    if (!(translatedPixel.X >= MessageMap.Count || translatedPixel.Y >= MessageMap[0].Count || translatedPixel.X < 0 || translatedPixel.Y < 0))
                        SetMap(translatedPixel, true);
                }

                PreviousLetterPositions.Add(new Point(CurrentTextModePosition.X, CurrentTextModePosition.Y));
                PreviousLetters.Add(inputText.PointMap);

                CurrentTextModePosition = Point.Add(CurrentTextModePosition, new Vector(inputText.Width + 2, 0));
                TextPositionFlashingTimer.Start();

            }
        }
        

        private void PlayPreview_Click(object sender, RoutedEventArgs e)
        {

            PreviewTimer.Stop();
            ResetTextPosition();

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
            else if (selection == 3)
            {
                SetMessageMapSize(EditingWidth, EditingHeight * 2, false);

                ScrollPositionX = 0;
                ScrollPositionY = 0;

                PreviewTimer.Interval = new TimeSpan(0, 0, 0, 0, FlashSpeed);
            }
            else if (selection == 2)
            {
                CenterOutStorage.Clear();

                foreach(List<int> column in MessageMap)
                {
                    CenterOutStorage.Add(column);
                }

                PreviewTimer.Interval = new TimeSpan(0, 0, 0, 0, HorizontalScrollingSpeed);
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

            //AddTextButton.IsEnabled = false;
            //TextPositionX.IsEnabled = false;
            //TextPositionY.IsEnabled = false;

            //AnchorTopLeftButton.IsEnabled = false;
            //AnchorCenterTopButton.IsEnabled = false;
            //AnchorTopRightButton.IsEnabled = false;
            //AnchorCenterRightButton.IsEnabled = false;
            //AnchorBottomRightButton.IsEnabled = false;
            //AnchorCenterBottomButton.IsEnabled = false;
            //AnchorBottomLeftButton.IsEnabled = false;
            //AnchorCenterLeftButton.IsEnabled = false;
            //AnchorCenterButton.IsEnabled = false;

            DrawModePencil.IsEnabled = false;
            DrawModeEraser.IsEnabled = false;
            DrawModeText.IsEnabled = false;
            DrawModeSwitch.IsEnabled = false;

            
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
            else if (selection == 2)
            {
                MessageMap = CenterOutStorage;
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

            //AddTextButton.IsEnabled = true;
            //TextPositionX.IsEnabled = true;
            //TextPositionY.IsEnabled = true;

            //AnchorTopLeftButton.IsEnabled = true;
            //AnchorCenterTopButton.IsEnabled = true;
            //AnchorTopRightButton.IsEnabled = true;
            //AnchorCenterRightButton.IsEnabled = true;
            //AnchorBottomRightButton.IsEnabled = true;
            //AnchorCenterBottomButton.IsEnabled = true;
            //AnchorBottomLeftButton.IsEnabled = true;
            //AnchorCenterLeftButton.IsEnabled = true;
            //AnchorCenterButton.IsEnabled = true;

            DrawModePencil.IsEnabled = true;
            DrawModeEraser.IsEnabled = true;
            DrawModeText.IsEnabled = true;
            DrawModeSwitch.IsEnabled = true;
             

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
            else if (selection == 3)
            {
                if (ScrollPositionY == 0)
                {
                    ScrollPositionY = LEDBoardHeight;
                }
                else
                {
                    ScrollPositionY = 0;
                }
            }
            else if (selection == 2)
            {
                List<int> storageLeft = MessageMap[0];
                List<int> storageRight = MessageMap[MessageMap.Count-1];

                int rightMiddle = (int)(Math.Round((double)(LEDBoardWidth/2)));

                for (int x = 1; x < (rightMiddle); x++)
                {
                     MessageMap[x - 1] = MessageMap[x];
                     MessageMap[MessageMap.Count - x] = MessageMap[MessageMap.Count - x - 1];
                }

                MessageMap[rightMiddle - 1] = storageLeft;
                MessageMap[rightMiddle] = storageRight;

                ResetLEDBoard();

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

        //int FontSize = 10;

        //private static System.Drawing.Bitmap GeometryToBitmap(Geometry geometry, int TargetSize)
        //{
        //    var rect = geometry.GetRenderBounds(new Pen(Brushes.Black, 0));

        //    var bigger = rect.Width > rect.Height ? rect.Width : rect.Height;
        //    var scale = TargetSize / bigger;

        //    Geometry scaledGeometry = Geometry.Combine(geometry, geometry, GeometryCombineMode.Intersect, new ScaleTransform(scale, scale));
        //    rect = scaledGeometry.GetRenderBounds(new Pen(Brushes.Black, 0));

        //    Geometry transformedGeometry = Geometry.Combine(scaledGeometry, scaledGeometry, GeometryCombineMode.Intersect, new TranslateTransform(((TargetSize - rect.Width) / 2) - rect.Left, ((TargetSize - rect.Height) / 2) - rect.Top));

        //    RenderTargetBitmap bmp = new RenderTargetBitmap(TargetSize, TargetSize, // Size
        //                                                    96, 96, // DPI 
        //                                                    PixelFormats.Pbgra32);

        //    DrawingVisual viz = new DrawingVisual();
        //    using (DrawingContext dc = viz.RenderOpen())
        //    {
        //        dc.DrawGeometry(Brushes.Black, null, transformedGeometry);
        //    }

        //    bmp.Render(viz);

        //    PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
        //    pngEncoder.Frames.Add(BitmapFrame.Create(bmp));

        //    System.IO.MemoryStream ms = new System.IO.MemoryStream();
        //    pngEncoder.Save(ms);

        //    System.Drawing.Bitmap bumpuh = new System.Drawing.Bitmap(System.Drawing.Bitmap.FromStream(ms));

        //    return CopyToBpp(bumpuh, 8);
        //}
        //public static int[] BitmapToByteArray(System.Drawing.Bitmap image)
        //{
        //    byte[] returns = null;
        //    if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
        //    {
        //        System.Drawing.Imaging.BitmapData bitmapData = image.LockBits(
        //                                        new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
        //                                        System.Drawing.Imaging.ImageLockMode.ReadWrite,
        //                                        image.PixelFormat);
        //        int noOfPixels = image.Width * image.Height;
        //        int colorDepth = System.Drawing.Bitmap.GetPixelFormatSize(image.PixelFormat);
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

        ///// <summary>
        ///// Copies a bitmap into a 1bpp/8bpp bitmap of the same dimensions, fast
        ///// </summary>
        ///// <param name="b">original bitmap</param>
        ///// <param name="bpp">1 or 8, target bpp</param>
        ///// <returns>a 1bpp copy of the bitmap</returns>
        //static System.Drawing.Bitmap CopyToBpp(System.Drawing.Bitmap b, int bpp)
        //{
        //    if (bpp != 1 && bpp != 8) throw new System.ArgumentException("1 or 8", "bpp");

        //    // Plan: built into Windows GDI is the ability to convert
        //    // bitmaps from one format to another. Most of the time, this
        //    // job is actually done by the graphics hardware accelerator card
        //    // and so is extremely fast. The rest of the time, the job is done by
        //    // very fast native code.
        //    // We will call into this GDI functionality from C#. Our plan:
        //    // (1) Convert our Bitmap into a GDI hbitmap (ie. copy unmanaged->managed)
        //    // (2) Create a GDI monochrome hbitmap
        //    // (3) Use GDI "BitBlt" function to copy from hbitmap into monochrome (as above)
        //    // (4) Convert the monochrone hbitmap into a Bitmap (ie. copy unmanaged->managed)

        //    int w = b.Width, h = b.Height;
        //    IntPtr hbm = b.GetHbitmap(); // this is step (1)
        //    //
        //    // Step (2): create the monochrome bitmap.
        //    // "BITMAPINFO" is an interop-struct which we define below.
        //    // In GDI terms, it's a BITMAPHEADERINFO followed by an array of two RGBQUADs
        //    BITMAPINFO bmi = new BITMAPINFO();
        //    bmi.biSize = 40;  // the size of the BITMAPHEADERINFO struct
        //    bmi.biWidth = w;
        //    bmi.biHeight = h;
        //    bmi.biPlanes = 1; // "planes" are confusing. We always use just 1. Read MSDN for more info.
        //    bmi.biBitCount = (short)bpp; // ie. 1bpp or 8bpp
        //    bmi.biCompression = BI_RGB; // ie. the pixels in our RGBQUAD table are stored as RGBs, not palette indexes
        //    bmi.biSizeImage = (uint)(((w + 7) & 0xFFFFFFF8) * h / 8);
        //    bmi.biXPelsPerMeter = 1000000; // not really important
        //    bmi.biYPelsPerMeter = 1000000; // not really important
        //    // Now for the colour table.
        //    uint ncols = (uint)1 << bpp; // 2 colours for 1bpp; 256 colours for 8bpp
        //    bmi.biClrUsed = ncols;
        //    bmi.biClrImportant = ncols;
        //    bmi.cols = new uint[256]; // The structure always has fixed size 256, even if we end up using fewer colours
        //    if (bpp == 1) { bmi.cols[0] = MAKERGB(0, 0, 0); bmi.cols[1] = MAKERGB(255, 255, 255); }
        //    else { for (int i = 0; i < ncols; i++) bmi.cols[i] = MAKERGB(i, i, i); }
        //    // For 8bpp we've created an palette with just greyscale colours.
        //    // You can set up any palette you want here. Here are some possibilities:
        //    // greyscale: for (int i=0; i<256; i++) bmi.cols[i]=MAKERGB(i,i,i);
        //    // rainbow: bmi.biClrUsed=216; bmi.biClrImportant=216; int[] colv=new int[6]{0,51,102,153,204,255};
        //    //          for (int i=0; i<216; i++) bmi.cols[i]=MAKERGB(colv[i/36],colv[(i/6)%6],colv[i%6]);
        //    // optimal: a difficult topic: http://en.wikipedia.org/wiki/Color_quantization
        //    // 
        //    // Now create the indexed bitmap "hbm0"
        //    IntPtr bits0; // not used for our purposes. It returns a pointer to the raw bits that make up the bitmap.
        //    IntPtr hbm0 = CreateDIBSection(IntPtr.Zero, ref bmi, DIB_RGB_COLORS, out bits0, IntPtr.Zero, 0);
        //    //
        //    // Step (3): use GDI's BitBlt function to copy from original hbitmap into monocrhome bitmap
        //    // GDI programming is kind of confusing... nb. The GDI equivalent of "Graphics" is called a "DC".
        //    IntPtr sdc = GetDC(IntPtr.Zero);       // First we obtain the DC for the screen
        //    // Next, create a DC for the original hbitmap
        //    IntPtr hdc = CreateCompatibleDC(sdc); SelectObject(hdc, hbm);
        //    // and create a DC for the monochrome hbitmap
        //    IntPtr hdc0 = CreateCompatibleDC(sdc); SelectObject(hdc0, hbm0);
        //    // Now we can do the BitBlt:
        //    BitBlt(hdc0, 0, 0, w, h, hdc, 0, 0, SRCCOPY);
        //    // Step (4): convert this monochrome hbitmap back into a Bitmap:
        //    System.Drawing.Bitmap b0 = System.Drawing.Bitmap.FromHbitmap(hbm0);
        //    //
        //    // Finally some cleanup.
        //    DeleteDC(hdc);
        //    DeleteDC(hdc0);
        //    ReleaseDC(IntPtr.Zero, sdc);
        //    DeleteObject(hbm);
        //    DeleteObject(hbm0);
        //    //
        //    return b0;
        //}
        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //public static extern bool DeleteObject(IntPtr hObject);

        //[System.Runtime.InteropServices.DllImport("user32.dll")]
        //public static extern IntPtr GetDC(IntPtr hwnd);

        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        //[System.Runtime.InteropServices.DllImport("user32.dll")]
        //public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //public static extern int DeleteDC(IntPtr hdc);

        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //public static extern int BitBlt(IntPtr hdcDst, int xDst, int yDst, int w, int h, IntPtr hdcSrc, int xSrc, int ySrc, int rop);
        //static int SRCCOPY = 0x00CC0020;

        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO bmi, uint Usage, out IntPtr bits, IntPtr hSection, uint dwOffset);
        //static uint BI_RGB = 0;
        //static uint DIB_RGB_COLORS = 0;
        //[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        //public struct BITMAPINFO
        //{
        //    public uint biSize;
        //    public int biWidth, biHeight;
        //    public short biPlanes, biBitCount;
        //    public uint biCompression, biSizeImage;
        //    public int biXPelsPerMeter, biYPelsPerMeter;
        //    public uint biClrUsed, biClrImportant;
        //    [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 256)]
        //    public uint[] cols;
        //}

        //static uint MAKERGB(int r, int g, int b)
        //{
        //    return ((uint)(b & 255)) | ((uint)((r & 255) << 8)) | ((uint)((g & 255) << 16));
        //}


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
            {1,1,1,1,1,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {1,1,1,1,1,0,0}
        };

        int[,] letterJ = new int[10, 7]{
            {0,0,1,1,1,0,0},
            {0,0,0,0,1,0,0},
            {0,0,0,0,1,0,0},
            {0,0,0,0,1,0,0},
            {0,0,0,0,1,0,0},
            {0,0,0,0,1,0,0},
            {0,0,0,0,1,0,0},
            {0,0,0,0,1,0,0},
            {1,0,0,1,0,0,0},
            {0,1,1,0,0,0,0}
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