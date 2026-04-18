using System.Reactive.Disposables;
using ReactiveUI;
using Splat;

namespace Nafer.Core.Application.Common;

/// <summary>
/// Pro-grade base class for all ViewModels.
/// </summary>
public abstract class ViewModelBase : ReactiveObject, IDisposable, IActivatableViewModel, IEnableLogger
{
    public ViewModelActivator Activator { get; } = new();

    protected CompositeDisposable Disposables { get; } = new();

    public virtual void Dispose()
    {
        Disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
