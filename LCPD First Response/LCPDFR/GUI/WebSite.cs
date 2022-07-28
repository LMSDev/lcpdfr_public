namespace LCPD_First_Response.LCPDFR.GUI
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Threading;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;

    /// <summary>
    /// Represents a website that can be rendered onto the screen.
    /// </summary>
    internal class WebSite : IDisposable
    {
        /// <summary>
        /// The url.
        /// </summary>
        private string url;

        /// <summary>
        /// The resolution.
        /// </summary>
        private Size resolution;

        /// <summary>
        /// The texture of the website image.
        /// </summary>
        private Texture texture;

        /// <summary>
        /// The image of the website.
        /// </summary>
        private Image webSiteImage;

        /// <summary>
        /// The internal web browser.
        /// </summary>
        private WebBrowser webBrowser;

        /// <summary>
        /// Gets a value indicating whether the website has been loaded.
        /// </summary>
        public bool HasLoaded { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSite"/> class.
        /// </summary>
        /// <param name="url">
        /// The url.
        /// </param>
        /// <param name="resolution">
        /// The resolution.
        /// </param>
        public WebSite(string url, Size resolution)
        {
            this.url = url;
            this.resolution = resolution;
        }

        /// <summary>
        /// Gets the website as texture.
        /// </summary>
        public Texture Texture
        {
            get
            {
                return this.texture;
            }
        }

        /// <summary>
        /// Navigates to the website.
        /// </summary>
        public void Navigate()
        {
            var th = new Thread(() =>
            {
                if (this.webBrowser == null)
                {
                    this.webBrowser = new WebBrowser();
                    this.webBrowser.ScrollBarsEnabled = false;
                    int top = this.webBrowser.Top;
                    this.webBrowser.ScriptErrorsSuppressed = true;
                    this.webBrowser.Size = new Size(this.resolution.Width + 10, this.resolution.Height + 15);
                    this.webBrowser.DocumentCompleted += webBrowser_DocumentCompleted;
                }

                this.webBrowser.Navigate(this.url);
                Application.Run();
            });
            th.SetApartmentState(ApartmentState.STA);
            th.IsBackground = true;
            th.Start();
        }

        /// <summary>
        /// Disposes the website and all its resources.
        /// </summary>
        public void Dispose()
        {
            if (this.webBrowser != null)
            {
                this.webBrowser.Dispose();
                this.webBrowser = null;
            }

            if (this.webSiteImage != null)
            {
                this.webSiteImage.Dispose();
                this.webSiteImage = null;
            }

            if (this.texture != null)
            {
                this.texture.Dispose();
                this.texture = null;
            }
        }


        /// <summary>
        /// Called when the website has been loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Log.Debug("Loaded: " + e.Url + ". State: " +  this.webBrowser.StatusText + "(" + this.webBrowser.ReadyState.ToString() + ")", "WebSite");
            Bitmap bitmap = new Bitmap(this.webBrowser.DisplayRectangle.Width, this.webBrowser.DisplayRectangle.Height);
            this.webBrowser.DrawToBitmap(bitmap, new Rectangle(0, 0, this.webBrowser.DisplayRectangle.Width, this.webBrowser.DisplayRectangle.Height));
            this.webSiteImage = bitmap.Clone(new Rectangle(10, 15, this.resolution.Width, this.resolution.Height), PixelFormat.DontCare);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            this.webSiteImage.Save(ms, ImageFormat.Bmp);
            this.texture = new Texture(ms.ToArray());
            this.webSiteImage.Save("test.jpg", ImageFormat.Bmp);
            this.webBrowser.Dispose();
            this.webBrowser = null;
            Application.ExitThread();

            this.HasLoaded = true;
        }
    }
}