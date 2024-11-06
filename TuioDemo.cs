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

public class Student
{
    public int StudentId { get; set; }
    public string Name { get; set; }

    public int Tscore { get; set; }

    public bool Attended { get; set; }

    public string Bluetooth { get; set; }

    public int Marker { get; set; }

    // Constructor
    public Student(int studentId, string name, string bluetooth, int marker)
    {
        StudentId = studentId;
        Name = name;
        Attended = false;
        Tscore = 0;
        Bluetooth = bluetooth;
        Marker = marker;
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
    private static int screen = 5;
    private static int QuestionNumber = 0;
    private string responseMessage = "";
    private int score = 0;
    private Dictionary<string, int> studentScores = new Dictionary<string, int>();
    private string dummyStudentId = "123";
    private int currentStudent = 0;
    private bool hasNavigated = false;

    private static List<string> questions = new List<string>();
    private static List<string> imagePaths = new List<string>();
    private static List<string> answers = new List<string>();
    private static List<string> bluetoothDevices = new List<string>();
    private List<Student> students = new List<Student>
    {
    new Student(1, "Kamal Mohamed", "CC:F9:F0:CD:B9:DC",0) { Attended = false, Tscore = 0 },
    new Student(2, "Jane Smith", "", 0) { Attended = true, Tscore = 5 },
    new Student(3, "Alex Brown", "", 0) { Attended = false, Tscore = 0 }
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

        checkLogin();
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
                studentRegister();
                break;
            case 6:
                chooseDevice(g);
                break;
        }

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

        // Load and draw the character image at the top, scaled based on window size
        //try
        //{
        //    using (Image characterImage = Image.FromFile("ID.png"))
        //    {
        //        float imageWidth = width / 6;
        //        float imageHeight = characterImage.Height * (imageWidth / characterImage.Width);
        //        float imageX = (width - imageWidth) / 2;
        //        float imageY = height / 4 - imageHeight;

        //        g.DrawImage(characterImage, new RectangleF(imageX, imageY, imageWidth, imageHeight));
        //    }
        //}
        //catch (FileNotFoundException)
        //{
        //     //Display a red "Image not found!" message on the screen for debugging
        //    g.DrawString("Image not found!", new Font("Arial", 12), Brushes.Red, 10, 10);
        //}
        // Set dynamic font size for welcome text based on window height
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
        drawWelcomeBackground(g, pevent, "Student register with TUIO");
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
        g.DrawString("Device", new Font("Arial", 16, FontStyle.Bold), Brushes.White, new PointF(width / 2 + 60, 110));
        g.DrawString("Attended", new Font("Arial", 16, FontStyle.Bold), Brushes.White, new PointF(width / 2 + 180, 110));

        float startX = width / 2 - 250;
        float startY = 150;
        float rowHeight = 30;
        float rowWidth = 500;

        foreach (var student in students)
        {
            g.FillRectangle(Brushes.White, new Rectangle((int)startX, (int)startY, (int)rowWidth, (int)rowHeight));
            g.DrawRectangle(Pens.Black, new Rectangle((int)startX, (int)startY, (int)rowWidth, (int)rowHeight));

            g.DrawString(student.Name, new Font("Arial", 14), Brushes.Black, new PointF(startX + 10, startY + 5));
            g.DrawString(student.Tscore.ToString(), new Font("Arial", 14), Brushes.Black, new PointF(startX + 180, startY + 5));
            g.DrawString(student.Bluetooth, new Font("Arial", 8), Brushes.Black, new PointF(startX + 300, startY + 5));
            g.DrawString(student.Attended ? "Yes" : "No", new Font("Arial", 14), Brushes.Black, new PointF(startX + 420, startY + 5));

            startY += rowHeight;

        }

        checkCollisonTrue();
    }


    private void drawScreenFour(PaintEventArgs pevent,
    Graphics g,
    SolidBrush brush1,
    SolidBrush brush2,
    SolidBrush brush3,
    SolidBrush brush4)
    {

    }

