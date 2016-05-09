using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using MaterialDesignThemes.Wpf;
using Slamby.TAU.Logger;
using Slamby.TAU.Model;
using Slamby.TAU.ViewModel;
using Slamby.TAU.View;

namespace Slamby.TAU.Helper
{
    public class DialogHandler
    {

        public virtual void SetTestResult(object result)
        {
            Messenger.Default.Send<Exception>(new NotSupportedException("This method supported only in test mode!"));
        }
        public virtual void SetTestInput(JContent input)
        {
            Messenger.Default.Send<Exception>(new NotSupportedException("This method supported only in test mode!"));
        }

        public virtual async Task<object> Show(object content)
        {
            return await Show(content, "RootDialog");
        }

        public virtual async Task<object> Show(object content, string identifier)
        {
            return await Show(content, identifier, null, null);
        }

        public virtual async Task<object> Show(object content, string identifier, DialogClosingEventHandler closingHandler)
        {
            return await Show(content, identifier, closingHandler, null);
        }

        public virtual async Task<object> Show(object content, string identifier, DialogOpenedEventHandler openingHandler)
        {
            return await Show(content, identifier, null, openingHandler);
        }

        public virtual async Task<object> Show(object content, string identifier, DialogClosingEventHandler closingHandler, DialogOpenedEventHandler openingHandler)
        {
            if (GlobalStore.DialogIsOpen)
                throw new Exception("Another process is in progress. Please try again!");
            GlobalStore.DialogIsOpen = true;
            object result = null;
            try
            {

                if (closingHandler == null)
                {
                    result = openingHandler == null
                        ? await DialogHost.Show(content, identifier)
                        : await DialogHost.Show(content, identifier, openingHandler);
                }
                else
                {
                    result = openingHandler == null
                        ? await DialogHost.Show(content, identifier, closingHandler)
                        : await DialogHost.Show(content, identifier, openingHandler, closingHandler);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
            finally
            {
                GlobalStore.DialogIsOpen = false;
            }
            return result;
        }

        public virtual async Task ShowProgress(Action closingHandler, Action openingHandler)
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

        public virtual async Task ShowProgress(Func<Task> closingHandler, Func<Task> openingHandler)
        {
            if (GlobalStore.DialogIsOpen)
                throw new Exception("Another process is in progress. Please try again!");
            GlobalStore.DialogIsOpen = true;

            try
            {
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
                        await
                            DialogHost.Show(new ProgressDialog(), "RootDialog",
                                async (object s, DialogClosingEventArgs oa) =>
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
                        await
                            DialogHost.Show(new ProgressDialog(), "RootDialog",
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
            catch (Exception exception)
            {
                Log.Error(exception);
            }

            finally
            {
                GlobalStore.DialogIsOpen = false;
            }
        }
    }
}
