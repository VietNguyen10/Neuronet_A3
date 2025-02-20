﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using Accord;
using Accord.Math;
using Accord.Neuro;
using Accord.MachineLearning;
using Accord.Imaging;
using Accord.Genetic;
using System.Numerics;
using System.Net.Mail;

namespace NeuralNet
{
    /// <summary>
    /// Main_Form class
    /// Ties project logic into Main_Form GUI
    /// </summary>
    public partial class Main_Form : Form
    {
        //Counter for the number of child forms
        private int childFormNumber = 0;

        //Drawing variables
        private Bitmap drawnBitmap;
        private Graphics drawnGraphics;
        private bool isDrawing = false;
        private System.Drawing.Point previousPoint;
        readonly private Pen drawingPen = new Pen(Brushes.Black, 10);

        //Neural Network variables
        MNISTLoader mload = new MNISTLoader();
        TMNISTLoader tload = new TMNISTLoader();
        double[][] images = null;
        int[] labels = null;
        string[] labels_tmnist = null;

        private Activation.ActivationType currentActivation = Activation.ActivationType.Sigmoid;
        //NeuralNet nnMNIST;
        //NeuralNet nnMNIST = new NeuralNet(new int[]{ 784, 250, 100, 10 });
        NeuralNet nnMNIST = new NeuralNet(new int[] { 784, 100, 100, 10 });


        public Main_Form()
        {
            InitializeComponent();
            InitializeDrawingArea();

            toolStripStatusLabel.Text = "Current Activation: " + currentActivation.ToString();
        }

        private void InitializeDrawingArea()
        {
            drawnBitmap = new Bitmap(drawingArea.Width, drawingArea.Height);
            drawnGraphics = Graphics.FromImage(drawnBitmap);
            drawnGraphics.Clear(Color.White);
            drawingArea.Image = drawnBitmap;
            previousPoint = System.Drawing.Point.Empty;    
        }

        private void ShowNewForm(object sender, EventArgs e)
        {
            Form childForm = new Form();
            childForm.MdiParent = this;
            childForm.Text = "Window " + childFormNumber++;
            childForm.Show();
        }

        private void OpenFile(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = openFileDialog.FileName;
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = saveFileDialog.FileName;
            }
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Drawing area functions
        /// </summary>

        private void drawingArea_MouseDown(object sender, MouseEventArgs e)
        {
            isDrawing = true;
            previousPoint = e.Location;
        }

        private void drawingArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                // Calculate the circle coordinates
                int diameter = 2 * Math.Max(Math.Abs(e.X - previousPoint.X), Math.Abs(e.Y - previousPoint.Y));
                int x = e.X - diameter / 2;
                int y = e.Y - diameter / 2;

                using (Graphics bitmapGraphics = Graphics.FromImage(drawnBitmap))
                {
                    bitmapGraphics.DrawEllipse(drawingPen, x, y, diameter, diameter);
                }
                drawnGraphics.DrawEllipse(drawingPen, x, y, diameter, diameter);

