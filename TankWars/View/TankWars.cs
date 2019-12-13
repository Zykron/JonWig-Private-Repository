//Authors: Ben Huenemann and Jonathan Wigderson

using System;
using System.Drawing;
using System.Windows.Forms;
namespace TankWars
{
    public partial class TankWars : Form
    {
        //Stores a controller and drawing pannel
        private GameController TheController;
        DrawingPanel drawingPanel;

        //Delegate for setting the connect button
        delegate void SetConnectButtonCallback(bool state);


        /// <summary>
        /// Constructor that sets up the form when given a game controller
        /// </summary>
        /// <param name="ctl">controller that the form should use</param>
        public TankWars(GameController ctl)
        {
            InitializeComponent();
            TheController = ctl;

            //Adds the on frame event listener and the error event listener
            TheController.OnFrameEvent += OnFrame;
            TheController.ErrorEvent += DisplayError;

            //Creates a drawing panel in the form
            ClientSize = new Size(Constants.ViewSize, Constants.ViewSize);
            drawingPanel = new DrawingPanel(TheController);
            drawingPanel.BackColor = Color.Black;
            drawingPanel.Location = new Point(Constants.ViewLocationX, Constants.ViewLocationY);
            drawingPanel.Size = new Size(ClientSize.Width, ClientSize.Height);
            Controls.Add(drawingPanel);

            //Allows mouse clicks to be detected on drawing panel
            drawingPanel.MouseDown += TankWars_MouseDown;
            drawingPanel.MouseUp += TankWars_MouseUp;
            drawingPanel.MouseMove += TankWars_MouseMove;
        }


        /// <summary>
        /// Method that should be called every frame to invalidate the form and force a redraw
        /// </summary>
        private void OnFrame()
        {
            // Don't try to redraw if the window doesn't exist yet.
            // This might happen if the controller sends an update
            // before the Form has started.
            if (!IsHandleCreated)
                return;

            // Invalidate this form and all its children
            // This will cause the form to redraw as soon as it can
            MethodInvoker m = new MethodInvoker(() => Invalidate(true));
            Invoke(m);
        }


        /// <summary>
        /// Displays an error message on the client if an error has occurred.
        /// </summary>
        /// <param name="errorMessage"></param>
        private void DisplayError(string errorMessage)
        {
            SetConnectButton(true);
            MessageBox.Show(errorMessage, "Connection Error", MessageBoxButtons.OK);
        }


        /// <summary>
        /// Connection button on the client
        /// </summary>
        /// <param name="state">The state to set the connect button to</param>
        private void SetConnectButton(bool state)
        {
            /* InvokeRequired required compares the thread ID of the
            calling thread to the thread ID of the creating thread.
            If these threads are different, it returns true. This
            code was found online since a method invoker doesn't work
            for every thread. */
            if (ConnectButton.InvokeRequired)
            {
                SetConnectButtonCallback d = new SetConnectButtonCallback(SetConnectButton);
                Invoke(d, new object[] { state });
            }
            else
            {
                ConnectButton.Enabled = state;
            }
        }


        /// <summary>
        /// Starts the connection and disables the connect button when the connect button is pressed
        /// </summary>
        private void ConnectButton_Click(object sender, EventArgs e)
        {
            ConnectButton.Enabled = false;
            TheController.TryConnect(NameInput.Text, ServerInput.Text, 11000);
        }
        

        /// <summary>
        /// Tracks when a key is being pushed down
        /// </summary>
        private void TankWars_KeyDown(object sender, KeyEventArgs e)
        {
            TheController.ProcessKeyDown(e.KeyCode);
        }


        /// <summary>
        /// Tracks when a key has been picked up
        /// </summary>
        private void TankWars_KeyUp(object sender, KeyEventArgs e)
        {
            TheController.ProcessKeyUp(e.KeyCode);
        }


        /// <summary>
        /// Tracks when a mouse button has been pressed down
        /// </summary>
        private void TankWars_MouseDown(object sender, MouseEventArgs e)
        {
            TheController.ProcessMouseDown(e.Button);
        }


        /// <summary>
        /// Tracks when a mouse button has been lifted up
        /// </summary>
        private void TankWars_MouseUp(object sender, MouseEventArgs e)
        {
            TheController.ProcessMouseUp();
        }


        /// <summary>
        /// Tracks location of the mouse
        /// </summary>
        private void TankWars_MouseMove(object sender, MouseEventArgs e)
        {
            if(TheController.wallsDone)
                TheController.ProcessMouseMove(e.X, e.Y);
        }
    }
}