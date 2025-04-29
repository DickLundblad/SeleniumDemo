using System;
using System.Windows.Forms;
using WixSharp;
using WixSharp.UI.WPF;

namespace WixSharp4
{
    public class Program
    {
        static void Main()
        {
            var project = new ManagedProject("MyProduct",
                              new Dir(@"%ProgramFiles%\My Company\My Product",
                                  new File("Program.cs")));

            project.GUID = new Guid("e5a9c3fd-37f4-4a54-a37a-db197dabc6c0");

            // project.ManagedUI = ManagedUI.DefaultWpf; // all stock UI dialogs

            //custom set of UI WPF dialogs
            project.ManagedUI = new ManagedUI();

            project.ManagedUI.InstallDialogs.Add<WixSharp4.WelcomeDialog>()
                                            .Add<WixSharp4.LicenceDialog>()
                                            .Add<WixSharp4.FeaturesDialog>()
                                            .Add<WixSharp4.InstallDirDialog>()
                                            .Add<WixSharp4.ProgressDialog>()
                                            .Add<WixSharp4.ExitDialog>();

            project.ManagedUI.ModifyDialogs.Add<WixSharp4.MaintenanceTypeDialog>()
                                           .Add<WixSharp4.FeaturesDialog>()
                                           .Add<WixSharp4.ProgressDialog>()
                                           .Add<WixSharp4.ExitDialog>();

            //project.SourceBaseDir = "<input dir path>";
            //project.OutDir = "<output dir path>";

            project.BuildMsi();
        }
    }
}