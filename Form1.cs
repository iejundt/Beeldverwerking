﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Vector = System.Windows.Vector;

namespace INFOIBV
{
	public partial class INFOIBV : Form
	{
		private Bitmap InputImage;
		private Bitmap OutputImage;

		private int Width, Height;

		public INFOIBV()
		{
			InitializeComponent();
		}

		private void LoadImageButton_Click(object sender, EventArgs e)
		{
			if (openImageDialog.ShowDialog() == DialogResult.OK)             // Open File Dialog
			{
				string file = openImageDialog.FileName;                     // Get the file name
				imageFileName.Text = file;                                  // Show file name
				if (InputImage != null) InputImage.Dispose();               // Reset image
				InputImage = new Bitmap(file);                              // Create new Bitmap from file
				if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
					InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // Dimension check
					MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
				else
					pictureBox1.Image = (Image)InputImage;                 // Display input image
			}
		}

		private void applyButton_Click(object sender, EventArgs e)
		{
			if (InputImage == null) return;                                 // Get out if no input image
			if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
			OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
			Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // Create array to speed-up operations (Bitmap functions are very slow)

			// Setup progress bar
			progressBar.Visible = true;
			progressBar.Minimum = 1;
			progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
			progressBar.Value = 1;
			progressBar.Step = 1;

			// Copy input Bitmap to array            
			for (int x = 0; x < InputImage.Size.Width; x++)
			{
				for (int y = 0; y < InputImage.Size.Height; y++)
				{
					Image[x, y] = InputImage.GetPixel(x, y);                // Set pixel color in array at (x,y)
				}
			}

			//==========================================================================================

			Width = InputImage.Size.Width;
			Height = InputImage.Size.Height;

			Color[,] grayscaleImage = ConvertToGrayscale(Image);

			float[,] floatImage = ConvertToFloat(grayscaleImage);

			float[,] edgeImage = DetectEdges(floatImage);

			float[,] thresholdImage = ApplyThreshold(edgeImage, 10);

            float[,] morphedImage = MorphologycalTransform(thresholdImage);

			Detection[] detectedObjects = FloodFillExtraction(thresholdImage);

			Detection[] filteredObjects = FilterBySize(detectedObjects, 32);

			// TODO: Normalize shape curve

			// TODO: Compare curve with reference (which needs to be constructed)

			// TODO: Show detections on original image

			float[,] normalizedImage = NormalizeFloats(morphedImage);
			Image = ConvertToImage(normalizedImage);

			//==========================================================================================

			// Copy array to output Bitmap
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					OutputImage.SetPixel(x, y, Image[x, y]);               // Set the pixel color at coordinate (x,y)
				}
			}