                drawingArea.Invalidate();
                previousPoint = e.Location; 
            }
        }


        private void drawingArea_MouseUp(object sender, MouseEventArgs e)
        {
            isDrawing = false;
        }

        // Submits Drawing Area -- process to 28x28//
        private void submitDrawingBtn_Click(object sender, EventArgs e)
        {
            // Create a new Bitmap with size 28x28
            Bitmap resizedImage = new Bitmap(28, 28);
            float scaleX = (float)28 / drawnBitmap.Width;
            float scaleY = (float)28 / drawnBitmap.Height;


            // Get the graphics object of the resized image
            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                // Clear the resized image with white background
                g.Clear(Color.White);

                g.ScaleTransform(scaleX, scaleY);
                g.DrawImage(drawnBitmap, new PointF(0, 0));
            }

            // Display the resized image in the displayPictureBox
            if (displayArea.Image != null)
            {
                displayArea.Image.Dispose();
            }
            drawnBitmap = resizedImage;

            displayDrawing();
        }

        // Clears the drawing area//
        private void clearDrawingBtn_Click(object sender, EventArgs e)
        {
            //Clears previous drawing
            drawnBitmap = new Bitmap(drawingArea.Width, drawingArea.Height);
            drawnGraphics.Clear(Color.White);
            drawingArea.Refresh();
        }

        private void displayDrawing()
        {
            // Create a new bitmap for the scaled drawing
            Bitmap scaledImage = new Bitmap(280, 280);

            // Clear the bitmap
            using (Graphics clearGraphics = Graphics.FromImage(scaledImage))
            {
                clearGraphics.Clear(Color.White); // Assuming you want to clear with white color
            }

            // Calculate the scaling factor for width and height
            int scaleFactorX = 280 / 28;
            int scaleFactorY = 280 / 28;

            // Draw the scaled pixels onto the bitmap
            using (Graphics g = Graphics.FromImage(scaledImage))
            {
                for (int y = 0; y < 28; y++)
                {
                    for (int x = 0; x < 28; x++)
                    {
                        // Get the pixel color from the input drawing
                        Color pixelColor = drawnBitmap.GetPixel(x, y);

                        // Calculate grayscale intensity using luminance formula
                        int intensity = (int)(0.299 * pixelColor.R + 0.587 * pixelColor.G + 0.114 * pixelColor.B);

                        // Create grayscale color using calculated intensity
                        Color grayscaleColor = Color.FromArgb(intensity, intensity, intensity);

                        // Draw a scaled rectangle at the corresponding position
                        Rectangle rect = new Rectangle(x * scaleFactorX, y * scaleFactorY, scaleFactorX, scaleFactorY);
                        using (SolidBrush brush = new SolidBrush(grayscaleColor))
                        {
                            g.FillRectangle(brush, rect);
                        }
                    }
                }
            }
            displayArea.Image = scaledImage;
        }

        private void loadMNISTCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (images, labels) = mload.LoadMNISTCSV();
            MessageBox.Show("MNIST dataset loaded successfully.");
        }

        private void loadMNISTCSVtestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (images, labels) = mload.LoadMNISTCSV_test();
            MessageBox.Show("MNIST test loaded successfully.");
        }

        private void loadMNISTByteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (images, labels) = mload.LoadTrainingDataset();
            MessageBox.Show("MNIST dataset loaded successfully.");
        }

        private void loadMNISTBytetestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (images, labels) = mload.LoadTestingDataset();
            MessageBox.Show("MNIST test loaded successfully.");
        }

        private void loadTMNISTCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //(images, labels_tmnist) = tload.LoadTMNISTCSV();
            MessageBox.Show("TMNIST dataset is currently broken. DO NOT USE. :(");
        }

        private void sigmoidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentActivation = Activation.ActivationType.Sigmoid;
            toolStripStatusLabel.Text = "Current Activation: " + currentActivation.ToString();
        }

        private void tanHToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentActivation = Activation.ActivationType.TanH;
            toolStripStatusLabel.Text = "Current Activation: " + currentActivation.ToString();
        }

        private void reLUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentActivation = Activation.ActivationType.ReLU;
            toolStripStatusLabel.Text = "Current Activation: " + currentActivation.ToString();
        }

        private void siLUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentActivation = Activation.ActivationType.SiLU;
            toolStripStatusLabel.Text = "Current Activation: " + currentActivation.ToString();
        }


        private void useExistingWeightsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                nnMNIST.LoadWeightsFromFile("../../weights.txt");
                MessageBox.Show("Weights loaded successfully.");    
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading weights: " + ex.Message);
            }
        }

        private void createWeightsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                nnMNIST.SetRandomWeights();
                nnMNIST.SaveWeightsToFile("../../weights.txt");
                MessageBox.Show("Weights created successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating weights: " + ex.Message);
            }
        }

        private void showDisplayBox_CheckedChanged(object sender, EventArgs e)
        {
            groupDisplay.Visible = !groupDisplay.Visible;
        }

        //////////////////////////////Testing button items/////////////////////////////////

        private void testBtn_Click(object sender, EventArgs e)
        {
            nnMNIST = new NeuralNet(new int[] { 784, 100, 100, 10 });

            double[] inputImg = FlattenImage(images);
            nnMNIST.SetInput(inputImg);
            Console.WriteLine("Input set successfully.");
           
            nnMNIST.SetRandomWeights();
            Console.WriteLine("Weights Randomized successfully.");

            nnMNIST.SaveWeightsToFile("../../weights.txt");
            Console.WriteLine("Weights Saved successfully.");
            
            nnMNIST.LoadWeightsFromFile("../../weights.txt");
            Console.WriteLine("Weights loaded successfully.");
            
            nnMNIST.ForwardFeed();
            Console.WriteLine("Forward feed completed.");
            
            nnMNIST.BackPropagation(labels, 0.05);
            Console.WriteLine("Backpropagation completed.");
            
            nnMNIST.LoadWeightsFromFile("../../weights.txt");
            Console.WriteLine("New Weights Loaded");
        }


        // Function to flatten a 2D array into a 1D array
        private double[] FlattenImage(double[][] image)
        {
            int rows = image.Length;
            int cols = image[0].Length;
            double[] flattenedImage = new double[rows * cols];

            int index = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    flattenedImage[index++] = image[i][j];
                }
            }

            return flattenedImage;
        }

        private void whatPokemon_Click(object sender, EventArgs e)
        {
            int result = nnMNIST.GetMaxNeuronIndex(nnMNIST.numLayers - 1);

            // Display the result
            label1.Text = result.ToString();
            MessageBox.Show("The result is: " + result);
        }

        private double[] ConvertBitmapToInput(Bitmap bitmap)
        {
            // Convert bitmap to grayscale
            Bitmap grayscaleBitmap = ConvertToGrayscale(bitmap);

            // Resize bitmap to match the input size expected by the neural network
            Bitmap resizedBitmap = ResizeBitmap(grayscaleBitmap, 28, 28);

            // Flatten the resized bitmap into a 1D array of doubles
            double[] input = FlattenBitmap(resizedBitmap);

            return input;
        }

        private Bitmap ConvertToGrayscale(Bitmap bitmap)
        {
            // Convert the bitmap to grayscale
            // Implementation depends on the specific requirements and libraries used
            // Example:
            Bitmap grayscaleBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            for (int x = 0; x < grayscaleBitmap.Width; x++)
            {
                for (int y = 0; y < grayscaleBitmap.Height; y++)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    int grayValue = (int)(pixelColor.R * 0.3 + pixelColor.G * 0.59 + pixelColor.B * 0.11);
                    grayscaleBitmap.SetPixel(x, y, Color.FromArgb(grayValue, grayValue, grayValue));
                }
            }
            return grayscaleBitmap;
        }

        private Bitmap ResizeBitmap(Bitmap bitmap, int width, int height)
        {
            // Resize the bitmap to the specified width and height
            // Implementation depends on the specific requirements and libraries used
            // Example:
            Bitmap resizedBitmap = new Bitmap(bitmap, new Size(width, height));
            return resizedBitmap;
        }

        private double[] FlattenBitmap(Bitmap bitmap)
        {
            // Flatten the bitmap into a 1D array of doubles
            // Implementation depends on the specific requirements and libraries used
            // Example:
            double[] input = new double[bitmap.Width * bitmap.Height];
            int index = 0;
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    input[index++] = pixelColor.R / 255.0; // Normalize pixel value to range [0, 1]
                }
            }
            return input;

        }


    }
}
