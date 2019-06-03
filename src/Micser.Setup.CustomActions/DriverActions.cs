using Microsoft.Deployment.WindowsInstaller;
using System.IO;

namespace Micser.Setup.CustomActions
{
    public class DriverActions
    {
        private const string HardwareId = @"Root\Micser.Vac.Driver";

        [CustomAction]
        public static ActionResult Install(Session session)
        {
            SetupApi.Log = msg => session.Log(msg);

            //MessageBox.Show($"Install {infPath} {File.Exists(infPath)}");
            //return ActionResult.Success;
            var installDir = session.CustomActionData["INSTALLDIR"];
            var infPath = Path.Combine(installDir, @"Driver\Micser.Vac.Driver.inf");

            if (!File.Exists(infPath))
            {
                session.Log($"File '{infPath}' does not exist.");
                return ActionResult.Failure;
            }

            var result = SetupApi.InstallInfDriver(infPath, HardwareId);
            return result ? ActionResult.Success : ActionResult.Failure;
        }

        [CustomAction]
        public static ActionResult Uninstall(Session session)
        {
            SetupApi.Log = msg => session.Log(msg);

            //MessageBox.Show($"Uninstall {infPath} {File.Exists(infPath)}");
            //return ActionResult.Success;
            var result = SetupApi.UninstallDevice(HardwareId);
            return result ? ActionResult.Success : ActionResult.Failure;
        }
    }
}