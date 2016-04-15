using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using MaterialDesignThemes.Wpf;
using Slamby.TAU.Model;
using Slamby.TAU.ViewModel;
using Slamby.TAU.View;

namespace Slamby.TAU.Helper
{
    public static class DialogHandler
    {
        public static object TestResult { get; set; } = true;
        public static JContent TestInput { get; set; } = null;


        public static async Task<object> Show(object content)
        {
            return await Show(content, "RootDialog");
        }

        public static async Task<object> Show(object content, string identifier)
        {
            return await Show(content, identifier, null, null);
        }

        public static async Task<object> Show(object content, string identifier, DialogClosingEventHandler closingHandler)
        {
            return await Show(content, identifier, closingHandler, null);
        }

        public static async Task<object> Show(object content, string identifier, DialogOpenedEventHandler openingHandler)
        {
            return await Show(content, identifier, null, openingHandler);
        }

        public static async Task<object> Show(object content, string identifier, DialogClosingEventHandler closingHandler, DialogOpenedEventHandler openingHandler)
        {
            if (GlobalStore.IsInTestMode)
            {
                if (TestInput != null && content is CommonDialog)
                {
                    ((CommonDialogViewModel)((CommonDialog)content).DataContext).Content = TestInput;
                    TestInput = null;
                }
                return TestResult;
            }
            if (closingHandler == null)
            {
                return openingHandler == null
                    ? await DialogHost.Show(content, identifier)
                    : await DialogHost.Show(content, identifier, openingHandler);
            }
            else
            {
                return openingHandler == null
                    ? await DialogHost.Show(content, identifier, closingHandler)
                    : await DialogHost.Show(content, identifier, openingHandler, closingHandler);
            }
        }

        public static async Task ShowProgress(Action closingHandler, Action openingHandler)
        {
            if (closingHandler == null)
            {
                if (openingHandler == null)
                    throw new ArgumentNullException();
                else
                    await ShowProgress(null, () => Task.Run(openingHandler));
            }
            else
            {
                if (openingHandler == null)
                    await ShowProgress(() => Task.Run(closingHandler), null);
                else
                    await ShowProgress(() => Task.Run(closingHandler), () => Task.Run(openingHandler));
            }

        }

        public static async Task ShowProgress(Func<Task> closingHandler, Func<Task> openingHandler)
        {
            if (GlobalStore.IsInTestMode)
            {
                await Task.Run(async () =>
                {
                    if (openingHandler != null) await openingHandler.Invoke();
                    if (closingHandler != null) await closingHandler.Invoke();
                });
            }
            if (closingHandler == null)
            {
                if (openingHandler == null)
                    await DialogHost.Show(new ProgressDialog(), "RootDialog");
                else
                    await DialogHost.Show(new ProgressDialog(), "RootDialog",
                            async (object s, DialogOpenedEventArgs oa) =>
                            {
                                try
                                {
                                    await Task.Run(async () => await openingHandler.Invoke());
                                }
                                catch (Exception exception)
                                {
                                    Messenger.Default.Send(exception);
                                }
                                finally
                                {
                                    oa.Session.Close();
                                }
                            });
            }
            else
            {
                if (openingHandler == null)
                    await DialogHost.Show(new ProgressDialog(), "RootDialog", async (object s, DialogClosingEventArgs oa) =>
                   {
                       try
                       {
                           await Task.Run(async () => await closingHandler.Invoke());
                       }
                       catch (Exception exception)
                       {
                           Messenger.Default.Send(exception);
                       }
                   });
                else
                    await DialogHost.Show(new ProgressDialog(), "RootDialog", async (object s, DialogOpenedEventArgs oa) =>
                    {
                        try
                        {
                            await Task.Run(async () => await openingHandler.Invoke());
                        }
                        catch (Exception exception)
                        {
                            Messenger.Default.Send(exception);
                        }
                        finally
                        {
                            oa.Session.Close();
                        }
                    }, async (object s, DialogClosingEventArgs oa) =>
                    {
                        try
                        {
                            await Task.Run(async () => await closingHandler.Invoke());
                        }
                        catch (Exception exception)
                        {
                            Messenger.Default.Send(exception);
                        }
                    });
            }
        }
    }
}
