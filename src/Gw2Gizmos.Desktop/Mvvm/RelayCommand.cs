using System;
using System.Windows.Input;

namespace Gw2Gizmos.Desktop.Mvvm;

/// <summary>
/// A parameterless <see cref="ICommand"/> backed by a delegate. A command with a <c>canExecute</c> guard re-queries
/// it only when its owner calls <see cref="RaiseCanExecuteChanged"/> after the guarded state changes.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    /// <summary>Tells bound controls to re-evaluate <see cref="CanExecute"/>. Call on the UI thread.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>An <see cref="ICommand"/> that passes a typed parameter to its delegate.</summary>
public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);

    /// <summary>Tells bound controls to re-evaluate <see cref="CanExecute"/>. Call on the UI thread.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
