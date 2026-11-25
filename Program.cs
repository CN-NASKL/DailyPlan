using System;
using System.Windows.Forms;
using DailyPlan.Core;

namespace DailyPlan;

// ===== 程序入口 =====
static class Program
{
    [STAThread]
    static void Main()
    {
        PlanBar.LoadSchedule();
        PlanBar.WriteScheduleTxt();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new PlanBar());
    }
}
