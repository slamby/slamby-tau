using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using MaterialDesignThemes.Wpf;
using Slamby.TAU.Model;
using Slamby.TAU.View;
using Slamby.TAU.ViewModel;

namespace Slamby.TAU.Helper
{
    public class TestDialogHandler : DialogHandler
    {
        private object _testResult = true;
        private JContent _testInput = null;

        public override void SetTestResult(object result)
        {
            _testResult = result;
        }
        public override void SetTestInput(JContent input)
        {
            _testInput = input;
        }

        public override async Task<object> Show(object content)
        {
            return await Show(content, "RootDialog");
        }

        public override async Task<object> Show(object content, string identifier)
        {
            return await Show(content, identifier, null, null);
        }

        public override async Task<object> Show(object content, string identifier, DialogClosingEventHandler closingHandler)
        {
            return await Show(content, identifier, closingHandler, null);
        }

        public override async Task<object> Show(object content, string identifier, DialogOpenedEventHandler openingHandler)
        {
            return await Show(content, identifier, null, openingHandler);
        }

        public override async Task<object> Show(object content, string identifier, DialogClosingEventHandler closingHandler, DialogOpenedEventHandler openingHandler)
        {
            if (_testInput != null && content is CommonDialog)
            {
                ((CommonDialogViewModel)((CommonDialog)content).DataContext).Content = _testInput;
                _testInput = null;
            }
            return _testResult;
        }

        public override async Task ShowProgress(Action closingHandler, Action openingHandler)
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

        public override async Task ShowProgress(Func<Task> closingHandler, Func<Task> openingHandler)
        {
            await Task.Run(async () =>
            {
                if (openingHandler != null) await openingHandler.Invoke();
                if (closingHandler != null) await closingHandler.Invoke();
            });
        }
    }
}
