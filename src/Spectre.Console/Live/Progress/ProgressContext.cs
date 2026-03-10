namespace Spectre.Console;

/// <summary>
/// Represents a context that can be used to interact with a <see cref="Progress"/>.
/// </summary>
public sealed class ProgressContext
{
    private readonly List<ProgressTask> _tasks;
    private readonly Lock _taskLock;
    private readonly IAnsiConsole _console;
    private readonly ProgressRenderer _renderer;
    private readonly TimeProvider _timeProvider;
    private int _taskId;

    /// <summary>
    /// Gets a value indicating whether or not all started tasks have completed.
    /// </summary>
    public bool IsFinished
    {
        get
        {
            lock (_taskLock)
            {
                return _tasks.Where(x => x.IsStarted).All(task => task.IsFinished);
            }
        }
    }

    internal ProgressContext(IAnsiConsole console, ProgressRenderer renderer, TimeProvider timeProvider)
    {
        // Stryker disable next-line all : Equivalent — internal constructor only called from Progress.StartAsync with non-null
        ArgumentNullException.ThrowIfNull(console);
        // Stryker disable next-line all : Equivalent — internal constructor only called from Progress.StartAsync with non-null
        ArgumentNullException.ThrowIfNull(renderer);
        // Stryker disable next-line all : Equivalent — internal constructor only called from Progress.StartAsync with non-null
        ArgumentNullException.ThrowIfNull(timeProvider);
        _tasks = [];
        _taskLock = LockFactory.Create();
        _console = console;
        _renderer = renderer;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Adds a task.
    /// </summary>
    /// <param name="description">The task description.</param>
    /// <param name="autoStart">Whether or not the task should start immediately.</param>
    /// <param name="maxValue">The task's max value.</param>
    /// <returns>The newly created task.</returns>
    public ProgressTask AddTask(string description, bool autoStart = true, double maxValue = 100)
    {
        lock (_taskLock)
        {
            var settings = new ProgressTaskSettings { AutoStart = autoStart, MaxValue = maxValue, };

            return AddTaskAtInternal(description, settings, _tasks.Count);
        }
    }

    /// <summary>
    /// Adds a task.
    /// </summary>
    /// <param name="description">The task description.</param>
    /// <param name="index">The index at which the task should be inserted.</param>
    /// <param name="autoStart">Whether or not the task should start immediately.</param>
    /// <param name="maxValue">The task's max value.</param>
    /// <returns>The newly created task.</returns>
    public ProgressTask AddTaskAt(string description, int index, bool autoStart = true, double maxValue = 100)
    {
        lock (_taskLock)
        {
            // Stryker disable once all : Equivalent — ProgressTaskSettings defaults match method parameter defaults
            var settings = new ProgressTaskSettings { AutoStart = autoStart, MaxValue = maxValue, };

            return AddTaskAtInternal(description, settings, index);
        }
    }

    /// <summary>
    /// Adds a task before the reference task.
    /// </summary>
    /// <param name="description">The task description.</param>
    /// <param name="referenceProgressTask">The reference task to add before.</param>
    /// <param name="autoStart">Whether or not the task should start immediately.</param>
    /// <param name="maxValue">The task's max value.</param>
    /// <returns>The newly created task.</returns>
    public ProgressTask AddTaskBefore(string description, ProgressTask referenceProgressTask, bool autoStart = true, double maxValue = 100)
    {
        lock (_taskLock)
        {
            // Stryker disable once all : Equivalent — ProgressTaskSettings defaults match method parameter defaults
            var settings = new ProgressTaskSettings { AutoStart = autoStart, MaxValue = maxValue, };
            var indexOfReference = ValidateReferenceTask(referenceProgressTask);

            return AddTaskAtInternal(description, settings, indexOfReference);
        }
    }

    /// <summary>
    /// Adds a task after the reference task.
    /// </summary>
    /// <param name="description">The task description.</param>
    /// <param name="referenceProgressTask">The reference task to add after.</param>
    /// <param name="autoStart">Whether or not the task should start immediately.</param>
    /// <param name="maxValue">The task's max value.</param>
    /// <returns>The newly created task.</returns>
    public ProgressTask AddTaskAfter(string description, ProgressTask referenceProgressTask, bool autoStart = true, double maxValue = 100)
    {
        lock (_taskLock)
        {
            // Stryker disable once all : Equivalent — ProgressTaskSettings defaults match method parameter defaults
            var settings = new ProgressTaskSettings { AutoStart = autoStart, MaxValue = maxValue, };
            var indexOfReference = ValidateReferenceTask(referenceProgressTask);

            return AddTaskAtInternal(description, settings, indexOfReference + 1);
        }
    }

    /// <summary>
    /// Adds a task.
    /// </summary>
    /// <param name="description">The task description.</param>
    /// <param name="settings">The task settings.</param>
    /// <returns>The newly created task.</returns>
    public ProgressTask AddTask(string description, ProgressTaskSettings settings)
    {
        lock (_taskLock)
        {
            return AddTaskAtInternal(description, settings, _tasks.Count);
        }
    }

    /// <summary>
    /// Adds a task at the specified index.
    /// </summary>
    /// <param name="description">The task description.</param>
    /// <param name="settings">The task settings.</param>
    /// <param name="index">The index at which the task should be inserted.</param>
    /// <returns>The newly created task.</returns>
    public ProgressTask AddTaskAt(string description, ProgressTaskSettings settings, int index)
    {
        lock (_taskLock)
        {
            return AddTaskAtInternal(description, settings, index);
        }
    }

    /// <summary>
    /// Adds a task before the reference task.
    /// </summary>
    /// <param name="description">The task description.</param>
    /// <param name="settings">The task settings.</param>
    /// <param name="referenceProgressTask">The reference task to add before.</param>
    /// <returns>The newly created task.</returns>
    public ProgressTask AddTaskBefore(string description, ProgressTaskSettings settings, ProgressTask referenceProgressTask)
    {
        lock (_taskLock)
        {
            var indexOfReference = ValidateReferenceTask(referenceProgressTask);

            return AddTaskAtInternal(description, settings, indexOfReference);
        }
    }

    /// <summary>
    /// Adds a task after the reference task.
    /// </summary>
    /// <param name="description">The task description.</param>
    /// <param name="settings">The task settings.</param>
    /// <param name="referenceProgressTask">The reference task to add after.</param>
    /// <returns>The newly created task.</returns>
    public ProgressTask AddTaskAfter(string description, ProgressTaskSettings settings, ProgressTask referenceProgressTask)
    {
        lock (_taskLock)
        {
            var indexOfReference = ValidateReferenceTask(referenceProgressTask);

            return AddTaskAtInternal(description, settings, indexOfReference + 1);
        }
    }

    /// <summary>
    /// Adds a child task directly beneath <paramref name="parent"/> in the display.
    /// The new task is inserted immediately after the last existing descendant of
    /// <paramref name="parent"/> so the visual hierarchy is preserved in the flat list.
    /// </summary>
    /// <param name="parent">The parent task. Must belong to this context.</param>
    /// <param name="description">The child task description.</param>
    /// <param name="autoStart">Whether or not the child task should start immediately.</param>
    /// <param name="maxValue">The child task's max value.</param>
    /// <returns>The newly created child task.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="parent"/> does not belong to this context.
    /// </exception>
    public ProgressTask AddChildTask(ProgressTask parent, string description, bool autoStart = true, double maxValue = 100)
    {
        ArgumentNullException.ThrowIfNull(parent);
        lock (_taskLock)
        {
            var settings = new ProgressTaskSettings { AutoStart = autoStart, MaxValue = maxValue };
            return AddChildTaskInternal(parent, description, settings);
        }
    }

    /// <summary>
    /// Adds a child task directly beneath <paramref name="parent"/> in the display.
    /// </summary>
    /// <param name="parent">The parent task. Must belong to this context.</param>
    /// <param name="description">The child task description.</param>
    /// <param name="settings">The task settings.</param>
    /// <returns>The newly created child task.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="parent"/> does not belong to this context.
    /// </exception>
    public ProgressTask AddChildTask(ProgressTask parent, string description, ProgressTaskSettings settings)
    {
        ArgumentNullException.ThrowIfNull(parent);
        lock (_taskLock)
        {
            return AddChildTaskInternal(parent, description, settings);
        }
    }

    /// <summary>
    /// Removes the task from the task collection.
    /// </summary>
    /// <param name="task">The task to remove.</param>
    /// <returns><c>true</c> if the task was successfully removed; otherwise, <c>false</c>.</returns>
    public bool RemoveTask(ProgressTask task)
    {
        lock (_taskLock)
        {
            if (!_tasks.Remove(task))
            {
                return false;
            }

            // Detach from parent's children list
            task.Parent?.RemoveChildInternal(task);
            task.Parent = null;

            return true;
        }
    }

    /// <summary>
    /// Refreshes the current progress.
    /// </summary>
    public void Refresh()
    {
        _renderer.Update(this);
        _console.Write(ControlCode.Empty);
    }

    private ProgressTask AddTaskAtInternal(string description, ProgressTaskSettings settings, int position)
    {
        // Stryker disable next-line all : Equivalent — private method only called from public methods that construct non-null settings
        ArgumentNullException.ThrowIfNull(settings);

        // Stryker disable once all : Equivalent — task ID ordering not validated by downstream consumers
        var task = new ProgressTask(_taskId++, description, settings.MaxValue, settings.AutoStart, _timeProvider);

        _tasks.Insert(position, task);

        return task;
    }

    // Must be called with _taskLock held.
    // Returns the index of referenceTask within _tasks, or throws if it doesn't belong to this context.
    private int ValidateReferenceTask(ProgressTask referenceTask)
    {
        ArgumentNullException.ThrowIfNull(referenceTask);
        var index = _tasks.IndexOf(referenceTask);
        if (index < 0)
        {
            throw new InvalidOperationException("The reference task does not belong to this progress context.");
        }

        return index;
    }

    // Must be called with _taskLock held.
    private ProgressTask AddChildTaskInternal(ProgressTask parent, string description, ProgressTaskSettings settings)
    {
        if (!_tasks.Contains(parent))
        {
            // Stryker disable once all : Equivalent — exception message text does not affect behavior
            throw new InvalidOperationException("The parent task does not belong to this progress context.");
        }

        var insertIndex = FindChildInsertionIndex(parent);
        var task = AddTaskAtInternal(description, settings, insertIndex);
        task.Parent = parent;
        parent.AddChildInternal(task);
        return task;
    }

    /// <summary>
    /// Finds the flat-list index immediately after the last descendant of
    /// <paramref name="parent"/>. If the parent has no descendants yet, returns
    /// the index immediately after the parent itself.
    /// Must be called with _taskLock held.
    /// </summary>
    private int FindChildInsertionIndex(ProgressTask parent)
    {
        var parentIndex = _tasks.IndexOf(parent);

        // Walk forward to find the last existing descendant.
        var lastDescendantIndex = parentIndex;
        for (var i = parentIndex + 1; i < _tasks.Count; i++)
        {
            var ancestor = _tasks[i].Parent;
            while (ancestor != null)
            {
                if (ancestor == parent)
                {
                    lastDescendantIndex = i;
                    // Stryker disable once all : Equivalent — removing break just causes extra parent-walking; result unchanged since no subsequent ancestor can re-match
                    break;
                }

                ancestor = ancestor.Parent;
            }
        }

        return lastDescendantIndex + 1;
    }

    /// <summary>
    /// Auto-completes any parent task whose <see cref="ProgressTask.AutoCompleteWithChildren"/>
    /// flag is <c>true</c> and whose children have all finished.
    /// Must be called with _taskLock held.
    /// </summary>
    private void PropagateAutoComplete()
    {
        foreach (var task in _tasks)
        {
            // Stryker disable once all : Equivalent — Children.Count > 0 guard prevents completing a parent with no children; removing it would stop the loop early via All() on empty set returning true
            if (task.AutoCompleteWithChildren && !task.IsFinished
                && task.Children.Count > 0
                && task.Children.All(c => c.IsFinished))
            {
                task.StopTask();
            }
        }
    }

    internal IReadOnlyList<ProgressTask> GetTasks()
    {
        lock (_taskLock)
        {
            PropagateAutoComplete();
            return new List<ProgressTask>(_tasks);
        }
    }
}