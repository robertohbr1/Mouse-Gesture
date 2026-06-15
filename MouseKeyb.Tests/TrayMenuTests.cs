using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Xunit;
using MouseKeyb;

namespace MouseKeyb.Tests;

/// <summary>
/// Integration tests for the system tray menu and settings window lifecycle.
/// </summary>
public class TrayMenuTests
{
    /// <summary>
    /// Pumps the WPF dispatcher queue to process pending asynchronous operations.
    /// </summary>
    private static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new DispatcherOperationCallback(ExitFrame), frame);
        Dispatcher.PushFrame(frame);
    }

    private static object? ExitFrame(object state)
    {
        ((DispatcherFrame)state).Continue = false;
        return null;
    }

    /// <summary>
    /// Verifies that:
    /// 1. With "Minimizar ao fechar" enabled, closing the window hides it (remains non-null but invisible), 
    ///    and clicking "Configurações" makes it visible again.
    /// 2. With "Minimizar ao fechar" disabled, closing the window actually closes it (reference becomes null), 
    ///    and clicking "Configurações" recreates and shows the window.
    /// Runs on an STA thread as required by WPF/WinForms.
    /// </summary>
    [Fact]
    public void ClickConfiguracoesMenu_ShouldHandleBothMinimizeAndCloseScenarios()
    {
        Exception? exception = null;

        var thread = new Thread(() =>
        {
            try
            {
                // Instantiate the App (requires STA)
                var app = new App();
                
                // Explicitly set ShutdownMode to OnExplicitShutdown to mimic real application startup
                // and prevent automatic application shutdown when MainWindow is closed in the test.
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // Create the context menu
                var menu = app.CreateContextMenu();

                // Find the "Configurações" menu item
                System.Windows.Forms.ToolStripItem? configItem = null;
                foreach (System.Windows.Forms.ToolStripItem item in menu.Items)
                {
                    if (item.Text == "Configurações")
                    {
                        configItem = item;
                        break;
                    }
                }

                Assert.NotNull(configItem);

                // --- TEST SCENARIO 1: Minimize to tray is ENABLED (Default) ---
                Assert.Null(app.MainWindow);

                // Click "Configurações" -> Should instantiate and show MainWindow
                configItem.PerformClick();
                DoEvents();

                Assert.NotNull(app.MainWindow);
                Assert.True(app.MainWindow.IsVisible);

                // Set minimizing to tray to true
                var mainWindowInstance = (MainWindow)app.MainWindow;
                mainWindowInstance.TrayMinimizeCheckBox.IsChecked = true;

                // Close the window -> Should only hide it
                mainWindowInstance.Close();
                DoEvents();

                // Verify it is hidden but reference is kept
                Assert.NotNull(app.MainWindow);
                Assert.False(app.MainWindow.IsVisible);

                // Click "Configurações" again -> Should make the existing hidden window visible again
                configItem.PerformClick();
                DoEvents();

                Assert.NotNull(app.MainWindow);
                Assert.True(app.MainWindow.IsVisible);
                Assert.Same(mainWindowInstance, app.MainWindow);

                // --- TEST SCENARIO 2: Minimize to tray is DISABLED ---
                // Disable minimizing to tray
                mainWindowInstance.TrayMinimizeCheckBox.IsChecked = false;

                // Close the window -> Should actually close it
                mainWindowInstance.Close();
                DoEvents();

                // Verify reference is null
                Assert.Null(app.MainWindow);

                // Click "Configurações" again -> Should recreate a new MainWindow and show it
                configItem.PerformClick();
                DoEvents();

                Assert.NotNull(app.MainWindow);
                Assert.True(app.MainWindow.IsVisible);
                Assert.NotSame(mainWindowInstance, app.MainWindow);

                // Cleanup
                app.MainWindow.Close();
                app.Shutdown();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw exception;
        }
    }
}