    private void addScore()
    {
        students[currentStudent].Tscore += 5;
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

        if (answers.Count >= 4)
        {
            int ii = 0;
            for (int i = QuestionNumber * 4; i < (QuestionNumber * 4) + 4; i++)
            {
                Debug.WriteLine(i + answers[i]);
                int x = (i % 2 == 0) ? 0 : midWidth;
                int y = ((i % 4) < 2) ? 0 : midHeight;

                g.FillRectangle(quadrantBrushes[ii], x, y, midWidth, midHeight);
                ii++;

                var cityFont = new Font("Arial", 24, FontStyle.Bold);
                var textSize = g.MeasureString(answers[i], cityFont);
                float textX = x + (midWidth - textSize.Width) / 2;
                float textY = y + (midHeight - textSize.Height) / 2;
                g.DrawString(answers[i], cityFont, Brushes.White, new PointF(textX, textY));
            }
        }

        int questionBoxWidth = 400;
        int questionBoxHeight = 100;
        int boxX = (width - questionBoxWidth) / 2;
        int boxY = (height - questionBoxHeight) / 2;


        var marker1 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == students[currentStudent].Marker);

        if (marker1 != null)
        {
            int x = 0;
            int y = 0;

            int i = QuestionNumber * 4;
            if (marker1.Angle >= 4.7 && marker1.Angle <= 6.5)
            {
                x = (1 % 2 == 0) ? 0 : midWidth;
                y = (1 < 2) ? 0 : midHeight;
                g.DrawImage(Image.FromFile(imagePaths[i]), x, y, midWidth, midHeight);
            }


            if (marker1.Angle >= 3.2 && marker1.Angle <= 4.687)
            {
                x = (0 % 2 == 0) ? 0 : midWidth;
                y = (0 < 2) ? 0 : midHeight;
                g.DrawImage(Image.FromFile(imagePaths[i + 1]), x, y, midWidth, midHeight);
            }


            if (marker1.Angle >= 1.6 && marker1.Angle <= 3.1)
            {
                x = (2 % 2 == 0) ? 0 : midWidth;
                y = (2 < 2) ? 0 : midHeight;
                g.DrawImage(Image.FromFile(imagePaths[i + 2]), x, y, midWidth, midHeight);
            }


            if (marker1.Angle >= 0 && marker1.Angle <= 1.5)
            {
                x = (3 % 2 == 0) ? 0 : midWidth;
                y = (3 < 2) ? 0 : midHeight;

                g.DrawImage(Image.FromFile(imagePaths[i + 3]), x, y, midWidth, midHeight);
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


        if (questions.Count > 0)
        {
            var questionFont = new Font("Arial", 18, FontStyle.Bold);
            var questionTextSize = g.MeasureString(questions[QuestionNumber], questionFont);
            float questionTextX = boxX + (questionBoxWidth - questionTextSize.Width) / 2;
            float questionTextY = boxY + (questionBoxHeight - questionTextSize.Height) / 2;
            g.DrawString(questions[QuestionNumber], questionFont, Brushes.Black, new PointF(questionTextX, questionTextY));
        }
    }


    private void checkNavigation()
    {
        var marker2 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == students[currentStudent].Marker);
        var marker4 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 4);
        double distanceThreshold = 0.25;

