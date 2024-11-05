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

public class Student
{
    public int StudentId { get; set; }
    public string Name { get; set; }

    public int Tscore { get; set; }

    public bool Attended { get; set; }

    // Constructor
    public Student(int studentId, string name)
    {
        StudentId = studentId;
        Name = name;
        Attended = false;
        Tscore = 0;
    }
}

public class TuioDemo : Form, TuioListener
{
    private TuioClient client;
    private Dictionary<long, TuioObject> objectList;
    private Dictionary<long, TuioCursor> cursorList;
    private Dictionary<long, TuioBlob> blobList;

    public static int width, height;
    private int window_width = 640;
    private int window_height = 480;
    private int window_left = 0;
    private int window_top = 0;
    private int screen_width = Screen.PrimaryScreen.Bounds.Width;
    private int screen_height = Screen.PrimaryScreen.Bounds.Height;

    private bool fullscreen;
    private bool verbose;
    private static List<string> guesture = new List<string>();
    private static int screen =5;
    private static int QuestionNumber = 0;
    private string Question = "What is the capital of Egypt?";
    private string choiceOne = "Alex";
    private string choiceTwo = "Cairo";
    private string choiceThree = "Giza";
    private string choiceFour = "Aswan";
    private string responseMessage = "";
    private int score = 0;
    private Dictionary<string, int> studentScores = new Dictionary<string, int>();
    private string dummyStudentId = "123";

    private static List<string> questions = new List<string>();
    private static List<string> imagePaths = new List<string>();
    private static List<string> answers = new List<string>();
    private static List<string> bluetoothDevices = new List<string>();
    private List<Student> students = new List<Student>
    {
    new Student(1, "John Doe") { Attended = true, Tscore = 3 },
    new Student(2, "Jane Smith") { Attended = true, Tscore = 5 },
    new Student(3, "Alex Brown") { Attended = false, Tscore = 0 }
    };

    Font font = new Font("Arial", 10.0f);
    SolidBrush fntBrush = new SolidBrush(Color.White);
    SolidBrush bgrBrush = new SolidBrush(Color.Purple);
    SolidBrush curBrush = new SolidBrush(Color.FromArgb(192, 0, 192));
    SolidBrush objBrush = new SolidBrush(Color.FromArgb(64, 0, 0));
    SolidBrush blbBrush = new SolidBrush(Color.FromArgb(64, 64, 64));
    Pen curPen = new Pen(new SolidBrush(Color.Blue), 1);

