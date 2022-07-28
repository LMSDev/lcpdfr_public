namespace LCPD_First_Response.Engine.GUI
{
    using System.Drawing;

    using LCPD_First_Response.Engine.Scripting.Native;

    /// <summary>
    /// Provides functions accessing the in-game GUI
    /// </summary>
    internal class Gui
    {
        /// <summary>
        /// Whether text input is active.
        /// </summary>
        private static bool textInputActive;

        /// <summary>
        /// Initializes static members of the <see cref="Gui"/> class.
        /// </summary>
        static Gui()
        {
            LCPDFR_Loader.PublicScript.PerFrameDrawing += PublicScript_PerFrameDrawing;
        }

        /// <summary>
        /// The PerFrameDrawing delegate.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The graphics event.</param>
        public delegate void PerFrameDrawingEventHandler(object sender, GTA.GraphicsEventArgs e);

        /// <summary>
        /// Called every frame to draw on top of the rendered D3D frame.
        /// </summary>
        public static event PerFrameDrawingEventHandler PerFrameDrawing;

        /// <summary>
        /// Sets a value indicating whether the camera can be controlled.
        /// </summary>
        public static bool CameraControlsActive
        {
            set
            {
                Natives.SetGameCameraControlsActive(value);
            }
        }

        /// <summary>
        /// Gets the screen resolution.
        /// </summary>
        public static Size Resolution
        {
            get
            {
                return GTA.Game.Resolution;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the HUD is displayed.
        /// </summary>
        public static bool DisplayHUD
        {
            set
            {
                Natives.DisplayHUD(value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether text input is active.
        /// </summary>
        public static bool TextInputActive
        {
            get
            {
                return textInputActive || AdvancedHookManaged.AGame.GetIsTextInputActive();
            }

            set
            {
                textInputActive = value;
                Natives.SetTextInputActive(value);
            }
        }

        /// <summary>
        /// Sets the radar zoom. 0 is default (actually 190), 1 is maximum zoom and 960 is default zoom out (when holding T). 
        /// The bigger the value (without 0) the further away the map appears.
        /// </summary>
        public static int RadarZoom
        {
            set
            {
                Natives.SetRadarZoom(value);
            }
        }

        /// <summary>
        /// Adds the text to the news scrollbar.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void AddStringToNewsScrollBar(string text)
        {
            Natives.AddStringToNewsScrollBar(text);
        }

        /// <summary>
        /// Draws a coloured cylinder. Has to be called in loop in order for the arrow to appear.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="color">The color.</param>
        public static void DrawColouredCylinder(GTA.Vector3 position, System.Drawing.Color color)
        {
            Natives.DrawColouredCylinder(position, 0, 0, color);
        }

        /// <summary>
        /// Draws a corona light. Has to be called in a loop.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="size">The size.</param>
        /// <param name="color">The color.</param>
        public static void DrawCorona(GTA.Vector3 position, float size, Color color)
        {
            Natives.DrawCorona(position, size, 1, 1f, color);
        }

        /// <summary>
        /// Draws a rectangle with the center at <paramref name="x"/> and <paramref name="y"/>.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="color">The color.</param>
        public static void DrawRect(float x, float y, float width, float height, Color color)
        {
            float realX = x / GTA.Game.Resolution.Width;
            float realY = y / GTA.Game.Resolution.Height;
            float realWidth = width / GTA.Game.Resolution.Width;
            float realHeight = height / GTA.Game.Resolution.Height;

            Natives.DrawRect(realX, realY, realWidth, realHeight, color);
        }

        /// <summary>
        /// Draws a rectangle using relative position as center and size (0-1).
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="color">The color.</param>
        public static void DrawRectRelative(float x, float y, float width, float height, Color color)
        {
            Natives.DrawRect(x, y, width, height, color);
        }

        /// <summary>
        /// Draws a rectangle using relative position (0-1) as center and fixed size.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="color">The color.</param>
        public static void DrawRectPositionRelative(float x, float y, float width, float height, Color color)
        {
            float realWidth = width / GTA.Game.Resolution.Width;
            float realHeight = height / GTA.Game.Resolution.Height;

            Natives.DrawRect(x, y, realWidth, realHeight, color);
        }

        /// <summary>
        /// Returns the screen position for <paramref name="worldPosition"/> in <paramref name="viewportID"/>.
        /// </summary>
        /// <param name="worldPosition">The position in the viewport.</param>
        /// <param name="viewportID">The ID of the viewport.</param>
        /// <param name="screenPosition">The position on screen (Out).</param>
        /// <returns>Boolean value.</returns>
        public static bool GetScreenPositionFromWorldPosition(GTA.Vector3 worldPosition, EViewportID viewportID, out GTA.Vector2 screenPosition)
        {
            return Natives.GetViewportPositionOfCoord(worldPosition, viewportID, out screenPosition);
        }

        /// <summary>
        /// Returns the relative screen position for <paramref name="worldPosition"/> in <paramref name="viewportID"/>.
        /// </summary>
        /// <param name="worldPosition">The position in the viewport.</param>
        /// <param name="viewportID">The ID of the viewport.</param>
        /// <param name="screenPosition">The position on screen (Out).</param>
        /// <returns>Boolean value.</returns>
        public static bool GetRelativeScreenPositionFromWorldPosition(GTA.Vector3 worldPosition, EViewportID viewportID, out GTA.Vector2 screenPosition)
        {
            bool ret = Natives.GetViewportPositionOfCoord(worldPosition, viewportID, out screenPosition);
            float relativeX = screenPosition.X / GTA.Game.Resolution.Width;
            float relativeY = screenPosition.Y / GTA.Game.Resolution.Height;
            screenPosition.X = relativeX;
            screenPosition.Y = relativeY;

            return ret;
        }

        /// <summary>
        /// Returns whether <paramref name="worldPosition"/> is on screen. Doesn't do any ray tracing checks, just uses raw screen logic.
        /// </summary>
        /// <param name="worldPosition">The world position.</param>
        /// <returns>True if on screen, false otherwise.</returns>
        public static bool IsPositionOnScreen(GTA.Vector3 worldPosition)
        {
            GTA.Vector2 screenPos = new GTA.Vector2(0, 0);
            GetRelativeScreenPositionFromWorldPosition(worldPosition, EViewportID.CViewportGame, out screenPos);
            if (screenPos.X < 0.0 || screenPos.X > 1.0 || screenPos.Y < 0.0 || screenPos.Y > 1.0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prints <paramref name="text"/> in the lower center of the screen.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="duration">The duration.</param>
        public static void PrintText(string text, int duration)
        {
            Natives.PrintStringWithLiteralStringNow(text, duration);
        }

        /// <summary>
        /// Called every frame to draw on top of the rendered D3D frame.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The graphics event.</param>
        private static void PublicScript_PerFrameDrawing(object sender, GTA.GraphicsEventArgs e)
        {
            if (PerFrameDrawing != null)
            {
                PerFrameDrawing(sender, e);
            }
        }
    }
}
