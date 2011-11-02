using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;

namespace BarcodeHttpHandler {

    public class BarcodeHandler : IHttpHandler {
        /// <summary>
        /// You will need to configure this handler in the web.config file of your
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>

        #region IHttpHandler Members

        public bool IsReusable {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        public void ProcessRequest(HttpContext context) {
            PrivateFontCollection pfc = new PrivateFontCollection();

            string code = context.Request.QueryString["Code"];
            if (String.IsNullOrEmpty(code)) throw new ArgumentNullException("Code");

            // Get embedded font resource stream
            Assembly aa = Assembly.GetExecutingAssembly();
            string[] resourceNames = aa.GetManifestResourceNames();
            Stream fontStream = aa.GetManifestResourceStream(resourceNames[0]);
            if (fontStream == null) throw new ArgumentNullException("fontStream");

            // Read font stream
            byte[] fontData = new Byte[fontStream.Length];
            fontStream.Read(fontData, 0, fontData.Length);
            fontStream.Close();

            // Pin array to get address
            GCHandle handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);

            try {
                IntPtr memIntPtr = Marshal.UnsafeAddrOfPinnedArrayElement(fontData, 0);
                if (memIntPtr == null) throw new ArgumentNullException("memIntPtr");

                // Add font to private collection and set font for use
                pfc.AddMemoryFont(memIntPtr, fontData.Length);
                Font barcode = new Font(pfc.Families[0], 44f);

                // Determine width
                Graphics gf = Graphics.FromImage(new Bitmap(100, 100));
                SizeF size = gf.MeasureString(String.Format("*{0}*", code), barcode);
                int width = (int)size.Width + 2;
                const int height = 70;

                using (Bitmap bm = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bm))
                using (MemoryStream ms = new MemoryStream()) {
                    Font text = new Font("Consolas", 12f);
                    PointF barcodeStart = new PointF(2f, 2f);
                    SolidBrush black = new SolidBrush(Color.Black);
                    SolidBrush white = new SolidBrush(Color.White);
                    g.FillRectangle(white, 0, 0, width, height);
                    g.DrawString(String.Format("*{0}*", code), barcode, black, barcodeStart);
                    g.DrawString(code, text, black, new Rectangle(0, 46, width, 20), new StringFormat() { Alignment = StringAlignment.Center });
                    context.Response.ContentType = "image/png";
                    bm.Save(ms, ImageFormat.Png);
                    ms.WriteTo(context.Response.OutputStream);
                }
            } finally {
                // Don't forget to unpin the array!
                handle.Free();
            }
        }

        #endregion IHttpHandler Members

        public string Properties { get; set; }
    }
}