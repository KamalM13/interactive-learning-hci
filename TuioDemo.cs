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

	private bool welcomeScreen = true;
	private string Question = "What is the capital of Egypt?";
    private string choiceOne = "Alex";
    private string choiceTwo = "Cairo";
    private string choiceThree = "Giza";
    private string choiceFour = "Aswan";
    private string responseMessage = "";

    private static List<string> questions = new List<string>();
    private static List<string> imagePaths = new List<string>();
    private static List<string> answers = new List<string>();  

    Font font = new Font("Arial", 10.0f);
	SolidBrush fntBrush = new SolidBrush(Color.White);
	SolidBrush bgrBrush = new SolidBrush(Color.Purple);
	SolidBrush curBrush = new SolidBrush(Color.FromArgb(192, 0, 192));
	SolidBrush objBrush = new SolidBrush(Color.FromArgb(64, 0, 0));
	SolidBrush blbBrush = new SolidBrush(Color.FromArgb(64, 64, 64));
	Pen curPen = new Pen(new SolidBrush(Color.Blue), 1);

	public TuioDemo(int port) {

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

	private void Form_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {

		if (e.KeyData == Keys.F1) {
			if (fullscreen == false) {

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
			} else {

				width = window_width;
				height = window_height;

				this.FormBorderStyle = FormBorderStyle.Sizable;
				this.Left = window_left;
				this.Top = window_top;
				this.Width = window_width;
				this.Height = window_height;

				fullscreen = false;
			}
		} else if (e.KeyData == Keys.Escape) {
			this.Close();

		} else if (e.KeyData == Keys.V) {
			verbose = !verbose;
		}

	}

	private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		client.removeTuioListener(this);

		client.disconnect();
		System.Environment.Exit(0);
	}

	public void addTuioObject(TuioObject o) {
		lock (objectList)
		{
			objectList.Add(o.SessionID, o);
		}

		// Check for the TUIO object ID and change the screen based on the SymbolID
		if (welcomeScreen)
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

	public void updateTuioObject(TuioObject o) {

		if (verbose) Console.WriteLine("set obj " + o.SymbolID + " " + o.SessionID + " " + o.X + " " + o.Y + " " + o.Angle + " " + o.MotionSpeed + " " + o.RotationSpeed + " " + o.MotionAccel + " " + o.RotationAccel);
	}

	public void removeTuioObject(TuioObject o) {
        lock (objectList)
        {
            objectList.Remove(o.SessionID);
        }

        // Reset the message if one of the objects is removed
        if (!objectList.Values.Any(obj => obj.SymbolID == 1) ||
            !objectList.Values.Any(obj => obj.SymbolID == 4))
        {
            responseMessage = Question;  // Reset to welcome message
            welcomeScreen = true;
		}
    }

	public void addTuioCursor(TuioCursor c) {
		lock (cursorList) {
			cursorList.Add(c.SessionID, c);
		}
		if (verbose) Console.WriteLine("add cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y);
	}

	public void updateTuioCursor(TuioCursor c) {
		if (verbose) Console.WriteLine("set cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y + " " + c.MotionSpeed + " " + c.MotionAccel);
	}

	public void removeTuioCursor(TuioCursor c) {
		lock (cursorList) {
			cursorList.Remove(c.SessionID);
		}
		if (verbose) Console.WriteLine("del cur " + c.CursorID + " (" + c.SessionID + ")");
	}

	public void addTuioBlob(TuioBlob b) {
		lock (blobList) {
			blobList.Add(b.SessionID, b);
		}
		if (verbose) Console.WriteLine("add blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area);
	}

	public void updateTuioBlob(TuioBlob b) {

		if (verbose) Console.WriteLine("set blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area + " " + b.MotionSpeed + " " + b.RotationSpeed + " " + b.MotionAccel + " " + b.RotationAccel);
	}

	public void removeTuioBlob(TuioBlob b) {
		lock (blobList) {
			blobList.Remove(b.SessionID);
		}
		if (verbose) Console.WriteLine("del blb " + b.BlobID + " (" + b.SessionID + ")");
	}

	public void refresh(TuioTime frameTime) {
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
        // Getting the graphics object
        /*Graphics g = pevent.Graphics;
        g.FillRectangle(bgrBrush, new Rectangle(0,0,width,height));

        // draw the cursor path
        if (cursorList.Count > 0) {
         lock(cursorList) {
         foreach (TuioCursor tcur in cursorList.Values) {
                List<TuioPoint> path = tcur.Path;
                TuioPoint current_point = path[0];

                for (int i = 0; i < path.Count; i++) {
                    TuioPoint next_point = path[i];
                    g.DrawLine(curPen, current_point.getScreenX(width), current_point.getScreenY(height), next_point.getScreenX(width), next_point.getScreenY(height));
                    current_point = next_point;
                }
                g.FillEllipse(curBrush, current_point.getScreenX(width) - height / 100, current_point.getScreenY(height) - height / 100, height / 50, height / 50);
                g.DrawString(tcur.CursorID + "", font, fntBrush, new PointF(tcur.getScreenX(width) - 10, tcur.getScreenY(height) - 10));
            }
        }
     }

        // draw the objects
        if (objectList.Count > 0) {
            lock(objectList) {
                foreach (TuioObject tobj in objectList.Values) {
                    int ox = tobj.getScreenX(width);
                    int oy = tobj.getScreenY(height);
                    int size = height / 10;

                    g.TranslateTransform(ox, oy);
                    g.RotateTransform((float)(tobj.Angle / Math.PI * 180.0f));
                    g.TranslateTransform(-ox, -oy);

                    g.FillRectangle(objBrush, new Rectangle(ox - size / 2, oy - size / 2, size, size));

                    g.TranslateTransform(ox, oy);
                    g.RotateTransform(-1 * (float)(tobj.Angle / Math.PI * 180.0f));
                    g.TranslateTransform(-ox, -oy);

                    g.DrawString(tobj.SymbolID + "", font, fntBrush, new PointF(ox - 10, oy - 10));
                }
            }
        }

        // draw the blobs
        if (blobList.Count > 0) {
            lock(blobList) {
                foreach (TuioBlob tblb in blobList.Values) {
                    int bx = tblb.getScreenX(width);
                    int by = tblb.getScreenY(height);
                    float bw = tblb.Width*width;
                    float bh = tblb.Height*height;

                    g.TranslateTransform(bx, by);
                    g.RotateTransform((float)(tblb.Angle / Math.PI * 180.0f));
                    g.TranslateTransform(-bx, -by);

                    g.FillEllipse(blbBrush, bx - bw / 2, by - bh / 2, bw, bh);

                    g.TranslateTransform(bx, by);
                    g.RotateTransform(-1 * (float)(tblb.Angle / Math.PI * 180.0f));
                    g.TranslateTransform(-bx, -by);

                    g.DrawString(tblb.BlobID + "", font, fntBrush, new PointF(bx, by));
                }
            }
        }*/
        Graphics g = pevent.Graphics;
        g.FillRectangle(bgrBrush, new Rectangle(0, 0, width, height));
        changeQuestionBackground(pevent);
        SolidBrush c1Brush = new SolidBrush(Color.Red);
        SolidBrush c2Brush = new SolidBrush(Color.Green);
        SolidBrush c3Brush = new SolidBrush(Color.Blue);
        SolidBrush c4Brush = new SolidBrush(Color.Olive);

        if (welcomeScreen)
        {
            var tuioObject = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 1);

            if (tuioObject != null)
            {
                // Convert the angle to degrees for easier reading
                float rotationAngleDegrees = (float)(tuioObject.Angle * (180.0 / Math.PI));

                // Draw the angle text on the screen
                g.DrawString("Angle: " + rotationAngleDegrees.ToString("0.0") + "°", font, fntBrush, new PointF(width / 2 - 100, height / 2 - 200));
            }
            if (objectList.Count > 0)
            {
                lock (objectList)
                {
                    foreach (TuioObject tobj in objectList.Values)
                    {
                        // Check if the TUIO object has SymbolID 1
                        if (tobj.SymbolID == 1)
                        {
                            // Get position and angle of the TUIO object
                            float ox = tobj.getScreenX(width);
                            float oy = tobj.getScreenY(height);
                            float angle = tobj.Angle; // Angle in radians

                            // Draw the arrow at the object's position
                            DrawArrow(pevent.Graphics, window_width / 2, window_height / 2, angle, 250); // Adjust size as needed
                        }
                    }
                }
            }


            // Draw the main "wheel" (central question)
            g.FillEllipse(c1Brush, width / 2 - 100, height / 2 - 50, 200, 100); // Central circle for the question
            if(questions.Count > 0)
            {
                g.DrawString(questions[0], font, fntBrush, new PointF((width - g.MeasureString(questions[0], font).Width) / 2, (height - g.MeasureString(questions[0], font).Height) / 2));
            }

            // Draw the four "choices" like wheel segments
            g.FillEllipse(c1Brush, 20, 20, 100, 60);  // Top-left
            g.FillEllipse(c2Brush, width - 120, 20, 100, 60);  // Top-right
            g.FillEllipse(c3Brush, 20, height - 80, 100, 60);  // Bottom-left
            g.FillEllipse(c4Brush, width - 120, height - 80, 100, 60);  // Bottom-right

            // Draw the text on the ellipses for choices
            if (answers.Count > 0)
            {
                g.DrawString(answers[0], font, fntBrush, new PointF(40, 40));  // Position for first choice
                g.DrawString(answers[1], font, fntBrush, new PointF(width - g.MeasureString(answers[1], font).Width - 40, 40));  // Second choice
                g.DrawString(answers[2], font, fntBrush, new PointF(40, height - g.MeasureString(answers[2], font).Height - 40));  // Third choice
                g.DrawString(answers[3], font, fntBrush, new PointF(width - g.MeasureString(answers[3], font).Width - 40, height - g.MeasureString(choiceFour, font).Height - 40));  // Fourth choice
            }

            checkCollisonTrue();

            // Check if the object with SymbolID == 1 exists in objectList
            
        }
        else
        {
            // Display the response after TUIO interaction
            g.DrawString(responseMessage, font, fntBrush, new PointF(width / 2 - 100, height / 2));
        }

    }

    private void changeQuestionBackground(PaintEventArgs pevent)
    {
        var marker1 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 1);
        Graphics g = pevent.Graphics;

        if (marker1 != null)
        {
            // Check if marker1's angle is within the range 5.23599 to 6.10865
            if (marker1.Angle >= 5.23599 && marker1.Angle <= 6.10865)
            {
                
                // Trigger background change or other actions
                g.DrawImage(Image.FromFile("cairo.png"), 0, 0, width, height);
            }
            else
            {
                
                // Optionally trigger different background or other actions
               // changeBackgroundImage("image2.png");
            }

            // Log or display the angle for debugging purposes
            Debug.WriteLine("Marker1 Angle: " + marker1.Angle);
        }
    }

    private void checkCollisonTrue()
    {
        double distanceThreshold = 0.30;

        // Get the objects for SymbolID 1 and 4
        var marker1 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 1);
        var marker4 = objectList.Values.FirstOrDefault(obj => obj.SymbolID == 4);

        if (marker4 != null && marker1 != null)
        {
            // Calculate the Euclidean distance between the two markers
            double distance = Math.Sqrt(Math.Pow(marker1.X - marker4.X, 2) + Math.Pow(marker1.Y - marker4.Y, 2));
            Debug.WriteLine("Distance: " + distance);

            // Check if the markers are close enough (within the threshold)
            if (distance <= distanceThreshold && marker1.Angle >= 5.23599 && marker1.Angle <= 6.10865)
            {
                responseMessage = "Ashter katkout";
                welcomeScreen = false;
            }
            else if (distance <= distanceThreshold && (marker1.Angle <= 5.23599 || marker1.Angle >= 6.10865))
            {
                responseMessage = "Stubiddd";
                welcomeScreen = false;
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
    private static string ReadMessage(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        // Send acknowledgment back to the client
        byte[] ack = Encoding.UTF8.GetBytes("ACK");
        stream.Write(ack, 0, ack.Length);

        return message;
    }
    private static void StartServer()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 12345);
        server.Start();
        Console.WriteLine("Server started...");

        TcpClient client = server.AcceptTcpClient();
        NetworkStream stream = client.GetStream();

        string question = ReadMessage(stream);
        if(question != null)
        {
            questions.Add(question);
            Debug.WriteLine("Question: " + questions[0]);
        }

        for (int i = 0; i < 4; i++)
        {
            string answer = ReadMessage(stream);
            if (answer != null)
            {
                answers.Add(answer);
            }
            Console.WriteLine("Answer " + (i + 1) + ": " + answers[i]);
        }

        for (int i = 0; i < 4; i++)
        {
            string imagePath = ReadMessage(stream);
            if (imagePath != null)
            {
                imagePaths.Add(imagePath);
            }
            Console.WriteLine("Image Path " + (i + 1) + ": " + imagePaths[i]);
        }

        client.Close();
        server.Stop();

    }

    public static void Main(String[] argv) {

        int port = 0;
			switch (argv.Length) {
				case 1:
					port = int.Parse(argv[0],null);
					if(port==0) goto default;
					break;
				case 0:
					port = 3333;
					break;
				default:
					Console.WriteLine("usage: mono TuioDemo [port]");
					System.Environment.Exit(0);
					break;
			}
			
		TuioDemo app = new TuioDemo(port);
        Thread systemThread = new Thread(StartServer);
        systemThread.Start();

        Application.Run(app);
		}
	}