			pictureBox2.Image = (Image)OutputImage;                         // Display output image
			progressBar.Visible = false;                                    // Hide progress bar
		}

		private void saveButton_Click(object sender, EventArgs e)
		{
			if (OutputImage == null) return;                                // Get out if no output image
			if (saveImageDialog.ShowDialog() == DialogResult.OK)
				OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
		}

		// ===== PROCESSING FUNCTIONS =====

		private float[,] NormalizeFloats(float[,] input)
		{
			float[,] output = new float[Width, Height];
			progressBar.Value = progressBar.Minimum;

			float MIN = float.MaxValue, MAX = float.MinValue;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = input[x, y];

					if (value < MIN)
						MIN = value;
					if (value > MAX)
						MAX = value;

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = input[x, y];

					if (MAX - MIN == 0) // Catch devide-by-zero
						throw new Exception("SHIT");

					value = (value - MIN) / (MAX - MIN);

					if (value < 0 || value > 1 || float.IsNaN(value)) // Still something went wrong
						throw new Exception("ALSO SHIT");

					output[x, y] = value;

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private Color[,] ConvertToImage(float[,] input)
		{
			Color[,] output = new Color[Width, Height];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = input[x, y];
					int grayValue = (int)Math.Round(value * 255);

					output[x, y] = Color.FromArgb(grayValue, grayValue, grayValue);

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private Color[,] ConvertToGrayscale(Color[,] input)
		{
			Color[,] output = new Color[Width, Height];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					Color inputColor = input[x, y];
					int grayscale = (int)(inputColor.R * 0.3 + inputColor.G * 0.59 + inputColor.B * 0.11); // Calculate grayscale value
					Color outputColor = Color.FromArgb(grayscale, grayscale, grayscale);
					output[x, y] = outputColor;

					progressBar.PerformStep();
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private float[,] ConvertToFloat(Color[,] input)
		{
			float[,] output = new float[Width, Height];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = input[x, y].R; // Calculate grayscale value
					output[x, y] = value; // Save to output

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		public float[,] DetectEdges(float[,] input)
		{
			float[,] output = new float[Width, Height];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = 0;

					if (0 < x && x < Width - 1)
					{
						value += Math.Abs((-input[x - 1, y] + input[x + 1, y]) / 3f); // Horizontal (-1, 0, 1) kernel
					}
					if (0 < y && y < Height - 1)
					{
						value += Math.Abs((-input[x, y - 1] + input[x, y + 1]) / 3f); // Vertical (-1, 0, 1) kernel
					}

					output[x, y] = value;

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private float[,] ApplyThreshold(float[,] input, float threshold)
		{
			float[,] output = new float[Width, Height];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					if (input[x, y] > threshold)
						output[x, y] = 1;
					else
						output[x, y] = 0;

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

        private float[,] MorphologycalTransform(float[,] input)
        {
            //float[,] temp = Erosion(input, 3);
            float[,] output = Dilation(input,4);
            //output = Erosion(output, 2);       
            
            return output;
        }

        private float[,] Erosion(float[,] input, int strength)
        {
            float[,] output = new float[Width, Height];

            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    int count = 0;

                    if (input[x, y] == 1)                   // + shape erosion 3x3
                    {
                        if (input[x - 1, y] == 1)
                            count++;
                        if (input[x + 1, y] == 1)
                            count++;
                        if (input[x, y - 1] == 1)
                            count++;
                        if (input[x, y + 1] == 1)
                            count++;

                        if (count > strength)
                        {
                            output[x, y] = 1;
                        }
                        else output[x, y] = 0;
                    }
                    else output[x, y] = 0;

                    
                }
            }
                    return output;
        }

        private float[,] Dilation(float[,] input, int strength)
        {
            float[,] output = new float[Width, Height];

            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    int count = 0;
                    if (input[x, y] == 0)
                        output[x, y] = 0;
                    else
                    {                                       // Square shape dilation 3x3
                        if (input[x - 1, y] == 0)
                            count++;
                        if (input[x + 1, y] == 0)
                            count++;
                        if (input[x, y - 1] == 0)
                            count++;
                        if (input[x, y + 1] == 0)
                            count++;
                        if (input[x - 1, y-1] == 0)
                            count++;
                        if (input[x + 1, y +1] == 0)
                            count++;
                        if (input[x-1, y - 1] == 0)
                            count++;
                        if (input[x+1, y + 1] == 0)
                            count++;

                        if (count > strength)
                        {
                            output[x, y] = 0;
                        }
                        else output[x, y] = 1;
                    }

                    
                }
            }

            return output;
        }


        private Detection[] FloodFillExtraction(float[,] input)
		{
			// STAGE 0: Copy the input to an array that can be manipulated
			int[,] flood = new int[Width, Height];
			for (int x = 0; x < Width; x++)
				for (int y = 0; y < Height; y++)
					flood[x, y] = (int)input[x, y]; // Copy the input to an array we can process

			progressBar.Value = progressBar.Minimum;

			// STAGE 1: Use flood fill to find all objects and assign an identifier to all their pixels
			int ObjectIdentifier = 2; // At this stage, 0 should represent an object and 1 should represent an edge.
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					if (flood[x, y] == 0) // Discovered new object
					{
						Queue<Point> work = new Queue<Point>(); // Keep track of BFS frontier
						work.Enqueue(new Point(x, y)); // Start with the point we just found

						while (work.Count > 0) // Continue until every pixel of the object has been processed
						{
							Point p = work.Dequeue();

							flood[p.X, p.Y] = ObjectIdentifier;

							if (p.X > 0 && flood[p.X - 1, p.Y] == 0)
							{
								work.Enqueue(new Point(p.X - 1, p.Y));
								flood[p.X - 1, p.Y] = ObjectIdentifier;
							}
							if (p.X < Width - 1 && flood[p.X + 1, p.Y] == 0)
							{
								work.Enqueue(new Point(p.X + 1, p.Y));
								flood[p.X + 1, p.Y] = ObjectIdentifier;
							}
							if (p.Y > 0 && flood[p.X, p.Y - 1] == 0)
							{
								work.Enqueue(new Point(p.X, p.Y - 1));
								flood[p.X, p.Y - 1] = ObjectIdentifier;
							}
							if (p.Y < Height - 1 && flood[p.X, p.Y + 1] == 0)
							{
								work.Enqueue(new Point(p.X, p.Y + 1));
								flood[p.X, p.Y + 1] = ObjectIdentifier;
							}
						}

						ObjectIdentifier++; // Increase the counter for the next detection
					}

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;

			// STAGE 2: Group all pixels of each object
			List<List<Point>> extract = new List<List<Point>>();
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					if (flood[x, y] > 1) // Part of object found
					{
						int Identifier = flood[x, y] - 2;

						if (extract.Count < Identifier) // Should never happen
							throw new Exception("SHIT");

						if (extract.Count == Identifier) // Object not yet stored
							extract.Add(new List<Point>());

						extract[Identifier].Add(new Point(x, y)); // Add point to the correct object
					}

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			// STAGE 3: Reconstruct a detected object from its pixels.
			Detection[] output = new Detection[extract.Count];
			for (int i = 0; i < extract.Count; i++)
				output[i] = new Detection(extract[i].ToArray());

			progressBar.Value = progressBar.Minimum;
			return output.ToArray();
		}

		private Detection[] FilterBySize(Detection[] input, int MinPixels)
		{
			List<Detection> output = new List<Detection>();

			foreach (Detection obj in input)
				if (obj.Size > MinPixels * MinPixels && obj.Size < (Width / 2) * (Height / 2))   //Filter out all objects with a surface smaller than the minimal pixelsize squared 
					output.Add(obj);                            

			return output.ToArray(); ;        //Return a new (smaller) array with the objects within the correct size range
		}

        private Detection[] FindTargetObjects(Detection[] input, float[] curve , float threshold)
        {
            List<Detection> outputList = new List<Detection>();
            foreach( Detection d in input)
            {
                if(CompareCurve(d.BoundaryCurve, curve) <= threshold)
                {
                    outputList.Add(d);
                }
            }
            return outputList.ToArray();
        }

        private float CompareCurve(float[] detection, float[] curve)
        {
            float minSqauredDifferenceSum = float.MaxValue;     //Float to store the smallest difference, set to highest value so any value smaller will replace this

            for (int o = 0; o < 360; o++)       //Loop over all angles of rotation to compare to the predefined curve
            {
                float squaredDifferenceSum = 0.0f;
                for (int i = o; i < 360 + o; i++)    //Use the angle of rotation as an offset when looping through the arrays
                {
                    int index = o;
                    if (i > 360)
                        o -= 360;    //If i becomes larger then 360

                    float difference = detection[o] - curve[o];
                    squaredDifferenceSum += difference * difference; //Add the squared difference to the sum
                }
                if (squaredDifferenceSum < minSqauredDifferenceSum) //Replace the minimal squared difference when necessary
                    minSqauredDifferenceSum = squaredDifferenceSum;
            }
            return minSqauredDifferenceSum;
        }
	}

	class Detection
	{
		public Point[] Points
		{
			get; private set;
		}

		public int Size
		{
			get; private set;
		}
		public int Left
		{
			get; private set;
		}
		public int Right
		{
			get; private set;
		}
		public int Top
		{
			get; private set;
		}
		public int Bottom
		{
			get; private set;
		}

		private PointF _center;
		public PointF Center
		{
			get
			{
				if (_center == null)
					CalculateCenter();
				return _center;
			}

			private set
			{
				_center = value;
			}
		}

		private float[] _boundarycurve;
		public float[] BoundaryCurve
		{
			get
			{
				if (_boundarycurve == null)
					CalculateBoundary();
				return _boundarycurve;
			}

			private set
			{
				_boundarycurve = value;
			}
		}

		public Detection(Point[] points)
		{
			Points = points;
			Size = points.Count();
			CalculateBoundingBox();
		}

		private void CalculateBoundingBox()
		{
			Left = int.MaxValue;
			Right = int.MinValue;
			Top = int.MinValue;
			Bottom = int.MaxValue;

			foreach(Point p in Points)
			{
				if (p.X < Left)
					Left = p.X;
				if (p.X > Right)
					Right = p.X;
				if (p.Y > Top)
					Top = p.Y;
				if (p.Y < Bottom)
					Bottom = p.Y;
			}
		}

		private void CalculateCenter()
		{
            float totalX = 0.0f;
            float totalY = 0.0f;

            for(int i = 0; i < Size; i++)
            {
                totalX += Points[i].X;
                totalY += Points[i].Y;
            }

            PointF center = new PointF(totalX / Size, totalY / Size);
		}

		private void CalculateBoundary()
		{
			// Initialize constant
			const int STEPS = 360;
			const double INTERVAL = 360.0 / STEPS;

			// Create a temporary bool[,] representation of the object
			int Width = Right - Left, Height = Bottom - Top;
			bool[,] workspace = new bool[Width, Height];
			foreach (Point p in Points)
				workspace[p.X - Left, p.Y - Top] = true;

			BoundaryCurve = new float[STEPS]; // Initialize an array to store the results
			Vector CenterVec = new Vector(Center.X - Left, Center.Y - Top); // Calculate the center of the workspace

			for(int i = 0; i < STEPS; i++) // Loop over the requested number of directions
			{
				// Construct a vector to walk over the workspace with
				double degrees = i * INTERVAL;
				double radians = degrees * Math.PI / 180;
				Vector vec = new Vector(Math.Cos(radians), Math.Sin(radians)) / 2;
				Vector pos = CenterVec; // Set the starting position of the walker

				while (pos.X > 0 && pos.X < Width && pos.Y > 0 && pos.Y < Height) // Keep walking until we run outside the workspace
				{
					int x = (int)Math.Round(pos.X);
					int y = (int)Math.Round(pos.Y);

					if (workspace[x, y]) // If our current position is part of the object
						// Overwrite any previous value, which means that we detect the outside edges
						// The default value is 0, which means that no detection defaults to 0
						BoundaryCurve[i] = (float)(new Vector(pos.X - CenterVec.X, pos.Y - CenterVec.Y)).Length;

					pos += vec; // Move the walker
				}
			}
		}
	}
}