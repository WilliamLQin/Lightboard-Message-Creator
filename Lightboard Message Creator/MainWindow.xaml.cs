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

        // Store current drawing mode
        string DrawMode;
        // Text Mode variables
        Point CurrentTextModePosition;
        bool TextModeOn = false;
        DispatcherTimer TextPositionFlashingTimer;
        int TextPositionInitialState = 0;
        // Store previously typed letters to delete on pressing Backspace (Text Mode)
        List<Point> PreviousLetterPositions = new List<Point>();
        List<List<Point>> PreviousLetters = new List<List<Point>>();

        // Maps of UI Buttons and a bitmap for recording states respectively
        List<List<Button>> CollectionLEDs;
        List<List<int>> MessageMap;

        // UI number of buttons horizontally and vertically
        int LEDBoardWidth = 72; // Target -> 72
        int LEDBoardHeight = 22; // Target -> 22

        // Preview parameters for display modes
        DispatcherTimer PreviewTimer;
        int HorizontalScrollingSpeed = 50;
        int VerticalScrollingSpeed = 30;
        int VerticalPauseSpeed = 3000;
        int FlashSpeed = 1000;

        // Storage variables for preview mode
        List<List<int>> CenterOutStorage = new List<List<int>>();
        int EditingWidth = 12;
        int EditingHeight = 12;
        int RollCounter = 0;
        bool Scrolling = false;

        // Scrolling state
        int ScrollPositionX = 0;
        int ScrollPositionY = 0;

        // Prevents events from occuring before Window_ContentRendered();
        bool FinishedLoading = false;
        
        // Hard coded dictionary for which coordinates in a character are on
        Dictionary<string, List<Point>> EnglishFont;


        public MainWindow()
        {
            InitializeComponent();
        }

        // Initialize window -> runs on start
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // Populate list of buttons on LED board
            /* In a sample 4x3 board (12 buttons), the buttons in the .xaml file will placed at the following indices
             * <Button/> -> [0][0]
             * <Button/> -> [0][1]
             * <Button/> -> [0][2]
             * <Button/> -> [1][0]
             * <Button/> -> [1][1]
             * <Button/> -> [1][2]
             * <Button/> -> [2][0]
             * <Button/> -> [2][1]
             * <Button/> -> [2][2]
             * <Button/> -> [3][0]
             * <Button/> -> [3][1]
             * <Button/> -> [3][2]
             * 
             * As such, when laying out the buttons on the .xaml file, lay the buttons out in columns:
             * 
             * 0  3  6  9
             * 1  4  7  10
             * 2  5  8  11
             */
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


            // Initialize bitmap with same width and height of UI button board
            MessageMap = new List<List<int>>();
            SetMessageMapSize(LEDBoardWidth, LEDBoardHeight, true); // Run function SetMessageMapSize(), true means that it is a new 2d list
            // Set UI textbox input elements for width and height to display width and height of UI board
            MessageWidth.Text = LEDBoardWidth.ToString();
            MessageHeight.Text = LEDBoardHeight.ToString();

            // Refresh display mode settings based on default set in .xaml
            ReloadNewDisplayMode();

            // Initialize timer necessary for the preview
            PreviewTimer = new DispatcherTimer();
            PreviewTimer.Tick += new EventHandler(PreviewTimer_Tick);
            PreviewTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);

            // Set default drawing mode
            DrawModePencil.IsEnabled = false;
            DrawMode = "Pencil";

            // Initialize timer necessary for text drawing mode
            TextPositionFlashingTimer = new DispatcherTimer();
            TextPositionFlashingTimer.Tick += new EventHandler(TextPositionFlashingTimer_Tick);
            TextPositionFlashingTimer.Interval = new TimeSpan(0, 0, 0, 0, 300);

            // Initialize hard coded font
            EnglishFont = new Dictionary<string, List<Point>>();
            InitializeEnglishFont();

            // Allow some other functions to run
            FinishedLoading = true;
        }

        // Used for hard coded font.
        // Converts a 2d array of integers to a list of which coordinates are set to 1
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

        // Limits some text box inputs to numbers only
        // Necessary for width and height adjustment
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Functions for top toolbar to open a new window, open a text file to import it into the application, and save current message to a text file
        // Work in progress
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

        // Compares string inputtedText with Dictionary<string, List<Point> EnglishFont to retrieve a List<Point> corresponding to inputtedText
        // InputText is a struct used to also include the width and height of the character retrieved from EnglishFont
        // Spaces have a set width of 5, as they produce empty lists
        private InputText ProcessInputText(string inputtedText)
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

        // Event triggers when a draw mode is selected in the second toolbar
        // Sets the current drawing mode
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

        // Switches the button at the current position of the text cursor to indicate its position
        private void TextPositionFlashingTimer_Tick(object sender, EventArgs e)
        {
            if (!(CurrentTextModePosition.X >= MessageMap.Count || CurrentTextModePosition.Y >= MessageMap[0].Count || CurrentTextModePosition.X < 0 || CurrentTextModePosition.Y < 0))
                SwitchMap(CurrentTextModePosition);
        }

        // Resets the current position of the text cursor to its original state and stops the timer
        private void ResetTextPosition()
        {
            if (!(CurrentTextModePosition.X >= MessageMap.Count || CurrentTextModePosition.Y >= MessageMap[0].Count || CurrentTextModePosition.X < 0 || CurrentTextModePosition.Y < 0))
                SetMap(CurrentTextModePosition, TextPositionInitialState == 1);
            TextPositionFlashingTimer.Stop();
        }

        // Event triggers when a button on the UI button board is pressed
        // Sets the corresponding position on the 2d int array MessageMap based on the drawing mode
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

        // Event triggers when a key is pressed
        // Only works when TextModeOn is true
        // When key pressed is backspace, delete the previously entered letter
        // Otherwise, check if key exists in hard coded font, set the resulting coordinates on MessageMap, and move the text cursor to new position
        private void TextKey_Pressed (object sender, KeyEventArgs e)
        {
            string pressedKey = e.Key.ToString();

            if (TextModeOn && e.Key == Key.Back && PreviousLetters != null && PreviousLetters.Count > 0)
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
        
        // Event triggers on pressing play button
        // If preview is currently paused, resume
        // Else, prepare board and map for selected preview mode and begin preview
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

            // Store MessageMap width and height prior to changing it for preview
            EditingWidth = MessageMap.Count;
            EditingHeight = MessageMap[0].Count;

            // Adjust accordingly to display mode
            /* 0 -> left scroll
             * 1 -> right scroll
             * 2 -> center out
             * 3 -> flash
             * 4 -> roll up
             * 5 -> roll down
             * 6 -> steady on
             * */
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
            
            // Refresh UI button board and begin preview
            ResetLEDBoard();

            Scrolling = true;
            PreviewTimer.Start();



            // Enable/disable controls during preview
            PlayPreview.IsEnabled = false;
            PausePreview.IsEnabled = true;
            StopPreview.IsEnabled = true;

            DisplayModeSelect.IsEnabled = false;
            TranslateLeft.IsEnabled = false;
            TranslateRight.IsEnabled = false;
            TranslateUp.IsEnabled = false;
            TranslateDown.IsEnabled = false;

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
        // Event triggers on pressing pause button
        // Pause current preview
        private void PausePreview_Click(object sender, RoutedEventArgs e)
        {
            PlayPreview.IsEnabled = true;
            PausePreview.IsEnabled = false;
            PreviewTimer.Stop();
        }
        // Event triggers on pressing stop button
        // Stop preview and reset to state prior to preview
        private void StopPreview_Click(object sender, RoutedEventArgs e)
        {
            ScrollPositionX = 0;
            ScrollPositionY = 0;

            RollCounter = 0;

            int w = MessageMap.Count;
            int h = MessageMap[0].Count;

            // Different resets for different display modes
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

            // Refresh UI button board and stop
            ResetLEDBoard();

            Scrolling = false;
            PreviewTimer.Stop();


            // Enable/Disable controls
            PlayPreview.IsEnabled = true;
            PausePreview.IsEnabled = false;
            StopPreview.IsEnabled = false;

            DisplayModeSelect.IsEnabled = true;
            TranslateLeft.IsEnabled = true;
            TranslateRight.IsEnabled = true;
            TranslateUp.IsEnabled = true;
            TranslateDown.IsEnabled = true;

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
        // Event triggers on timer tick
        // Progresses preview based on preview mode
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

        // Event triggers when display mode combo box selection is changed
        // Changes the display mode
        private void DisplayModeSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FinishedLoading)
                ReloadNewDisplayMode();
        }

        // Changes the display mode
        // Enables/disables width/height controls based on display mode
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

        // Event triggers when confirm button is clicked
        // Resizes the MessageMap based on inputted width and height
        private void SizeConfirm_Click(object sender, RoutedEventArgs e)
        {
            ScrollPositionX = 0;
            ScrollPositionY = 0;
            ResizeMessage();
        }

        // Event triggers when clear button is clicked
        // Resets the MessageMap
        private void ClearMessage_Click(object sender, RoutedEventArgs e)
        {
            SetMessageMapSize(MessageMap.Count, MessageMap[0].Count, true);
        }

        // Parses textboxes for width and height and resizes the MessageMap
        // Adjusts the scroll bars as needed
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

        // Event triggers when horizontal scroll bar is moved
        // Adjusts scroll position and refreshes the UI button board
        private void ScrollBarX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScrollPositionX = Convert.ToInt32(ScrollBarX.Value);
            ResetLEDBoard();
        }

        // Event triggers when vertical scroll bar is moved
        // Adjusts scroll position and refreshed the UI button board
        private void ScrollBarY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScrollPositionY = Convert.ToInt32(ScrollBarY.Value);
            ResetLEDBoard();
        }

        // Sets the size of the MessageMap
        // If newMap = true, populates the MessageMap completely with zeros
        // Else, add zeros as necessary, preserving the original map
        // Size of the MessageMap cannot be smaller than the UI button board
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

        // Four events trigger when translation buttons are pressed
        // Translates the 1s on the MessageMap in the desired direction
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

        // Translates the 1s on the MessageMap in the desired direction
        // All points that go off the MessageMap are lost
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

        // Refreshes every button on the UI button board to the corresponding index on the MessageMap
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

        // Sets a position on the MessageMap to 1 or 0, also adjusting the corresponding button on the UI button board
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

        // Switches a position on the MessageMap, also adjusting the corresponding button on the UI button board
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

        // Given a button on the UI button board, find the corresponding coordinate point on the MessageMap
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

        // Changes the colour of a button on the UI button board based on active
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

        

        // Initialize hard coded font
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


            EnglishFont.Add("D0", Convert2DArrayToPointList(character0));
            EnglishFont.Add("D1", Convert2DArrayToPointList(character1));
            EnglishFont.Add("D2", Convert2DArrayToPointList(character2));
            EnglishFont.Add("D3", Convert2DArrayToPointList(character3));
            EnglishFont.Add("D4", Convert2DArrayToPointList(character4));
            EnglishFont.Add("D5", Convert2DArrayToPointList(character5));
            EnglishFont.Add("D6", Convert2DArrayToPointList(character6));
            EnglishFont.Add("D7", Convert2DArrayToPointList(character7));
            EnglishFont.Add("D8", Convert2DArrayToPointList(character8));
            EnglishFont.Add("D9", Convert2DArrayToPointList(character9));
        }

        // Hard coded maps for characters from A-Z and 0-9, also space

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

        int[,] character0 = new int[10, 7]{
            {0,1,1,1,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,1,1,0},
            {1,0,0,1,0,1,0},
            {1,0,1,0,0,1,0},
            {1,1,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {0,1,1,1,1,0,0}
        };

        int[,] character1 = new int[10, 7]{
            {0,0,1,0,0,0,0},
            {0,1,1,0,0,0,0},
            {1,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {1,1,1,1,1,0,0}
        };

        int[,] character2 = new int[10, 7]{
            {0,1,1,1,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,1,0,0},
            {0,0,0,1,0,0,0},
            {0,0,1,0,0,0,0},
            {0,1,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,1,1,1,1,1,0}
        };

        int[,] character3 = new int[10, 7]{
            {0,1,1,1,1,0,0},
            {1,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,1,0,0},
            {0,0,1,1,0,0,0},
            {0,0,0,0,1,0,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {1,0,0,0,1,0,0},
            {0,1,1,1,0,0,0}
        };

        int[,] character4 = new int[10, 7]{
            {0,0,0,1,1,0,0},
            {0,0,1,0,1,0,0},
            {0,0,1,0,1,0,0},
            {0,1,0,0,1,0,0},
            {0,1,0,0,1,0,0},
            {1,0,0,0,1,0,0},
            {1,1,1,1,1,1,0},
            {0,0,0,0,1,0,0},
            {0,0,0,0,1,0,0},
            {0,0,0,0,1,0,0}
        };

        int[,] character5 = new int[10, 7]{
            {1,1,1,1,1,1,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,1,1,1,1,0,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {0,1,1,1,1,0,0}
        };

        int[,] character6 = new int[10, 7]{
            {0,0,1,1,1,0,0},
            {0,1,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {1,0,1,1,1,0,0},
            {1,1,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {0,1,1,1,1,0,0}
        };

        int[,] character7 = new int[10, 7]{
            {1,1,1,1,1,1,0},
            {1,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,1,0,0},
            {0,0,0,0,1,0,0},
            {0,0,0,1,0,0,0},
            {0,0,0,1,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0},
            {0,0,1,0,0,0,0}
        };

        int[,] character8 = new int[10, 7]{
            {0,1,1,1,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {0,1,0,0,1,0,0},
            {0,0,1,1,0,0,0},
            {0,1,0,0,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {0,1,1,1,1,0,0}
        };

        int[,] character9 = new int[10, 7]{
            {0,1,1,1,1,0,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,0,1,0},
            {1,0,0,0,1,1,0},
            {0,1,1,1,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,0,1,0},
            {0,0,0,0,1,0,0},
            {0,1,1,1,0,0,0}
        };

    }

    // InputText struct used in hard coded font
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