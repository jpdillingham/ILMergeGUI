// -----------------------------------------------------------------------
// <copyright file="ClickOnceUpdater.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace ILMergeGui
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Deployment.Application;
    using System.Windows.Forms;
    using System.Diagnostics;

    /// <summary>
    /// See http://msdn.microsoft.com/en-us/library/ms404263.aspx
    /// </summary>
    public class ClickOnceUpdater
    {
        internal static void InstallUpdateSyncWithInfo(String Url)
        {
            String Caption = String.Format("{0} - {1}", Mainform.AppTitle, "Updater");

            UpdateCheckInfo info = null;

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

                try
                {
                    info = ad.CheckForDetailedUpdate();
                }
                catch (DeploymentDownloadException dde)
                {
                    MessageBox.Show("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message, Caption);
                    return;
                }
                catch (InvalidDeploymentException ide)
                {
                    MessageBox.Show("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message, Caption);
                    return;
                }
                catch (InvalidOperationException ioe)
                {
                    MessageBox.Show("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message, Caption);
                    return;
                }

                if (info.UpdateAvailable)
                {
                    Boolean doUpdate = true;

                    if (!info.IsUpdateRequired)
                    {
                        DialogResult dr = MessageBox.Show("An update is available. Would you like to update the application now?", Caption, MessageBoxButtons.OKCancel);
                        if (!(DialogResult.OK == dr))
                        {
                            doUpdate = false;
                        }
                    }
                    else
                    {
                        // Display a message that the app MUST reboot. Display the minimum required version.
                        MessageBox.Show("This application has detected a mandatory update from your current " +
                            "version to version " + info.MinimumRequiredVersion.ToString() +
                            ". The application will now install the update and restart.",
                            Caption, MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }

                    if (doUpdate)
                    {
                        try
                        {
                            ad.Update();
                            MessageBox.Show("The application has been upgraded, and will now restart.", Caption);
                            Application.Restart();
                        }
                        catch (DeploymentDownloadException dde)
                        {
                            MessageBox.Show("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde, Caption);
                            return;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("There is currently no update available.", Caption);
                }
            }
            else
            {
                if (MessageBox.Show(
                     String.Format("This automated check is only available for Click-Once Installers\r\n\r\nVisit the {0} website?", Caption), Caption, MessageBoxButtons.YesNo,
                             MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    VisitWebsite(Url);
                }
            }
        }

        internal static void VisitWebsite(String Url)
        {
            Process.Start(Url);
        }
    }
}
