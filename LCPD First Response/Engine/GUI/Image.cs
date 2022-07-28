namespace LCPD_First_Response.Engine.GUI
{
    using System;
    using System.Drawing;
    using System.IO;

    using GTA;

    /// <summary>
    /// Represents an image drawn on the screen.
    /// </summary>
    internal class Image : IDisposable
    {
        /// <summary>
        /// The height.
        /// </summary>
        private int height;

        /// <summary>
        /// The width.
        /// </summary>
        private int width;

        /// <summary>
        /// The X position.
        /// </summary>
        private int x;

        /// <summary>
        /// The Y position;
        /// </summary>
        private int y;

        /// <summary>
        /// The drawing rectangle.
        /// </summary>
        private Rectangle rectangle;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture texture;

        /// <summary>
        /// Gets an empty image representation.
        /// </summary>
        public static Image EmptyImage
        {
            get
            {
                Image image = new Image();
                byte[] data = new byte[]
                                  {
                                      66, 77, 58, 0, 0, 0, 0, 0, 0, 0, 54, 0, 0, 0, 40, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1
                                      , 0, 24, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                      204, 72, 63, 0
                                  };
                image.texture = new Texture(data);
                return image;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class. The image will be drawn on the screen automatically.
        /// </summary>
        /// <param name="name">
        /// The name of the file.
        /// </param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="x">The X position of the image.</param>
        /// <param name="y">The Y position of the image.</param>
        public Image(string name, int width, int height, int x, int y)
        {
            // Hook up events
            LCPDFR_Loader.PublicScript.PerFrameDrawing += new GTA.GraphicsEventHandler(this.PublicScript_PerFrameDrawing);

            if (!File.Exists(name))
            {
                throw new FileNotFoundException(name + " not found.");
            }

            this.Color = Color.White;
            this.width = width;
            this.height = height;
            this.x = x;
            this.y = y;
            this.RecalculateRectangle();

            // Load texture
            byte[] data = File.ReadAllBytes(name);
            this.texture = new Texture(data);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="name">
        /// The name of the file.
        /// </param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="x">The X position of the image.</param>
        /// <param name="y">The Y position of the image.</param>
        /// <param name="drawAutomatically">Whether the image is drawn automatically.</param>
        public Image(string name, int width, int height, int x, int y, bool drawAutomatically)
        {
            // Hook up events
            if (drawAutomatically)
            {
                LCPDFR_Loader.PublicScript.PerFrameDrawing += new GTA.GraphicsEventHandler(this.PublicScript_PerFrameDrawing);
            }

            if (!File.Exists(name))
            {
                throw new FileNotFoundException(name + " not found.");
            }

            this.Color = Color.White;
            this.width = width;
            this.height = height;
            this.x = x;
            this.y = y;
            this.RecalculateRectangle();

            // Load texture
            byte[] data = File.ReadAllBytes(name);
            this.texture = new Texture(data);
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="Image"/> class from being created.
        /// </summary>
        private Image()
        {
        }

        /// <summary>
        /// Gets or sets the drawing color.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets the height of the image.
        /// </summary>
        public int Height
        {
            get
            {
                return this.height;
            }

            set
            {
                this.height = value;
                this.RecalculateRectangle();
            }
        }

        /// <summary>
        /// Gets or sets the width of the image.
        /// </summary>
        public int Width
        {
            get
            {
                return this.width;
            }

            set
            {
                this.width = value;
                this.RecalculateRectangle();
            }
        }

        /// <summary>
        /// Gets or sets the X position of the image.
        /// </summary>
        public int X
        {
            get
            {
                return this.x;
            }

            set
            {
                this.x = value;
                this.RecalculateRectangle();
            }
        }

        /// <summary>
        /// Gets or sets the Y position of the image.
        /// </summary>
        public int Y
        {
            get
            {
                return this.y;
            }

            set
            {
                this.y = value;
                this.RecalculateRectangle();
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="Image"/> from <paramref name="resourceName"/>.
        /// </summary>
        /// <param name="resourceName">The name of the resource file.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="x">The X position of the image.</param>
        /// <param name="y">The Y position of the image.</param>
        /// <param name="drawAutomatically">Whether the image is drawn automatically.</param>
        /// <param name="nameSpace">The namespace where the resource is defined.</param>
        /// <returns>A new instance of <see cref="Image"/>.</returns>
        public static Image FromResource(string resourceName, int width, int height, int x, int y, bool drawAutomatically, Type nameSpace)
        {
            Image image = new Image { Color = Color.White, width = width, height = height, x = x, y = y };
            image.RecalculateRectangle();

            // Load texture
            byte[] data = ResourceHelper.GetResourceBytes(resourceName, nameSpace);
            image.texture = new Texture(data);

            return image;
        }

        /// <summary>
        /// Release all resources.
        /// </summary>
        public void Dispose()
        {
            LCPDFR_Loader.PublicScript.PerFrameDrawing -= new GTA.GraphicsEventHandler(this.PublicScript_PerFrameDrawing);
            this.texture.Dispose();
        }

        /// <summary>
        /// Draws the image on <paramref name="graphics"/>.
        /// </summary>
        /// <param name="graphics">The graphics instance.</param>
        public void Draw(GTA.Graphics graphics)
        {
            graphics.DrawSprite(this.texture, this.rectangle, this.Color);
        }

        /// <summary>
        /// Called when drawing on screen.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The graphics event arguments.</param>
        private void PublicScript_PerFrameDrawing(object sender, GraphicsEventArgs e)
        {
            this.Draw(e.Graphics);
        }

        /// <summary>
        /// Calculates the rectangle using Height, Widht, X and Y and updates it.
        /// </summary>
        private void RecalculateRectangle()
        {
            this.rectangle = new Rectangle(this.X, this.Y, this.Width, this.Height);
        }
    }
}