    public static object marker1 { get; private set; }

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
        }

        // Check for the TUIO object ID and change the screen based on the SymbolID
        //if (screen)
        {
            /*if (objectList.Values.Any(obj => obj.SymbolID == 1) &&
           objectList.Values.Any(obj => obj.SymbolID == 4))
            {
                responseMessage = "Ashter katkout";
                welcomeScreen = false;
            }
            else if (objectList.Values.Any(obj => obj.SymbolID == 4) &&
					!objectList.Values.Any(obj => obj.SymbolID == 1))
            {
                responseMessage = "Stubiddd";
                welcomeScreen = false;
            }*/

            //        if (objectList.Values.Any(obj => obj.SymbolID == 1) &&
            //objectList.Values.Any(obj => obj.SymbolID == 4) &&
            //objectList.Values.Any(obj => obj.SymbolID == 1 && obj.Angle >= 5.23599 && obj.Angle <= 6.10865))  // Range: 300 to 350 degrees (in radians)
            //        {

            //            responseMessage = "Ashter katkout";
            //            welcomeScreen = false;
            //        }
            //        else if (objectList.Values.Any(obj => obj.SymbolID == 4) &&
            //                !objectList.Values.Any(obj => obj.SymbolID == 1 && obj.Angle >= 5.23599 && obj.Angle <= 6.10865))
            //        {
            //            responseMessage = "Stubiddd";
            //            welcomeScreen = false;
            //        }
        }
    }

    public void updateTuioObject(TuioObject o)
    {

        if (verbose) Console.WriteLine("set obj " + o.SymbolID + " " + o.SessionID + " " + o.X + " " + o.Y + " " + o.Angle + " " + o.MotionSpeed + " " + o.RotationSpeed + " " + o.MotionAccel + " " + o.RotationAccel);
    }

    public void removeTuioObject(TuioObject o)
    {
        lock (objectList)
        {
            objectList.Remove(o.SessionID);
        }

        // Reset the message if one of the objects is removed
        if (!objectList.Values.Any(obj => obj.SymbolID == 1) ||
            !objectList.Values.Any(obj => obj.SymbolID == 4))
        {
            responseMessage = Question;  // Reset to welcome message
            screen = 1;
        }
    }

    public void addTuioCursor(TuioCursor c)
    {
        lock (cursorList)
        {
            cursorList.Add(c.SessionID, c);
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


        if (screen == 1) // 1 is the question screen
        {
            checkCollisonTrue();
            changeQuestionBackground(pevent, g, c1Brush, c2Brush, c3Brush, c4Brush);
            checkCollisonTrue();
        }
        else if (screen == 2) // 2 is the answer screen
        {
            drawScreenTwo(g);
            checkCollisonTrue();
        }
        else if (screen == 3) // 3 is the teacher screen
        {
            drawScreenThree(g);
            checkCollisonTrue();
        }
        else if (screen == 4)
        {
            drawScreenFour(pevent, g, c1Brush, c2Brush, c3Brush, c4Brush);
            checkCollisonTrue();
        }
        else if (screen == 5)
        {
            DrawWelcomeScreen(g, pevent);
           
            checkCollisonTrue();
        }
    }
    private void DrawWelcomeScreen(Graphics g, PaintEventArgs pevent)
    {
        // Define dimensions based on window size
        int width = this.ClientSize.Width;
        int height = this.ClientSize.Height;

        // Set background gradient with colors that look good on larger screens
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

        // Load and draw the character image at the top, scaled based on window size
        try
        {
            using (Image characterImage = Image.FromFile("ID.png"))
            {
                float imageWidth = width / 6;
                float imageHeight = characterImage.Height * (imageWidth / characterImage.Width);
                float imageX = (width - imageWidth) / 2;
                float imageY = height / 4 - imageHeight;

                g.DrawImage(characterImage, new RectangleF(imageX, imageY, imageWidth, imageHeight));
            }
        }
        catch (FileNotFoundException)
        {
            // Display a red "Image not found!" message on the screen for debugging
            g.DrawString("Image not found!", new Font("Arial", 12), Brushes.Red, 10, 10);
        }

        // Set dynamic font size for welcome text based on window height
        float fontSize = Math.Max(18, height / 20);
        Font font = new Font("Comic Sans MS", fontSize, FontStyle.Bold);
        Brush textBrush = new SolidBrush(Color.Yellow);

        // Welcome text
        string welcomeText = "Welcome! Show your ID code to start";

        // Measure text to center it below the image
        SizeF textSize = g.MeasureString(welcomeText, font);
        float x = (width - textSize.Width) / 2;
        float y = height / 2; // Position text below the image

        // Draw semi-transparent rounded rectangle (text bubble) around text
        RectangleF textRect = new RectangleF(x - 6, y -10, textSize.Width + 10, textSize.Height + 20);
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

        // Draw welcome text within the bubble
        g.DrawString(welcomeText, font, textBrush, x, y);

        // Dispose of resources
        font.Dispose();
        textBrush.Dispose();
    }



    private void TuioInputReceived(PaintEventArgs pevent, int tuioId)
    {
        //this.tuioId = tuioId; // Store the received TUIO ID in the class property
        Graphics g = pevent.Graphics;

        // Clear previous drawing and redraw the background image
        g.DrawImage(Image.FromFile("home.jpg"), 0, 0, width, height);
        

        // SolidBrushes remain the same
        SolidBrush c1Brush = new SolidBrush(Color.Red);
        SolidBrush c2Brush = new SolidBrush(Color.Green);
        SolidBrush c3Brush = new SolidBrush(Color.Blue);
        SolidBrush c4Brush = new SolidBrush(Color.Olive);
        changeQuestionBackground(pevent, g, c1Brush, c2Brush, c3Brush, c4Brush);

        // Handle TUIO input: switch screens based on TUIO ID
        if (tuioId == 10)
        {

            drawScreenTwo(g);
            checkCollisonTrue();
            screen = 1;

        }
        else if (tuioId == 11)
        {
            drawScreenThree(g);
            checkCollisonTrue(); // Switch to teacher screen
            screen = 3;
        }

        // Refresh the form to update the screen
        this.Invalidate();
    }

    private void drawScreenFour(PaintEventArgs pevent,
    Graphics g,
    SolidBrush brush1,
    SolidBrush brush2,
    SolidBrush brush3,
    SolidBrush brush4)
    {

    }


    private void drawScreenTwo(Graphics g)
    {
        // Display the response after TUIO interaction
        if (responseMessage == "Ashter katkout")
        {
            g.DrawString
                (responseMessage,
                 new Font("Comic Sans MS", 18.0f, FontStyle.Bold),
                 new SolidBrush(Color.Cyan),
                 new PointF(width / 2 - 100, height / 2));
        }
        else if (responseMessage == "Try again")
        {
            g.DrawString
                (responseMessage,
                 new Font("Arial", 19.0f, FontStyle.Italic),
                 new SolidBrush(Color.Red),
                 new PointF(width / 2 - 100, height / 2));
        }
        checkCollisonTrue();
    }
    private void drawScreenThree(Graphics g)
    {
        // Set the background color by filling the rectangle covering the entire screen
        g.Clear(Color.LightBlue); // Change to any background color you prefer

        // Draw a welcome message
        g.DrawString("Hello, Teacher!", new Font("Arial", 24, FontStyle.Bold), Brushes.Black, new PointF(width / 2 - 100, 50));

        // Draw the header for the table
        g.FillRectangle(Brushes.DarkBlue, new Rectangle((int)(width / 2 - 150), 100, 300, 40));
        g.DrawString("Name", new Font("Arial", 16, FontStyle.Bold), Brushes.White, new PointF(width / 2 - 140, 110));
        g.DrawString("Score", new Font("Arial", 16, FontStyle.Bold), Brushes.White, new PointF(width / 2 + 50, 110));

        // Variables for positioning the rows
        float startX = width / 2 - 150;
        float startY = 150;
        float rowHeight = 30;
        float rowWidth = 300;

        // Draw table rows for each student who attended the quiz
        foreach (var student in students)
        {
            if (student.Attended)
            {
                // Draw a rectangle for the current row
                g.FillRectangle(Brushes.White, new Rectangle((int)startX, (int)startY, (int)rowWidth, (int)rowHeight));
                g.DrawRectangle(Pens.Black, new Rectangle((int)startX, (int)startY, (int)rowWidth, (int)rowHeight));

                // Draw student name and score
                g.DrawString(student.Name, new Font("Arial", 14), Brushes.Black, new PointF(startX + 10, startY + 5));
                g.DrawString(student.Tscore.ToString(), new Font("Arial", 14), Brushes.Black, new PointF(startX + 200, startY + 5));

                // Move down for the next row
                startY += rowHeight;
            }
        }

        // Check for any collision detection logic if necessary
        checkCollisonTrue();
    }



    private void addScore()
    {
        score += 1;
        studentScores[dummyStudentId] = score;
        Debug.WriteLine("hi");
    }

    private void changeQuestionBackground(PaintEventArgs pevent,
        Graphics g,
        SolidBrush c1Brush,
        SolidBrush c2Brush,
        SolidBrush c3Brush,
        SolidBrush c4Brush)
    {

        Brush[] quadrantBrushes = { c1Brush, c2Brush, c3Brush, c4Brush };

        // Dimensions for the quadrants
        int midWidth = width / 2;
        int midHeight = height / 2;

        //// Draw each quadrant

        if (answers.Count >= 4)
        {
            int ii = 0;
            for (int i = QuestionNumber * 4; i < QuestionNumber + 4; i++)
            {
                int x = (i % 2 == 0) ? 0 : midWidth; // Left or right half
                int y = (i < 2) ? 0 : midHeight; // Top or bottom half

                // Fill each quadrant with a different color
                g.FillRectangle(quadrantBrushes[ii], x, y, midWidth, midHeight);
                ii++;

                // Draw the city name in the center of each quadrant
                var cityFont = new Font("Arial", 24, FontStyle.Bold);
                var textSize = g.MeasureString(answers[i], cityFont);
                float textX = x + (midWidth - textSize.Width) / 2;
                float textY = y + (midHeight - textSize.Height) / 2;
                g.DrawString(answers[i], cityFont, Brushes.White, new PointF(textX, textY));
            }
        }

        // Draw a box in the center for the question
        int questionBoxWidth = 400;
        int questionBoxHeight = 100;
        int boxX = (width - questionBoxWidth) / 2;
        int boxY = (height - questionBoxHeight) / 2;

        ///////////

        var marker1 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 1);





        if (marker1 != null)
        {
            int x = 0;
            int y = 0;

            int i = QuestionNumber * 4;
            if (marker1.Angle >= 4.7 && marker1.Angle <= 6.5)
            {
                x = (1 % 2 == 0) ? 0 : midWidth; // Left or right halfKD
                y = (1 < 2) ? 0 : midHeight; // Top or bottom half
                g.DrawImage(Image.FromFile(imagePaths[i]), x, y, midWidth, midHeight);
            }


            if (marker1.Angle >= 3.2 && marker1.Angle <= 4.687)
            {
                x = (0 % 2 == 0) ? 0 : midWidth; // Left or right half
                y = (0 < 2) ? 0 : midHeight; // Top or bottom half
                g.DrawImage(Image.FromFile(imagePaths[i + 1]), x, y, midWidth, midHeight);
            }


            if (marker1.Angle >= 1.6 && marker1.Angle <= 3.1)
            {
                x = (2 % 2 == 0) ? 0 : midWidth; // Left or right half
                y = (2 < 2) ? 0 : midHeight; // Top or bottom half
                g.DrawImage(Image.FromFile(imagePaths[i + 2]), x, y, midWidth, midHeight);
            }


            if (marker1.Angle >= 0 && marker1.Angle <= 1.5)
            {
                x = (3 % 2 == 0) ? 0 : midWidth; // Left or right half
                y = (3 < 2) ? 0 : midHeight; // Top or bottom half

                g.DrawImage(Image.FromFile(imagePaths[i + 3]), x, y, midWidth, midHeight);
            }



            // Log or display the angle for debugging purposes
            Debug.WriteLine("Marker1 Angle: " + marker1.Angle);
        }

        var tuioObject = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 1);
        if (tuioObject != null)
        {

            // Get position and angle of the TUIO object
            float ox = tuioObject.getScreenX(width);
            float oy = tuioObject.getScreenY(height);
            float angle = tuioObject.Angle; // Angle in radians

            // Draw the arrow at the object's position
            DrawArrow(pevent.Graphics, window_width / 2, window_height / 2, angle, 250); // Adjust size as needed
        }

        // Draw the question box with a border
        g.FillRectangle(Brushes.White, boxX, boxY, questionBoxWidth, questionBoxHeight);
        g.DrawRectangle(Pens.Black, boxX, boxY, questionBoxWidth, questionBoxHeight);
        // Draw the question inside the box

        if (questions.Count > 0)
        {
            var questionFont = new Font("Arial", 18, FontStyle.Bold);
            var questionTextSize = g.MeasureString(questions[QuestionNumber], questionFont);
            float questionTextX = boxX + (questionBoxWidth - questionTextSize.Width) / 2;
            float questionTextY = boxY + (questionBoxHeight - questionTextSize.Height) / 2;
            g.DrawString(questions[QuestionNumber], questionFont, Brushes.Black, new PointF(questionTextX, questionTextY));
        }
    }

    private void checkCollisonTrue()
    {
        double distanceThreshold = 0.35;

        // Get the objects for SymbolID 1 and 4
        var marker1 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 1);
        var marker4 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 4); // Answer selection TUIO

        if (marker4 != null && marker1 != null)
        {
            // Calculate the Euclidean distance between the two markers
            double distance = Math.Sqrt(Math.Pow(marker1.X - marker4.X, 2) + Math.Pow(marker1.Y - marker4.Y, 2));
            Debug.WriteLine("Distance: " + distance);


            if (distance >= distanceThreshold)
            {
                screen = 1;
            }
            else if (distance <= distanceThreshold && marker1.Angle >= 5.23599 && marker1.Angle <= 6.10865)
            {
                responseMessage = "Ashter katkout";
                screen = 2;
                addScore();
            }
            else if (distance <= distanceThreshold && (marker1.Angle <= 5.23599 || marker1.Angle >= 6.10865))
            {
                responseMessage = "Try again";
                screen = 2;
            }
        }

        if (guesture.Count > 0 && marker1 != null) 
        {
            
            if (guesture[guesture.Count - 1] == "ok" && marker1.Angle >= 5.23599 && marker1.Angle <= 6.10865)
            {
                responseMessage = "Ashter katkout";
                screen = 2;
                addScore();
            }
            else if (guesture[guesture.Count - 1] == "ok" && (marker1.Angle <= 5.23599 || marker1.Angle >= 6.10865))
            {
                responseMessage = "Try again";
                screen = 2;
            }
            if (guesture[guesture.Count - 1] == "stop")
            {
                responseMessage = "you left the party";
            }
        }
        if (guesture.Count > 0)
        {
            if (guesture[guesture.Count - 1] == "stop")
            {
                responseMessage = "you left the party";
            }
        }

        var marker2 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 2); // navigation TUIO
        float thresholdSpeed = 0.01f;
        if (marker2 != null)
        {

            Debug.WriteLine("Marker2 Speed: " + marker2.RotationSpeed);
            // Check if it's on the right half and movement exceeds the threshold
            if (marker2.RotationSpeed > 7)
            {
                QuestionNumber += 1;
                if (QuestionNumber > questions.Count)
                    QuestionNumber = 0;
            }
            if (marker2.RotationSpeed < -7)
            {
                QuestionNumber -= 1;
                if (QuestionNumber < 0)
                    QuestionNumber = questions.Count-1; 
            }

        }
        if (guesture.Count > 0)
        {
            if (guesture[guesture.Count - 1]=="next")
            {
                QuestionNumber += 1;
                if (QuestionNumber > questions.Count)
                    QuestionNumber = 0;
            }
            if (guesture[guesture.Count - 1] == "previous")
            {
                QuestionNumber -= 1;
                if (QuestionNumber < 0)
                    QuestionNumber = questions.Count - 1;
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

    private static async Task ProcessClientAsync(TcpClient client)
    {
        using (NetworkStream stream = client.GetStream())
        {
            while (client.Connected)  // Continuous loop for real-time handling
            {
                string message = await ReadMessageAsync(stream);
                if (message == null)
                {
                    Console.WriteLine("Client disconnected.");
                    break;
                }
                Debug.WriteLine(message);

                // Determine message type by prefix and process accordingly
                if (message.StartsWith("Q:"))
                {
                    string question = message.Substring(2);
                    if (!questions.Contains(question))
                    {
                        questions.Add(question);
                        Debug.WriteLine("Question: " + question);
                    }
                }
                else if (message.StartsWith("A:"))
                {
                    string answer = message.Substring(2);
                    if (!answers.Contains(answer))
                    {
                        answers.Add(answer);
                        Debug.WriteLine("Answer " + answers.Count % 4 + ": " + answer);
                    } 
                }
                else if (message.StartsWith("IMG:"))
                {
                    string imagePath = message.Substring(4);
                    if (!imagePaths.Contains(imagePath))
                    {
                        imagePaths.Add(imagePath);
                        Debug.WriteLine("Image Path " + imagePaths.Count % 4+ ": " + imagePath);
                    }
                }
                else if (message.StartsWith("BT:"))
                {
                    string device = message.Substring(3, 17);
                    bluetoothDevices.Add(device);
                    Debug.WriteLine("Bluetooth Device " + bluetoothDevices.Count + ": " + device);
                }
                if (message.StartsWith("URE:"))
                {
                    string device = message.Substring(4);
                    guesture.Add(device);
                    Debug.WriteLine("guesture is " + guesture.Count + ": " + device);
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

        while (true)  // Continuous loop to keep server running
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            _ = ProcessClientAsync(client); // Process each client in a separate task
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

        // Assuming TuioDemo is a defined class that accepts an integer port
        TuioDemo app = new TuioDemo(port);

        // Start the server in a new background thread
        Thread systemThread = new Thread(() => StartServer().Wait())
        {
            IsBackground = true // Ensures the thread stops when the main app closes
        };
        systemThread.Start();

        // Run the main application
        Application.Run(app);
    }
}