        if (marker2 != null && marker4 != null)
        {
            double distance = Math.Sqrt(Math.Pow(marker2.X - marker4.X, 2) + Math.Pow(marker2.Y - marker4.Y, 2));

            if (distance < distanceThreshold && !hasNavigated)
            {
                if (marker2.X > marker4.X)
                {
                    QuestionNumber = (QuestionNumber + 1) % questions.Count;
                    screen = 1;
                }
                else if (marker2.X < marker4.X)
                {
                    QuestionNumber = (QuestionNumber - 1 + questions.Count) % questions.Count;
                    screen = 1;
                }
                hasNavigated = true;
            }
            else if (distance >= distanceThreshold)
            {
                hasNavigated = false;
            }
        }
    }
    private void checkLogin()
    {
        var marker1 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 1); // Student Login
        if (marker1 != null)
        {
            for (int i = 0; i < students.Count; i++)
            {
                if (students[i].Bluetooth == "CC:F9:F0:CD:B9:DC")
                {
                    students[i].Attended = true;
                    screen = 3;
                }
            }
        }
    }
    private void studentRegister()
    {
        var marker = objectList.Values.FirstOrDefault();
        if (marker != null && marker.SymbolID != 4 && marker.SymbolID != 8 && marker.SymbolID != 10)
        {
            bool flag = false;
            // check if any students has the current register marker
            for (int i = 0; i < students.Count; i++)
            {
                if (students[i].Marker == marker.SymbolID)
                {
                    students[i].Attended = true;
                    currentStudent = i;
                    screen = 3;
                    flag = true;
                }
            }
            if (flag) return;
            Student temp = new Student(students.Count + 1, "Student" + (students.Count + 1), "", marker.SymbolID) { Attended = true };
            students.Add(temp);
            currentStudent = students.Count - 1;
            screen = 6;

        }
        else if (marker != null)
        {
            if(marker.SymbolID == 10)
            {
                screen = 3;
            }
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
            for (int i = 0; i < students.Count; i++)
            {
                if (students[i].Marker == marker.SymbolID)
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
                    students[index].Bluetooth = bluetoothDevices[selectedDeviceIndex];
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

        var marker1 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == students[currentStudent].Marker);
        var marker4 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 4); // Answer selection TUIO

        if (marker4 != null && marker1 != null)
        {
            double distance = Math.Sqrt(Math.Pow(marker1.X - marker4.X, 2) + Math.Pow(marker1.Y - marker4.Y, 2));
            //Debug.WriteLine("Distance: " + distance);
            if (distance <= distanceThreshold && marker1.Angle >= 5.23599 && marker1.Angle <= 6.10865 && !hasNavigated)
            {
                responseMessage = "Ashter katkout";
                screen = 2;
                addScore();
                hasNavigated = true;
            }
            else if (distance <= distanceThreshold && (marker1.Angle <= 5.23599 || marker1.Angle >= 6.10865) && !hasNavigated)
            {
                responseMessage = "Try again";
                screen = 2;
                hasNavigated = true;
            }
            else if (distance > distanceThreshold)
            {
                screen = 1;
                hasNavigated = false;
            }

        }
        if (gesture.Count > 0 && marker1 != null)
        {

            if (gesture[gesture.Count - 1] == "ok" && marker1.Angle >= 5.23599 && marker1.Angle <= 6.10865)
            {
                responseMessage = "Ashter katkout";
                screen = 2;
                addScore();
            }
            else if (gesture[gesture.Count - 1] == "ok" && (marker1.Angle <= 5.23599 || marker1.Angle >= 6.10865))
            {
                responseMessage = "Try again";
                screen = 2;
            }
            if (gesture[gesture.Count - 1] == "stop")
            {
                exitRun();
            }
        }
        if (gesture.Count > 0)
        {
            if (gesture[gesture.Count - 1] == "stop")
            {
                exitRun();
            }
        }


        if (gesture.Count > 0)
        {
            if (gesture[gesture.Count - 1] == "next")
            {
                QuestionNumber += 1;
                if (QuestionNumber > questions.Count)
                    QuestionNumber = 0;
            }
            if (gesture[gesture.Count - 1] == "previous")
            {
                QuestionNumber -= 1;
                if (QuestionNumber < 0)
                    QuestionNumber = questions.Count - 1;
            }

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
                        //Debug.WriteLine("Question: " + question);
                    }
                }
                else if (message.StartsWith("A:"))
                {
                    string answer = message.Substring(2);
                    if (!answers.Contains(answer))
                    {
                        answers.Add(answer);
                        //Debug.WriteLine("Answer " + answers.Count % 4 + ": " + answer);
                    }
                }
                else if (message.StartsWith("IMG:"))
                {
                    string imagePath = message.Substring(4);
                    if (!imagePaths.Contains(imagePath))
                    {
                        imagePaths.Add(imagePath);
                        //Debug.WriteLine("Image Path " + imagePaths.Count % 4+ ": " + imagePath);
                    }
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
                if (gestureTimer != null)
                {
                    gestureTimer.Stop();
                }
                if (message.StartsWith("URE:"))
                {
                    string device = message.Substring(4);
                    gesture.Add(device);
                    StartGestureListener();
                    Debug.WriteLine("gesture is " + gesture.Count + ": " + device);
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