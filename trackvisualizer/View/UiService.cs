using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using trackvisualizer.Service;

namespace trackvisualizer.View
{
    public class UiService: IUiService
    {
        private readonly SemaphoreSlim _foregroundSemaphore = new SemaphoreSlim(1,1);

        public async Task NofityError(string text)
        {
            //Allow only 1 foreground 'blocking' error message at once
            await _foregroundSemaphore.WaitAsync();

            try
            {                
                var errorWindow = new Error(text);

                var taskCompletionSource = new TaskCompletionSource<bool>();

                errorWindow.Closed += (sender, args) => taskCompletionSource.TrySetResult(true);
                errorWindow.Show();

                await taskCompletionSource.Task;
            }
            finally
            {
                _foregroundSemaphore.Release();
            }
        }

        public async Task<string> Ask(string what, string initialValue)
        {
            //Allow only 1 foreground 'blocking' window at once
            await _foregroundSemaphore.WaitAsync();

            try
            {
                var inputbox = new Inputbox(what,initialValue);

                var taskCompletionSource = new TaskCompletionSource<bool>();

                inputbox.Closed += (sender, args) => taskCompletionSource.TrySetResult(true);
                inputbox.Show();

                await taskCompletionSource.Task;

                return inputbox.Result;
            }
            finally
            {
                _foregroundSemaphore.Release();
            }
        }
    }
}
