using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Terminator
{
    [RunInstaller(true)]
    public class TerminatorServiceInstaller : Installer
    {
        public TerminatorServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();
            var installer = new ServiceInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;

            installer.DisplayName = "Process Terminator";
            installer.StartType = ServiceStartMode.Automatic;
            installer.ServiceName = "Process Terminator";

            Installers.Add(processInstaller);
            Installers.Add(installer);
        }
    }
}