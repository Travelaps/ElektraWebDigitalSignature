using System.Windows.Forms;

namespace DemoButtons
{
  static class Program
  {
    [System.STAThread]
    static void Main()
    {
      System.Windows.Forms.Application.EnableVisualStyles();
      System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
      Control.CheckForIllegalCrossThreadCalls = false; //thread çakışmalarını önlemek için
      System.Windows.Forms.Application.Run(new FrmMain());      
    }
  }
}
