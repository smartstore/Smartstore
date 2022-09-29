namespace Smartstore.WebApi.Client.Utilities
{
    public class HourGlass : IDisposable
    {
        public HourGlass()
        {
            Enabled = true;
        }
        public void Dispose()
        {
            Enabled = false;
        }
        public static bool Enabled
        {
            get => Application.UseWaitCursor;
            set
            {
                if (value == Application.UseWaitCursor)
                {
                    return;
                }

                Application.UseWaitCursor = value;
                Form f = Form.ActiveForm;
                if (f != null)   // Send WM_SETCURSOR
                {
                    SendMessage(f.Handle, 0x20, f.Handle, (IntPtr)1);
                }
            }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    }
}
