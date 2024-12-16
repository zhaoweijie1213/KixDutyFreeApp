using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    /// <summary>
    /// 全局loading
    /// </summary>
    public class LoadingService : ISingletonDependency
    {
        private bool _isLoading;
        private DateTime _loadingStartTime;
        private readonly int _minDisplayTimeMs = 200;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    NotifyStateChanged();
                }
            }
        }

        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();

        public async Task ShowAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!IsLoading)
                {
                    IsLoading = true;
                    _loadingStartTime = DateTime.UtcNow;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task HideAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (IsLoading)
                {
                    var elapsed = DateTime.UtcNow - _loadingStartTime;
                    if (elapsed.TotalMilliseconds < _minDisplayTimeMs)
                    {
                        var delay = _minDisplayTimeMs - (int)elapsed.TotalMilliseconds;
                        await Task.Delay(delay);
                    }
                    IsLoading = false;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
