/*
	TUIO C# Demo - part of the reacTIVision project
	Copyright (c) 2005-2016 Martin Kaltenbrunner <martin@tuio.org>

	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using TUIO;
using System.Linq;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.IO;
using System.Timers;
using System.Text.Json;
using System.Reflection.Emit;
using System.Text.Json.Serialization;


public class User
{
    public int StudentId { get; set; }
    public string Name { get; set; }

    public int Tscore { get; set; }

    public bool Attended { get; set; }

    public string Bluetooth { get; set; }

    public int Marker { get; set; }

    public bool IsStudent { get; set; }

    public string Emot { get; set; }

    // Constructor
    public User(int studentId, string name, string bluetooth, int marker, bool isStudent, string emot)
    {
        StudentId = studentId;
        Name = name;
        Attended = false;
        Tscore = 0;
        Bluetooth = bluetooth;
        Marker = marker;
        IsStudent = isStudent;
        Emot = emot;
    }
}

public class TuioDemo : Form, TuioListener
{
    private TuioClient client;
    private Dictionary<long, TuioObject> objectList;
    private Dictionary<long, TuioCursor> cursorList;
    private Dictionary<long, TuioBlob> blobList;

    private static System.Timers.Timer gestureTimer = new System.Timers.Timer(1000);

    public static int width, height;
    private int window_width = 640;
    private int window_height = 480;
    private int window_left = 0;
    private int window_top = 0;
    private int screen_width = Screen.PrimaryScreen.Bounds.Width;
    private int screen_height = Screen.PrimaryScreen.Bounds.Height;

    private bool fullscreen;
    private bool verbose;
    private static List<string> gesture = new List<string>();
    private static int screen = 6;
    private static int QuestionNumber = 0;
    private string responseMessage = "";
    private int score = 0;
    private Dictionary<string, int> studentScores = new Dictionary<string, int>();
    private int currentStudent = 0;
    private bool hasNavigated = false;
    private static string laserCoordinates = "";
    private static volatile int laser_X = 0;
    private static volatile int laser_Y = 0;

    private static List<Question> easyQuestions = new List<Question>();
    private static List<Question> mediumQuestions = new List<Question>();
    private static List<Question> hardQuestions = new List<Question>();
    private static int currentQuestionIndex = 0;

    public class Question
    {
        public string Text { get; set; }
        public List<string> Choices { get; set; }
        public List<string> ImagePaths { get; set; }
        public string CorrectAnswer { get; set; }
        public string Difficulty { get; set; }

        public Question(string text, List<string> choices, List<string> imagePaths, string correctAnswer, string difficulty)
        {
            Text = text;
            Choices = choices;
            ImagePaths = imagePaths;
            CorrectAnswer = correctAnswer;
            Difficulty = difficulty;
        }
    }


    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }
    private Difficulty difficultyLevel;
    private Difficulty currentDifficulty = Difficulty.Easy; // Set default to Easy, or as needed
    private static List<string> bluetoothDevices = new List<string>();
    private static List<User> users = new List<User>
    {
    new User(1, "Kamal Mohamed", "CC:F9:F0:CD:B9:DC",0, true, "") { Attended = false, Tscore = 0 },
    new User(2, "Jane Smith", "", 0, true, "") { Attended = true, Tscore = 5 },
    new User(3, "Alex Brown", "", 0, true, "") { Attended = false, Tscore = 0 }
    };
    private static int loggedInUser = 0;
    private static string currentObject = "";


    public TuioDemo(int port)
    {

        verbose = false;
        fullscreen = false;
        width = window_width;
        height = window_height;

        this.ClientSize = new System.Drawing.Size(width, height);
        this.Name = "TuioDemo";
        this.Text = "TuioDemo";

        this.Closing += new CancelEventHandler(Form_Closing);
        this.KeyDown += new KeyEventHandler(Form_KeyDown);

        this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                        ControlStyles.UserPaint |
                        ControlStyles.DoubleBuffer, true);

        objectList = new Dictionary<long, TuioObject>(128);
        cursorList = new Dictionary<long, TuioCursor>(128);
        blobList = new Dictionary<long, TuioBlob>(128);

        client = new TuioClient(port);
        client.addTuioListener(this);
        LoadQuestions();
        client.connect();
    }

    private void Form_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {

        if (e.KeyData == Keys.F1)
        {
            if (fullscreen == false)
            {

                width = screen_width;
                height = screen_height;

                window_left = this.Left;
                window_top = this.Top;

                this.FormBorderStyle = FormBorderStyle.None;
                this.Left = 0;
                this.Top = 0;
                this.Width = screen_width;
                this.Height = screen_height;

                fullscreen = true;
            }
            else
            {

                width = window_width;
                height = window_height;

                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Left = window_left;
                this.Top = window_top;
                this.Width = window_width;
                this.Height = window_height;

                fullscreen = false;
            }
        }
        else if (e.KeyData == Keys.Escape)
        {
            this.Close();

        }
        else if (e.KeyData == Keys.V)
        {
            verbose = !verbose;
        }

    }

    private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        client.removeTuioListener(this);

        client.disconnect();
        System.Environment.Exit(0);
    }

    public void addTuioObject(TuioObject o)
    {
        lock (objectList)
        {
            objectList.Add(o.SessionID, o);
            Debug.WriteLine(o.SymbolID);
        }
    }

    public void updateTuioObject(TuioObject o)
    {

        //if (verbose) Console.WriteLine("set obj " + o.SymbolID + " " + o.SessionID + " " + o.X + " " + o.Y + " " + o.Angle + " " + o.MotionSpeed + " " + o.RotationSpeed + " " + o.MotionAccel + " " + o.RotationAccel);
    }

    public void removeTuioObject(TuioObject o)
    {
        lock (objectList)
        {
            objectList.Remove(o.SessionID);
        }
    }

    public void addTuioCursor(TuioCursor c)
    {
        lock (cursorList)
        {
            cursorList.Add(c.SessionID, c);
            Debug.WriteLine(c.SessionID);
        }
        if (verbose) Console.WriteLine("add cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y);
    }

    public void updateTuioCursor(TuioCursor c)
    {
        if (verbose) Console.WriteLine("set cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y + " " + c.MotionSpeed + " " + c.MotionAccel);
    }

    public void removeTuioCursor(TuioCursor c)
    {
        lock (cursorList)
        {
            cursorList.Remove(c.SessionID);
        }
        if (verbose) Console.WriteLine("del cur " + c.CursorID + " (" + c.SessionID + ")");
    }

    public void addTuioBlob(TuioBlob b)
    {
        lock (blobList)
        {
            blobList.Add(b.SessionID, b);
        }
        if (verbose) Console.WriteLine("add blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area);
    }

    public void updateTuioBlob(TuioBlob b)
    {

        if (verbose) Console.WriteLine("set blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area + " " + b.MotionSpeed + " " + b.RotationSpeed + " " + b.MotionAccel + " " + b.RotationAccel);
    }

    public void removeTuioBlob(TuioBlob b)
    {
        lock (blobList)
        {
            blobList.Remove(b.SessionID);
        }
        if (verbose) Console.WriteLine("del blb " + b.BlobID + " (" + b.SessionID + ")");
    }

    public void refresh(TuioTime frameTime)
    {
        Invalidate();
    }

    private void DrawArrow(Graphics g, float x, float y, float angle, float size)
    {
        // Calculate the arrow points based on the angle
        float arrowLength = size;
        float arrowHeadLength = size / 5;
        float arrowHeadWidth = size / 8;

        // Calculate arrow base points
        float x1 = x + arrowLength * (float)Math.Cos(angle);
        float y1 = y + arrowLength * (float)Math.Sin(angle);

        // Calculate arrowhead points
        float x2 = x1 - arrowHeadLength * (float)Math.Cos(angle - Math.PI / 6);
        float y2 = y1 - arrowHeadLength * (float)Math.Sin(angle - Math.PI / 6);
        float x3 = x1 - arrowHeadLength * (float)Math.Cos(angle + Math.PI / 6);
        float y3 = y1 - arrowHeadLength * (float)Math.Sin(angle + Math.PI / 6);

        Pen thickPen = new Pen(Color.Black, 5);
        // Draw the arrow line
        g.DrawLine(thickPen, x, y, x1, y1);

        // Draw the arrowhead
        g.DrawLine(thickPen, x1, y1, x2, y2);
        g.DrawLine(thickPen, x1, y1, x3, y3);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {

        Graphics g = pevent.Graphics;
        //g.FillRectangle(bgrBrush, new Rectangle(0, 0, width, height));
        g.DrawImage(Image.FromFile("home.jpg"), 0, 0, width, height);

        SolidBrush c1Brush = new SolidBrush(Color.Red);
        SolidBrush c2Brush = new SolidBrush(Color.Green);
        SolidBrush c3Brush = new SolidBrush(Color.Blue);
        SolidBrush c4Brush = new SolidBrush(Color.Olive);

        //check if bluetooh list is populated
        //for (int i = 0; i < bluetoothDevices.Count; i++)
        //{
        //    if (bluetoothDevices[i] == "CC:F9:F0:CD:B9:DC")
        //        screen = 3;
        //}

        //checkCollisonTrue();
        exitRun();
        switch (screen)
        {
            case 1:
                changeQuestionBackground(pevent, g, c1Brush, c2Brush, c3Brush, c4Brush);
                checkCollisonTrue();
                break;
            case 2:
                drawScreenTwo(g);
                break;
            case 3:
                drawScreenThree(g);
                break;
            case 4:
                drawScreenFour(pevent, g, c1Brush, c2Brush, c3Brush, c4Brush);
                break;
            case 5:
                drawWelcomeScreen(g, pevent);
                if (loggedInUser != 0)
                    checkLogin(loggedInUser);
                //studentRegister();
                break;
            case 6:
                drawLaserScreen(pevent);

                break;
        }

    }

    private void drawLaserScreen(PaintEventArgs pevent)
    {
        Graphics g = pevent.Graphics;

        // Set a light, child-friendly background color
        g.Clear(Color.LightCyan);

        // Load the images for the boxes and laser
        Image boxImage1 = Image.FromFile("home2.jpg");  // Replace with the path to your first box image
        Image boxImage2 = Image.FromFile("farm.jpg");  // Replace with the path to your second box image
        Image laserImage = Image.FromFile("chicken.png");  // Replace with the path to your laser image

        // Define padding and border width
        int padding = 30;
        int borderWidth = 6;

        // Define the box sizes (you can adjust these based on your images)
        int boxWidth = 280;
        int boxHeight = 250;

        // Define positions for the two boxes with padding and borders
        Rectangle box1Rect = new Rectangle(padding, padding + 60, boxWidth, boxHeight);  // Added space for text above box
        Rectangle box2Rect = new Rectangle(this.ClientSize.Width - boxWidth - padding, padding + 60, boxWidth, boxHeight);  // Added space for text above box

        // Draw the borders around the boxes with soft contrasting colors
        using (Pen borderPen1 = new Pen(Color.Pink, borderWidth))
        using (Pen borderPen2 = new Pen(Color.Yellow, borderWidth))
        {
            g.DrawRectangle(borderPen1, box1Rect); // Pink border for the left box
            g.DrawRectangle(borderPen2, box2Rect); // Yellow border for the right box
        }

        // Draw the images inside the boxes with padding
        g.DrawImage(boxImage1, box1Rect.X + borderWidth, box1Rect.Y + borderWidth, box1Rect.Width - 2 * borderWidth, box1Rect.Height - 2 * borderWidth);
        g.DrawImage(boxImage2, box2Rect.X + borderWidth, box2Rect.Y + borderWidth, box2Rect.Width - 2 * borderWidth, box2Rect.Height - 2 * borderWidth);

        // Set up the font and color for the text (larger, centered, and child-friendly)
        using (Font font = new Font("Comic Sans MS", 24, FontStyle.Bold))
        using (Brush textBrush1 = new SolidBrush(Color.HotPink))  // Text color for "Home"
        using (Brush textBrush2 = new SolidBrush(Color.Orange))   // Text color for "Farm"
        {
            // Draw "Home" above the left box (centered)
            string homeText = "Home";
            SizeF homeTextSize = g.MeasureString(homeText, font);
            float homeTextX = (box1Rect.X + box1Rect.Width / 2) - homeTextSize.Width / 2;
            g.DrawString(homeText, font, textBrush1, homeTextX, padding);

            // Draw "Farm" above the right box (centered)
            string farmText = "Farm";
            SizeF farmTextSize = g.MeasureString(farmText, font);
            float farmTextX = (box2Rect.X + box2Rect.Width / 2) - farmTextSize.Width / 2;
            g.DrawString(farmText, font, textBrush2, farmTextX, padding);
        }

        // Define the position and size for the laser image
        int laserRadius = 70;  // Optional if you want to define a size for the laser image
                               // Draw the laser image at (laser_X, laser_Y)
        g.DrawImage(laserImage, laser_X - laserRadius / 2, laser_Y - laserRadius / 2, laserRadius, laserRadius);

        // Draw the coordinates on the screen for debugging
        //using (Font font = new Font("Arial", 12))
        //{
        //    g.DrawString($"Laser X: {laser_X} Y: {laser_Y}", font, Brushes.White, 10, 300);
        //}

        // Invalidate to trigger a repaint (optional)
        this.Invalidate();
    }




    private void drawWelcomeBackground(Graphics g, PaintEventArgs pevent, string welcomeText)
    {
        int width = this.ClientSize.Width;
        int height = this.ClientSize.Height;

        using (LinearGradientBrush bgBrush = new LinearGradientBrush(new Rectangle(0, 0, width, height), Color.LightSkyBlue, Color.MediumPurple, LinearGradientMode.Vertical))
        {
            g.FillRectangle(bgBrush, new Rectangle(0, 0, width, height));
        }

        // Draw fun background shapes
        Random random = new Random();
        for (int i = 0; i < 10; i++)
        {
            int shapeSize = random.Next(30, 60);
            int posX = random.Next(0, width);
            int posY = random.Next(0, height);
            using (Brush shapeBrush = new SolidBrush(Color.FromArgb(50, Color.Yellow)))
            {
                g.FillEllipse(shapeBrush, posX, posY, shapeSize, shapeSize);
            }
        }


        float fontSize = Math.Max(18, height / 20);
        Font font = new Font("Comic Sans MS", fontSize, FontStyle.Bold);
        Brush textBrush = new SolidBrush(Color.Yellow);


        SizeF textSize = g.MeasureString(welcomeText, font);
        float x = (width - textSize.Width) / 2;
        float y = height / 2;

        RectangleF textRect = new RectangleF(x - 6, y - 10, textSize.Width + 10, textSize.Height + 20);
        using (GraphicsPath path = new GraphicsPath())
        {
            float cornerRadius = Math.Min(textRect.Width, textRect.Height) / 5;
            path.AddArc(textRect.X, textRect.Y, cornerRadius, cornerRadius, 180, 90);
            path.AddArc(textRect.Right - cornerRadius, textRect.Y, cornerRadius, cornerRadius, 270, 90);
            path.AddArc(textRect.Right - cornerRadius, textRect.Bottom - cornerRadius, cornerRadius, cornerRadius, 0, 90);
            path.AddArc(textRect.X, textRect.Bottom - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            path.CloseFigure();

            using (Brush bubbleBrush = new SolidBrush(Color.FromArgb(180, Color.DarkBlue)))
            {
                g.FillPath(bubbleBrush, path);
            }
        }

        g.DrawString(welcomeText, font, textBrush, x, y);

        font.Dispose();
        textBrush.Dispose();

    }

    private void drawWelcomeScreen(Graphics g, PaintEventArgs pevent)
    {
        drawWelcomeBackground(g, pevent, "Student facial recognition login");
    }

    private void drawLearningScreen(Graphics g, PaintEventArgs pevent)
    {

    }
    private void drawScreenTwo(Graphics g)
    {
        // Display the response after TUIO interaction
        if (responseMessage == "Ashter katkout")
        {
            drawWelcomeBackground(g, null, "Correct Answer!");
            checkNavigation();
        }
        else if (responseMessage == "Try again")
        {
            drawWelcomeBackground(g, null, "Incorrect Answer!");
            checkCollisonTrue();
        }
    }
    private void drawScreenThree(Graphics g)
    {
        g.Clear(Color.LightBlue);

        g.DrawString("Hello, Teacher!", new Font("Arial", 24, FontStyle.Bold), Brushes.Black, new PointF(width / 2 - 100, 50));

        g.FillRectangle(Brushes.DarkBlue, new Rectangle((int)(width / 2 - 250), 100, 550, 40));
        g.DrawString("Name", new Font("Arial", 16, FontStyle.Bold), Brushes.White, new PointF(width / 2 - 240, 110));
        g.DrawString("Score", new Font("Arial", 16, FontStyle.Bold), Brushes.White, new PointF(width / 2 - 70, 110));
        g.DrawString("Emotion", new Font("Arial", 16, FontStyle.Bold), Brushes.White, new PointF(width / 2 + 60, 110));
        g.DrawString("Attended", new Font("Arial", 16, FontStyle.Bold), Brushes.White, new PointF(width / 2 + 180, 110));

        float startX = width / 2 - 250;
        float startY = 150;
        float rowHeight = 30;
        float rowWidth = 500;

        foreach (var student in users)
        {
            g.FillRectangle(Brushes.White, new Rectangle((int)startX, (int)startY, (int)rowWidth, (int)rowHeight));
            g.DrawRectangle(Pens.Black, new Rectangle((int)startX, (int)startY, (int)rowWidth, (int)rowHeight));

            g.DrawString(student.Name, new Font("Arial", 14), Brushes.Black, new PointF(startX + 10, startY + 5));
            g.DrawString(student.Tscore.ToString(), new Font("Arial", 14), Brushes.Black, new PointF(startX + 180, startY + 5));
            //g.DrawString(student.Bluetooth, new Font("Arial", 8), Brushes.Black, new PointF(startX + 300, startY + 5));
            g.DrawString(student.Emot, new Font("Arial", 14), Brushes.Black, new PointF(startX + 300, startY + 5));
            g.DrawString(student.Attended ? "Yes" : "No", new Font("Arial", 14), Brushes.Black, new PointF(startX + 420, startY + 5));

            startY += rowHeight;

        }

        checkCollisonTrue();
    }

    private string currentMenu = "Main"; // Tracks the current menu
    private string selectedCategory = ""; // Tracks the selected category
    private string selectedSubmenu = "";

    private Dictionary<string, List<string>> subMenus = new Dictionary<string, List<string>>
    {
        { "Math", new List<string> { "Algebra", "Geometry", "Calculus" } },
        { "Science", new List<string> { "Physics", "Chemistry", "Biology" } },
        { "History", new List<string> { "Ancient", "Medieval", "Modern" } },
        { "Art", new List<string> { "Painting", "Sculpture", "Music" } },
        { "Psychology", new List<string> { "Behavioral", "Cognitive", "Developmental" } },
        { "Computer Science", new List<string> { "Programming", "Data Structures", "AI" } }
    };
    private Dictionary<string, List<string>> learningTopics = new Dictionary<string, List<string>>
    {
        { "Algebra", new List<string> { "Linear Equations", "Polynomials", "Quadratic Functions" } },
        { "Geometry", new List<string> { "Euclidean Geometry", "Circles and Angles", "Coordinate Geometry" } },
        { "Calculus", new List<string> { "Limits", "Derivatives", "Integrals" } },

        { "Biology", new List<string> { "Cell Biology", "Genetics", "Ecology", "Evolution" } },
        { "Physics", new List<string> { "Newton's Laws", "Thermodynamics", "Quantum Mechanics" } },
        { "Chemistry", new List<string> { "Atomic Structure", "Chemical Reactions", "Organic Chemistry" } },

        { "Ancient", new List<string> { "Ancient Egypt", "Ancient Greece", "Ancient Rome" } },
        { "Medieval", new List<string> { "Feudalism", "The Crusades", "The Black Plague" } },
        { "Modern", new List<string> { "Industrial Revolution", "World Wars", "Cold War" } },

        { "Painting", new List<string> { "Impressionism", "Cubism", "Surrealism" } },
        { "Sculpture", new List<string> { "Classical Sculpture", "Modern Sculpture", "Sculpture Materials" } },
        { "Music", new List<string> { "Classical Music", "Jazz", "Electronic Music" } },

        { "Behavioral", new List<string> { "Conditioning", "Motivation", "Social Behavior" } },
        { "Cognitive", new List<string> { "Memory", "Perception", "Decision Making" } },
        { "Developmental", new List<string> { "Child Development", "Adolescence", "Aging and Cognitive Decline" } },

        { "Programming", new List<string> { "Introduction to Programming", "Object-Oriented Programming", "Error Handling" } },
        { "Data Structures", new List<string> { "Arrays", "Linked Lists", "Stacks and Queues" } },
        { "AI", new List<string> { "Machine Learning", "Natural Language Processing", "Computer Vision" } },


    };


    private string selectedTopic = ""; // The specific learning topic
    private string selectedDetail = ""; // Selected detail about the topic
    private Dictionary<string, (string ImagePath, List<string> Details)> topicDetails = new Dictionary<string, (string, List<string>)>
    {
        { "Chicken", ("chicken.jpg", new List<string> { "Habitat", "Weight", "Diet", "Lifespan" }) },
        { "Elephant", ("elephant.jpg", new List<string> { "Habitat", "Weight", "Diet", "Lifespan" }) },
        { "Penguin", ("penguin.jpg", new List<string> { "Habitat", "Weight", "Diet", "Lifespan" }) }
    };

    private Dictionary<string, List<string>> subMenusKids = new Dictionary<string, List<string>>
    {
        { "Math", new List<string> { "Counting", "Shapes", "Basic Addition and Subtraction" } },
        { "Science", new List<string> { "Animals", "Plants", "The Weather" } },
        { "History", new List<string> { "Dinosaurs", "Ancient Egypt", "Famous People" } },
        { "Art", new List<string> { "Drawing", "Coloring", "Making Crafts" } },
        { "Psychology", new List<string> { "Feelings", "Friendship", "Sharing" } },
    };

    private Dictionary<string, List<string>> learningTopicsKids = new Dictionary<string, List<string>>
    {
        { "Counting", new List<string> { "1 to 10", "Skip Counting", "Counting by 5s and 10s" } },
        { "Shapes", new List<string> { "Circles", "Squares", "Triangles" } },
        { "Basic Addition and Subtraction", new List<string> { "Adding Small Numbers", "Subtracting Small Numbers", "Math with Objects" } },//hand gestures

        { "Animals", new List<string> { "chicken", "elephant", "penguin" } },
        { "Plants", new List<string> { "flower", "tree", "grass" } },
        { "The Weather", new List<string> { "Sunny Days", "Rain and Snow", "Clouds and Storms" } },

        { "Dinosaurs", new List<string> { "T-Rex", "Triceratops", "" } },
        { "Ancient Egypt", new List<string> { "Pyramids of Giza", "Temple of Karnak", "The Sphinx" } },
        { "Famous People", new List<string> { "Albert Einstein", "Marie Curie", "Neil Armstrong" } },

        { "Drawing", new List<string> { "How to Draw Animals", "Drawing Your Family", "Drawing with Colors" } },//bad
        { "Coloring", new List<string> { "red", "yellow", "green" } },
        { "Making Crafts", new List<string> { "Paper Crafts", "Clay Models", "Simple DIY Projects" } },//bad

        { "Feelings", new List<string> { "Happy", "Sad", "frightened" } },
        { "Friendship", new List<string> { "friend", "college", "bestfriend" } },
        { "Sharing", new List<string> { "family", "friends", "strangers" } },

    };

    private Dictionary<string, (string ImagePath, List<string> Details)> topicDetailsKids = new Dictionary<string, (string, List<string>)>
    {
        { "Chicken", ("chicken.jpg", new List<string> { "Habitat: Farm", "Weight: 2-3 kg", "Diet: Seeds, grains, insects" }) },
        { "Elephant", ("elephant.jpg", new List<string> { "Habitat: nature", "Weight: 6000-7000 kg", "Diet: Grass, fruits, bark" }) },
        { "Penguin", ("penguin.jpg", new List<string> { "Habitat: nature", "Weight: 1-40 kg", "Diet: Fish, krill" }) },

        { "Circles", ("Circles.jpg", new List<string> { "number of angles: 0" }) },
        { "Squares", ("Squares.jpg", new List<string> { "number of angles: 4" }) },
        { "Triangle", ("Triangle.jpg", new List<string> { "number of angles: 3" }) },

        { "sunny day", ("sunny.jpg", new List<string> { "sound: Birds chirping", "season: Summer", "what to do: Play outside, wear sunglasses" }) },
        { "rain and snow", ("rain.jpg", new List<string> { "sound: Raindrops, snowflakes", "season: Winter, Spring", "what to do: Stay inside, play in the snow" }) },
        { "clouds and storms", ("storm.jpg", new List<string> { "sound: Thunder, rain", "season: Summer", "what to do: Stay indoors, watch the storm" }) },

        { "flower", ("flower.jpg", new List<string> { "action: Blooms in spring", "size: Varies", "color: Red, yellow, pink, etc." }) },
        { "tree", ("tree.jpg", new List<string> { "action: Provides shade, oxygen", "size: Varies, can grow very tall", "color: Green leaves, brown trunk" }) },
        { "grass", ("grass.jpg", new List<string> { "action: Grows in fields", "size: Short, up to a foot tall", "color: Green" }) },

        { "T-Rex", ("T_Rex.jpg", new List<string> { "Habitat: Forests, plains", "Weight: 7-9 tons", "Diet: Carnivore", "Lifespan: 30 years" }) },
        { "Triceratops", ("Triceratops.jpg", new List<string> { "Habitat: Forests", "Weight: 6-12 tons", "Diet: Herbivore", "Lifespan: 30-40 years" }) },
        { "Velociraptor", ("Velociraptor.jpg", new List<string> { "Habitat: Deserts, forests", "Weight: 15-30 kg", "Diet: Carnivore", "Lifespan: 10-15 years" }) },

        { "Pyramids of Giza", ("pyramids_giza.jpg", new List<string> { "Date Built: 2580-2560 BC", "Size: 481 feet tall", "King: Pharaoh Khufu" }) },
        { "Temple of Karnak", ("temple_karnak.jpg", new List<string> { "Date Built: 2000 BC", "Size: 100 feet tall", "King: Pharaoh Ramses II" }) },
        { "The Sphinx", ("sphinx.jpg", new List<string> { "Date Built: 2500 BC", "Size: 66 feet tall", "King: Pharaoh Khafre" }) },

        { "Albert Einstein", ("Albert.jpg", new List<string> { "birthyear: 1879", "gender: Male", "story: Developed theory of relativity", "country: Germany" }) },
        { "Marie Curie", ("Marie.jpg", new List<string> { "birthyear: 1867", "gender: Female", "story: Pioneered research on radioactivity", "country: Poland" }) },
        { "Neil Armstrong", ("Neil.jpg", new List<string> { "birthyear: 1930", "gender: Male", "story: First person to walk on the moon", "country: USA" }) },

        { "red", ("red.jpg", new List<string> { "fruit: Apples, strawberries", "flower: Roses, poppies", "object: Firetrucks, stop signs" }) },
        { "yellow", ("yellow.jpg", new List<string> { "fruit: Bananas, lemons", "flower: Sunflowers, daisies", "object: School buses, sun" }) },
        { "green", ("green.jpg", new List<string> { "fruit: Apples, grapes", "flower: Tulips, lilies", "object: Grass, leaves" }) },

        { "Happy", ("happy.jpg", new List<string> { "why: When you achieve something or feel loved", "what to do: Smile, play, enjoy", "what makes you happy: Friends, good weather" }) },
        { "Sad", ("sad.jpg", new List<string> { "why: When something disappointing happens", "what to do: Talk to someone, relax", "what makes you feel better: Music, time with friends" }) },
        { "frightened", ("frightened.jpg", new List<string> { "why: When you are scared or worried", "what to do: Stay close to a trusted person, breathe", "how to feel safe: In a well-lit room, with friends" }) },

        { "friend", ("friend.jpg", new List<string> { "Habitat: Anywhere", "Weight: Varies", "Diet: Varies", "Lifespan: Varies" }) },
        { "college", ("college.jpg", new List<string> { "Habitat: School", "Weight: N/A", "Diet: N/A", "Lifespan: 4 years on average" }) },
        { "bestfriend", ("bestfriend.jpg", new List<string> { "Habitat: Anywhere", "Weight: Varies", "Diet: Varies", "Lifespan: Varies" }) },

        { "family", ("family.jpg", new List<string> { "secrets: Share and protect each other", "friendly: Love and support", "ignore: Ignore conflicts, not each other" }) },
        { "friends", ("friends.jpg", new List<string> { "secrets: Trust and share", "friendly: Support and have fun", "ignore: Ignore disagreements" }) },
        { "strangers", ("strangers.jpg", new List<string> { "secrets: No secrets yet", "friendly: Be polite", "ignore: Don't ignore, be cautious" }) },
    };


    private bool canNavigate = true;
    private DateTime lastNavigationTime;
    private TimeSpan navigationCooldown = TimeSpan.FromSeconds(1);
    private void DrawPieMenu(Graphics g, List<string> options, int x, int y, int radius, double angleThreshold = 0.25)
    {
        int numOptions = options.Count;
        float anglePerOption = 360f / numOptions;
        double distanceThreshold = 0.25;
        string selectedOptionText = "None";
        var marker4 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 4);
        var marker5 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 5);
        var marker1 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 0);

        for (int i = 0; i < numOptions; i++)
        {
            float startAngle = i * anglePerOption;
            float sweepAngle = anglePerOption;

            Brush sliceBrush = Brushes.LightGray;
            Pen outlinePen = Pens.Black;


            if (marker1 != null)
            {
                float normalizedAngle = (float)((marker1.Angle * 180 / Math.PI) % 360);
                if (normalizedAngle >= startAngle && normalizedAngle < startAngle + sweepAngle)
                {
                    sliceBrush = Brushes.LightBlue;
                    selectedOptionText = options[i];
                }
            }


            g.FillPie(sliceBrush, x - radius, y - radius, radius * 2, radius * 2, startAngle, sweepAngle);
            g.DrawPie(outlinePen, x - radius, y - radius, radius * 2, radius * 2, startAngle, sweepAngle);


            float textAngle = (startAngle + sweepAngle / 2) * (float)(Math.PI / 180);
            float textX = x + (float)(radius * 0.75 * Math.Cos(textAngle));
            float textY = y + (float)(radius * 0.75 * Math.Sin(textAngle));
            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(options[i], new Font("Arial", 10), Brushes.Black, new PointF(textX, textY), sf);
        }
        if (marker4 != null && marker5 != null)
        {
            screen = 1;
        }
        if (marker1 != null)
        {
            float markerScreenX = x + (float)(radius * Math.Cos(marker1.Angle));
            float markerScreenY = y + (float)(radius * Math.Sin(marker1.Angle));
            g.FillEllipse(Brushes.Red, markerScreenX - 5, markerScreenY - 5, 10, 10);

            if (marker4 != null)
            {
                double distanceToMarker4 = Math.Sqrt(Math.Pow(marker1.X - marker4.X, 2) + Math.Pow(marker1.Y - marker4.Y, 2));

                if (distanceToMarker4 <= distanceThreshold)
                {
                    g.DrawString($"Selected: {selectedOptionText}", new Font("Arial", 12), Brushes.Black, new PointF(x - 50, y + radius + 20));

                    if (canNavigate)
                    {
                        HandleNavigation(selectedOptionText); // Handle Next navigation
                        canNavigate = false;
                        lastNavigationTime = DateTime.Now;
                    }
                }
            }

            if (marker5 != null)
            {
                double distanceToMarker5 = Math.Sqrt(Math.Pow(marker1.X - marker5.X, 2) + Math.Pow(marker1.Y - marker5.Y, 2));

                if (distanceToMarker5 <= distanceThreshold)
                {
                    g.DrawString($"Selected: Previous", new Font("Arial", 12), Brushes.Black, new PointF(x - 50, y + radius + 20));

                    if (canNavigate)
                    {
                        HandleNavigation("Previous"); // Handle Previous navigation
                        canNavigate = false;
                        lastNavigationTime = DateTime.Now;
                    }
                }
            }

            if ((marker4 == null || Math.Sqrt(Math.Pow(marker1.X - marker4.X, 2) + Math.Pow(marker1.Y - marker4.Y, 2)) > distanceThreshold) &&
                (marker5 == null || Math.Sqrt(Math.Pow(marker1.X - marker5.X, 2) + Math.Pow(marker1.Y - marker5.Y, 2)) > distanceThreshold))
            {
                g.DrawString($"No valid selection", new Font("Arial", 12), Brushes.Black, new PointF(x - 50, y + radius + 20));
            }
        }
        // Reset cooldown if elapsed time exceeds the cooldown duration
        if (!canNavigate && (DateTime.Now - lastNavigationTime) >= navigationCooldown)
        {
            canNavigate = true;
        }
    }


    private void DrawLearningMenu(PaintEventArgs pevent)
    {
        Graphics g = pevent.Graphics;

        List<string> options;
        if (currentMenu == "Main")
        {
            options = new List<string> { "Math", "Science", "History", "Art", "Psychology", "Computer Science" };
        }
        else if (currentMenu == "Submenu" && subMenus.ContainsKey(selectedCategory))
        {
            options = subMenus[selectedCategory];
        }
        else if (currentMenu == "Learning" && learningTopics.ContainsKey(selectedSubmenu))
        {
            options = learningTopics[selectedSubmenu];
        }
        else if (currentMenu == "TopicDetails")
        {
            DrawTopicDetails(g);
            return;
        }
        else
        {
            options = new List<string>();
        }
        if(currentObject == "bottle")
        {
            currentMenu = "TopicDetails";
            selectedTopic = "Chicken";
        }
        DrawPieMenu(g, options, 200, 200, 150);
    }

    private void DrawTopicDetails(Graphics g)
    {
        if (topicDetails.ContainsKey(selectedTopic))
        {
            var (imagePath, details) = topicDetails[selectedTopic];

            // Draw the background image based on the selected topic
            g.DrawImage(Image.FromFile(imagePath), 0, 0, 400, 400);

            // Draw pie menu for topic details
            DrawPieMenu(g, details, 200, 350, 100);
        }

        // Display the selected detail
        if (!string.IsNullOrEmpty(selectedDetail))
        {
            g.DrawString($"Detail: {selectedDetail}", new Font("Arial", 12), Brushes.Black, new PointF(50, 420));
        }
    }
    private Stack<string> navigationHistory = new Stack<string>();

    private void HandleNavigation(string selectedOptionText)
    {
        if (currentMenu == "Learning" && selectedOptionText == "Chicken")
        {
            navigationHistory.Push(currentMenu); // Save the current menu
            selectedTopic = selectedOptionText;
            currentMenu = "TopicDetails"; // Transition to topic details
        }
        else if (currentMenu == "TopicDetails" && topicDetails.ContainsKey(selectedTopic) && topicDetails[selectedTopic].Details.Contains(selectedOptionText))
        {

            selectedDetail = selectedOptionText; // Update the detail being displayed
        }
        else if (currentMenu == "Main" && subMenus.ContainsKey(selectedOptionText))
        {
            navigationHistory.Push(currentMenu);
            selectedCategory = selectedOptionText;
            currentMenu = "Submenu";
        }
        else if (currentMenu == "Submenu" && learningTopics.ContainsKey(selectedOptionText))
        {
            navigationHistory.Push(currentMenu);
            selectedSubmenu = selectedOptionText;
            currentMenu = "Learning";
        }
        else if (selectedOptionText == "Previous") // Handle navigation backward
        {
            if (navigationHistory.Count > 0)
            {
                currentMenu = navigationHistory.Pop();
            }
        }
    }


    private void drawScreenFour(PaintEventArgs pevent,
    Graphics g,
    SolidBrush brush1,
    SolidBrush brush2,
    SolidBrush brush3,
    SolidBrush brush4)
    {
        DrawLearningMenu(pevent);
    }

    private void addScore()
    {
        users[currentStudent].Tscore += 5;
    }
    private void drawScore(Graphics g)
    {
        int scoreIndicatorSize = 80;
        int scoreIndicatorX = width - scoreIndicatorSize - 20;
        int scoreIndicatorY = 20;

        using (Pen scorePen = new Pen(Color.FromArgb(100, 150, 255), 8))
        {
            g.DrawEllipse(Pens.Gray, scoreIndicatorX, scoreIndicatorY, scoreIndicatorSize, scoreIndicatorSize);

            float sweepAngle = (users[currentStudent].Tscore / 10) * 360;
            g.DrawArc(scorePen, scoreIndicatorX, scoreIndicatorY, scoreIndicatorSize, scoreIndicatorSize, -90, sweepAngle);
        }

        var scoreFont = new Font("Arial", 14, FontStyle.Bold);
        string scoreText = $"{users[currentStudent].Tscore}%";
        var scoreTextSize = g.MeasureString(scoreText, scoreFont);
        float scoreTextX = scoreIndicatorX + (scoreIndicatorSize - scoreTextSize.Width) / 2;
        float scoreTextY = scoreIndicatorY + (scoreIndicatorSize - scoreTextSize.Height) / 2;

        g.DrawString(scoreText, scoreFont, Brushes.Black, new PointF(scoreTextX, scoreTextY));
    }
    // Method to get the current question based on the difficulty
    private Question GetCurrentQuestion()
    {
        List<Question> selectedQuestions = difficultyLevel == Difficulty.Easy ? easyQuestions :
                                            difficultyLevel == Difficulty.Medium ? mediumQuestions : hardQuestions;
        return selectedQuestions[currentQuestionIndex];
    }
    private List<Question> GetCurrentDifficulty()
    {
        List<Question> selectedQuestions = difficultyLevel == Difficulty.Easy ? easyQuestions :
                                            difficultyLevel == Difficulty.Medium ? mediumQuestions : hardQuestions;
        return selectedQuestions;
    }

    private void LoadQuestions()
    {
        string jsonPath = "questions.json";
        if (File.Exists(jsonPath))
        {
            try
            {
                string jsonData = File.ReadAllText(jsonPath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                List<Question> allQuestions = JsonSerializer.Deserialize<List<Question>>(jsonData, options);

                if (allQuestions != null)
                {
                    Debug.WriteLine($"Loaded {allQuestions.Count} questions.");

                    // Separate questions by difficulty
                    foreach (var question in allQuestions)
                    {
                        string difficulty = question.Difficulty?.ToLower() ?? "unknown";

                        switch (difficulty)
                        {
                            case "easy":
                                easyQuestions.Add(question);
                                break;
                            case "medium":
                                mediumQuestions.Add(question);
                                break;
                            case "hard":
                                hardQuestions.Add(question);
                                break;
                            default:
                                Debug.WriteLine($"Unknown difficulty for question: {question.Text}");
                                break;
                        }
                    }

                    // Debug output for confirmation
                    Debug.WriteLine($"Easy Questions: {easyQuestions.Count}");
                    Debug.WriteLine($"Medium Questions: {mediumQuestions.Count}");
                    Debug.WriteLine($"Hard Questions: {hardQuestions.Count}");
                }
                else
                {
                    Debug.WriteLine("No questions loaded. JSON data might be empty.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading questions: {ex.Message}");
            }
        }
        else
        {
            throw new FileNotFoundException("Questions file not found!");
        }
    }
    private void changeQuestionBackground(PaintEventArgs pevent,
        Graphics g,
        SolidBrush c1Brush,
        SolidBrush c2Brush,
        SolidBrush c3Brush,
        SolidBrush c4Brush)
    {

        Brush[] quadrantBrushes = { c1Brush, c2Brush, c3Brush, c4Brush };

        int midWidth = width / 2;
        int midHeight = height / 2;

        Question question1 = GetCurrentQuestion();
        int ii = 0;
        for (int i = 0; i < 4; i++)
        {
            int x = (i % 2 == 0) ? 0 : midWidth;
            int y = ((i % 4) < 2) ? 0 : midHeight;

            g.FillRectangle(quadrantBrushes[ii], x, y, midWidth, midHeight);
            ii++;

            var cityFont = new Font("Arial", 24, FontStyle.Bold);
            var textSize = g.MeasureString(question1.Choices[i], cityFont);
            float textX = x + (midWidth - textSize.Width) / 2;
            float textY = y + (midHeight - textSize.Height) / 2;
            g.DrawString(question1.Choices[i], cityFont, Brushes.White, new PointF(textX, textY));
        }


        int questionBoxWidth = 400;
        int questionBoxHeight = 100;
        int boxX = (width - questionBoxWidth) / 2;
        int boxY = (height - questionBoxHeight) / 2;


        var marker1 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == users[currentStudent].Marker);

        if (marker1 != null)
        {
            int x = 0;
            int y = 0;

            int i = 0;
            if (marker1.Angle >= 4.7 && marker1.Angle <= 6.5)
            {
                x = (1 % 2 == 0) ? 0 : midWidth;
                y = (1 < 2) ? 0 : midHeight;
                g.DrawImage(Image.FromFile(question1.ImagePaths[i]), x, y, midWidth, midHeight);
            }


            if (marker1.Angle >= 3.2 && marker1.Angle <= 4.687)
            {
                x = (0 % 2 == 0) ? 0 : midWidth;
                y = (0 < 2) ? 0 : midHeight;
                g.DrawImage(Image.FromFile(question1.ImagePaths[i + 1]), x, y, midWidth, midHeight);
            }


            if (marker1.Angle >= 1.6 && marker1.Angle <= 3.1)
            {
                x = (2 % 2 == 0) ? 0 : midWidth;
                y = (2 < 2) ? 0 : midHeight;
                g.DrawImage(Image.FromFile(question1.ImagePaths[i + 2]), x, y, midWidth, midHeight);
            }


            if (marker1.Angle >= 0 && marker1.Angle <= 1.5)
            {
                x = (3 % 2 == 0) ? 0 : midWidth;
                y = (3 < 2) ? 0 : midHeight;

                g.DrawImage(Image.FromFile(question1.ImagePaths[i + 3]), x, y, midWidth, midHeight);
            }

        }

        if (marker1 != null)
        {
            float ox = marker1.getScreenX(width);
            float oy = marker1.getScreenY(height);
            float angle = marker1.Angle;

            DrawArrow(pevent.Graphics, window_width / 2, window_height / 2, angle, 250);
        }

        g.FillRectangle(Brushes.White, boxX, boxY, questionBoxWidth, questionBoxHeight);
        g.DrawRectangle(Pens.Black, boxX, boxY, questionBoxWidth, questionBoxHeight);




        var questionFont = new Font("Arial", 18, FontStyle.Bold);
        var questionTextSize = g.MeasureString(question1.Text, questionFont);
        float questionTextX = boxX + (questionBoxWidth - questionTextSize.Width) / 2;
        float questionTextY = boxY + (questionBoxHeight - questionTextSize.Height) / 2;
        g.DrawString(question1.Text, questionFont, Brushes.Black, new PointF(questionTextX, questionTextY));

        drawScore(g);
    }

    private void checkNavigation()
    {
        var marker2 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == users[currentStudent].Marker);
        var marker4 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 4);
        double distanceThreshold = 0.25;
        List<Question> questions = GetCurrentDifficulty();

        if (marker2 != null && marker4 != null)
        {
            double distance = Math.Sqrt(Math.Pow(marker2.X - marker4.X, 2) + Math.Pow(marker2.Y - marker4.Y, 2));

            if (distance < distanceThreshold && !hasNavigated)
            {
                currentQuestionIndex += 1;
                if (currentQuestionIndex > questions.Count)
                    currentQuestionIndex = 0;
                screen = 1;
                hasNavigated = true;
            }
            else if (distance >= distanceThreshold)
            {
                hasNavigated = false;
            }
        }
    }
    private void checkLogin(int userId)
    {
        //Find user id in users that matches the logged in user
        var user = users.FirstOrDefault(u => u.StudentId == userId);
        if (user != null)
        {
            screen = 4;
        }

    }
    
    private int selectedDeviceIndex = 0;
    private void chooseDevice(Graphics g)
    {
        g.Clear(Color.LightBlue);

        g.DrawString("Select Your Bluetooth Device", new Font("Arial", 24, FontStyle.Bold), Brushes.Black, new PointF(width / 2 - 150, 50));

        float centerX = width / 2;
        float centerY = height / 2;
        float radius = 150;

        int deviceCount = bluetoothDevices.Count;
        double angleIncrement = 360.0 / deviceCount;

        for (int i = 0; i < deviceCount; i++)
        {
            var device = bluetoothDevices[i];

            double angle = i * angleIncrement * (Math.PI / 180);
            float x = centerX + (float)(radius * Math.Cos(angle)) - 40;
            float y = centerY + (float)(radius * Math.Sin(angle)) - 10;


            var brush = (i == selectedDeviceIndex) ? Brushes.LightGreen : Brushes.White;
            g.FillEllipse(brush, x - 20, y - 20, 80, 40);
            g.DrawEllipse(Pens.Black, x - 20, y - 20, 80, 40);

            // Draw the device name
            g.DrawString(device, new Font("Arial", 12), Brushes.Black, new PointF(x, y));
        }

        // Handle device selection with TUIO rotation
        var marker = objectList.Values.FirstOrDefault();
        if (marker != null)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].Marker == marker.SymbolID)
                {
                    handleTuioRotation(marker, i);
                }
            }

        }
    }
    private void handleTuioRotation(TuioObject marker, int index)
    {
        if (marker != null)
        {
            // Ensure there are devices in the list
            if (bluetoothDevices.Count == 0)
            {
                Console.WriteLine("No Bluetooth devices available.");
                return; // Exit the function if no devices are present
            }

            double anglePerDevice = 360.0 / bluetoothDevices.Count;
            double angle = marker.Angle * (180 / Math.PI);

            angle = angle % 360;
            if (angle < 0) angle += 360;

            selectedDeviceIndex = (int)(angle / anglePerDevice) % bluetoothDevices.Count;
            var marker4 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 4);
            if (marker4 != null)
            {
                double distance = Math.Sqrt(Math.Pow(marker.X - marker4.X, 2) + Math.Pow(marker.Y - marker4.Y, 2));
                if (distance < 0.35)
                {
                    users[index].Bluetooth = bluetoothDevices[selectedDeviceIndex];
                    currentStudent = index;
                    screen = 1;
                    hasNavigated = true;
                }
            }
        }
    }

    private void checkCollisonTrue()
    {
        double distanceThreshold = 0.25;

        var marker1 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == users[currentStudent].Marker);
        var marker4 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 4); // Answer selection TUIO
        var marker5 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 5); // Previous TUIO

        if (marker4 != null && marker5 != null)
        {
            double distanceToMarker4_5 = Math.Sqrt(Math.Pow(marker4.X - marker5.X, 2) + Math.Pow(marker4.Y - marker5.Y, 2));
            if (distanceToMarker4_5 < 0.15)
            {
                screen = 3;
            }
        }

        List<Question> questions = GetCurrentDifficulty();

        if (marker4 != null && marker1 != null)
        {
            double distance = Math.Sqrt(Math.Pow(marker1.X - marker4.X, 2) + Math.Pow(marker1.Y - marker4.Y, 2));
            // Debug.WriteLine("Distance: " + distance);

            if (distance <= distanceThreshold && marker1.Angle >= 5.23599 && marker1.Angle <= 6.10865 && !hasNavigated)
            {
                responseMessage = "Ashter katkout"; // Correct answer message
                screen = 2; // Move to the next screen
                addScore(); // Increment the score
                hasNavigated = true; // Prevent further triggers

            }
            else if (distance <= distanceThreshold && (marker1.Angle <= 5.23599 || marker1.Angle >= 6.10865) && !hasNavigated)
            {
                responseMessage = "Try again"; // Incorrect answer message
                screen = 2; // Stay on the same screen for retry
                hasNavigated = true; // Prevent further triggers
            }
            else if (distance > distanceThreshold)
            {
                screen = 1;
                hasNavigated = false;
            }
        }

        if (gesture.Count > 0 && marker1 != null)
        {
            // Handle gesture for "ok"
            if (gesture[gesture.Count - 1] == "ok" && marker1.Angle >= 5.23599 && marker1.Angle <= 6.10865)
            {
                responseMessage = "Ashter katkout";
                screen = 2; // Move to the next screen
                addScore(); // Increment the score
                            // Handle difficulty-specific actions
                HandleDifficulty();
            }
            else if (gesture[gesture.Count - 1] == "ok" && (marker1.Angle <= 5.23599 || marker1.Angle >= 6.10865))
            {
                responseMessage = "Try again";
                screen = 2; // Stay on the same screen for retry
            }

            // Handle gesture for "stop"
            if (gesture[gesture.Count - 1] == "stop")
            {
                screen = 5;
            }
        }

        if (gesture.Count > 0)
        {
            // Handle gesture for "next" (next question)
            if (gesture[gesture.Count - 1] == "next")
            {
                currentQuestionIndex += 1;
                if (currentQuestionIndex > questions.Count)
                    currentQuestionIndex = 0;
            }
            // Handle gesture for "previous" (previous question)
            if (gesture[gesture.Count - 1] == "previous")
            {
                currentQuestionIndex -= 1;
                if (currentQuestionIndex < 0)
                    currentQuestionIndex = questions.Count - 1;
            }
        }
    }

    // Method to handle difficulty levels and actions after answering a question
    private void HandleDifficulty()
    {
        if (currentDifficulty == Difficulty.Easy)
        {
            score += 1; // Increase score for easy difficulty
            responseMessage = "Well done! You answered correctly!";
        }
        else if (currentDifficulty == Difficulty.Medium)
        {

            score += 2; // Increase score for medium difficulty
            responseMessage = "Good job! You are doing well!";
        }
        else if (currentDifficulty == Difficulty.Hard)
        {
            score += 3; // Increase score for hard difficulty
            responseMessage = "Excellent! You're mastering this!";
        }
    }


    private void exitRun()
    {
        var marker = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 8);
        var marker4 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 4);
        if (marker != null && marker4 != null)
        {
            double distance = Math.Sqrt(Math.Pow(marker.X - marker4.X, 2) + Math.Pow(marker.Y - marker4.Y, 2));
            if (distance < 0.35)
            {
                screen = 5;
            }
        }
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        // 
        // TuioDemo
        // 
        this.ClientSize = new System.Drawing.Size(282, 253);
        this.Name = "TuioDemo";
        this.Load += new System.EventHandler(this.TuioDemo_Load);
        this.ResumeLayout(false);

    }

    private void TuioDemo_Load(object sender, EventArgs e)
    {

    }

    private static async Task<string> ReadMessageAsync(NetworkStream stream)
    {
        byte[] lengthBytes = new byte[4];
        int bytesRead = await stream.ReadAsync(lengthBytes, 0, lengthBytes.Length);

        if (bytesRead == 0) return null; // Client disconnected
        int length = BitConverter.ToInt32(lengthBytes, 0);

        byte[] messageBytes = new byte[length];
        bytesRead = await stream.ReadAsync(messageBytes, 0, length);
        byte[] ack = new byte[] { 1 };  // Acknowledgment byte
        await stream.WriteAsync(ack, 0, ack.Length);
        return Encoding.UTF8.GetString(messageBytes, 0, bytesRead);

    }

    public static void StartGestureListener()
    {
        // Configure a timer to check for gestures every few seconds.
        gestureTimer = new System.Timers.Timer(200); // Use System.Timers.Timer explicitly
        gestureTimer.Elapsed += OnGestureTimeout;
        gestureTimer.AutoReset = true; // Timer will reset after each elapsed event
        gestureTimer.Enabled = true;
        Debug.WriteLine("timerrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr");
    }

    private static void OnGestureTimeout(object sender, ElapsedEventArgs e)
    {
        // If there is a gesture in the list and no new message has been received, clear it.
        if (gesture.Count > 0)
        {
            gesture.Clear();
            Debug.WriteLine("Gesture list cleared due to timeout.");
        }
    }
    private static async Task ProcessClientAsync(TcpClient client)
{
    using (NetworkStream stream = client.GetStream())
    {
        while (client.Connected) // Continuous loop for real-time handling
        {
            try
            {
                string message = await ReadMessageAsync(stream);

                if (message == null)
                {
                    Console.WriteLine("Client disconnected.");
                    break;
                }

                Debug.WriteLine("CONNECTED...");
                Debug.WriteLine(message);

                if (message.StartsWith("ID:"))
                {
                    int id = int.Parse(message.Substring(3));
                    loggedInUser = id;
                }
                else if (message.StartsWith("BT:"))
                {
                    string device = message.Substring(21);
                    if (!bluetoothDevices.Contains(device))
                    {
                        bluetoothDevices.Add(device);
                        Debug.WriteLine("Bluetooth Device " + bluetoothDevices.Count + ": " + device);
                    }
                }
                else if (message.StartsWith("DE:"))
                {
                    string objectDetection = message.Substring(3);
                    Debug.WriteLine("Object Detection: " + objectDetection);
                    currentObject = objectDetection;
                }
                else if (message.StartsWith("GEST:"))
                {
                    string gesture = message.Substring(5);
                    if (screen == 1 && gesture == "next")
                    {
                        currentQuestionIndex++;
                        if (currentQuestionIndex >= easyQuestions.Count)
                            currentQuestionIndex = 0;
                    }
                }
                else if (message.StartsWith("LAS:"))
                {
                    Debug.WriteLine("In LAS");
                    string laser = message.Substring(4);
                    if (screen == 6 && laser != "none")
                    {
                        laser = laser.Replace("'", "\"");

                        // Extract x and y coordinates
                        int xStartIndex = laser.IndexOf("\"x\":") + 5;
                        int xEndIndex = laser.IndexOf(",", xStartIndex);
                        int yStartIndex = laser.IndexOf("\"y\":") + 5;
                        int yEndIndex = laser.IndexOf("}", yStartIndex);

                        string xValueString = laser.Substring(xStartIndex, xEndIndex - xStartIndex).Trim();
                        string yValueString = laser.Substring(yStartIndex, yEndIndex - yStartIndex).Trim();

                        laser_X = int.Parse(xValueString);
                        laser_Y = int.Parse(yValueString);

                        Debug.WriteLine("Laser Coordinates: " + laser);
                    }
                }
                else if (message.StartsWith("URE:"))
                {
                    string device = message.Substring(4);
                    gesture.Add(device);
                    StartGestureListener();
                    Debug.WriteLine("Gesture is " + gesture.Count + ": " + device);
                }
                else if (message.StartsWith("EMOT:"))
                {
                    string emotion = message.Substring(5);
                    var user = users.FirstOrDefault(u => u.StudentId == 1);
                    if (user != null)
                    {
                        user.Emot = emotion;
                    }
                }

                if (gestureTimer != null)
                {
                    gestureTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading message: {ex.Message}");
                break;
            }
        }
    }

    client.Close();
}

    public static async Task StartServer()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 12345);
        server.Start();
        Console.WriteLine("Server started...");

        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            _ = ProcessClientAsync(client);
        }
    }

    public static void Main(string[] argv)
    {
        int port = 0;

        switch (argv.Length)
        {
            case 1:
                port = int.Parse(argv[0]);
                if (port == 0) goto default;
                break;
            case 0:
                port = 3333;
                break;
            default:
                Console.WriteLine("usage: TuioDemo [port]");
                Environment.Exit(0);
                break;
        }

        TuioDemo app = new TuioDemo(port);

        Thread systemThread = new Thread(() => StartServer().Wait())
        {
            IsBackground = true
        };
        systemThread.Start();

        Application.Run(app);
    }
}