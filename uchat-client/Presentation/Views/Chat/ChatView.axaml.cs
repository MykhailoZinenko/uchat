using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using uchat_client.Core.Application.Features.Chat.ViewModels;
using Avalonia.Controls.Primitives;

namespace uchat_client.Presentation.Views.Chat;

public partial class ChatView : UserControl
{
    private const long MaxProfilePictureSize = 5 * 1024 * 1024;
    private const long MaxFileUploadSize = 15 * 1024 * 1024;
    private INotifyCollectionChanged? _messagesCollection;
    private bool _isRestoringScroll;
    private bool _pendingLoadMore;
    private bool _suppressLoadMoreUntilScrollAway;

    public ChatView()
    {
        InitializeComponent();
        DataContextChanged += ChatView_DataContextChanged;
        Loaded += ChatView_Loaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ChatView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (MessagesScrollViewer != null)
        {
            MessagesScrollViewer.ScrollChanged += MessagesScrollViewer_ScrollChanged;
        }
    }

    private void ChatView_DataContextChanged(object? sender, EventArgs e)
    {
        if (_messagesCollection != null)
        {
            _messagesCollection.CollectionChanged -= Messages_CollectionChanged;
            _messagesCollection = null;
        }

        if (DataContext is ChatViewModel vm && vm.Messages is INotifyCollectionChanged collection)
        {
            _messagesCollection = collection;
            _messagesCollection.CollectionChanged += Messages_CollectionChanged;
            ScrollToBottom();
        }
    }

    private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (MessagesScrollViewer == null) return;
        if (sender is not IList list) return;
        if (e.NewItems == null && e.Action == NotifyCollectionChangedAction.Add) return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                // Only auto-scroll when items are appended at the end (new messages),
                // not when older history is inserted at the top.
                var newCount = e.NewItems?.Count ?? 0;
                var appendedIndex = list.Count - newCount;
                if (newCount > 0 && e.NewStartingIndex >= appendedIndex)
                {
                    ScrollToBottom();
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                ScrollToBottom();
                break;
        }
    }

    private void ScrollToBottom()
    {
        if (MessagesScrollViewer == null) return;
        Dispatcher.UIThread.Post(() => MessagesScrollViewer.ScrollToEnd());
    }

    private async void MessagesScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (MessagesScrollViewer == null || DataContext is not ChatViewModel viewModel)
            return;

        // If user scrolls down away from the top, re-enable load-more
        if (MessagesScrollViewer.Offset.Y > 200)
            _suppressLoadMoreUntilScrollAway = false;

        if (_isRestoringScroll)
            return;

        // When near the top, load older messages while preserving viewport position
        var shouldLoadMore = MessagesScrollViewer.Offset.Y <= 100 &&
                             !_suppressLoadMoreUntilScrollAway &&
                             !_pendingLoadMore &&
                             !viewModel.IsLoadingMore;

        if (!shouldLoadMore)
            return;

        // Capture anchor message (first visible) and its offset in viewport
        var anchorMessageId = GetFirstVisibleMessageId(out var anchorOffsetInViewport);
        if (!anchorMessageId.HasValue)
            return;

        _pendingLoadMore = true;
        try
        {
            await viewModel.LoadMoreMessagesAsync();
            // Prevent immediate re-trigger until user scrolls away from top region
            _suppressLoadMoreUntilScrollAway = true;
        }
        finally
        {
            _pendingLoadMore = false;
        }

        // Defer until layout updates so measurements include newly inserted items
        var anchorId = anchorMessageId.Value;
        Dispatcher.UIThread.Post(() =>
        {
            if (MessagesScrollViewer == null) return;
            _isRestoringScroll = true;

            var anchorContainer = FindContainerByMessageId(anchorId);
            if (anchorContainer != null)
            {
                var pt = anchorContainer.TranslatePoint(new Point(0, 0), MessagesScrollViewer);
                if (pt.HasValue)
                {
                    // Adjust offset by how much the anchor moved after prepend
                    var delta = pt.Value.Y - anchorOffsetInViewport;
                    var targetOffsetY = MessagesScrollViewer.Offset.Y + delta;
                    if (targetOffsetY < 0) targetOffsetY = 0;
                    MessagesScrollViewer.Offset = new Vector(MessagesScrollViewer.Offset.X, targetOffsetY);
                }
            }

            _isRestoringScroll = false;
        }, DispatcherPriority.Background);
    }

    private int? GetFirstVisibleMessageId(out double offsetInViewport)
    {
        offsetInViewport = 0;

        if (MessagesItemsControl == null)
        {
            return null;
        }

        for (int i = 0; i < MessagesItemsControl.ItemCount; i++)
        {
            var container = MessagesItemsControl.ContainerFromIndex(i) as Control;
            if (container == null) continue;

            if (MessagesScrollViewer == null) continue;

            var pt = container.TranslatePoint(new Point(0, 0), MessagesScrollViewer);
            if (pt.HasValue && pt.Value.Y + container.Bounds.Height > 0)
            {
                offsetInViewport = pt.Value.Y;
                if (container.DataContext is ChatMessageViewModel vm)
                {
                    return vm.MessageId;
                }
                break;
            }
        }

        return null;
    }

    private Control? FindContainerByMessageId(int messageId)
    {
        if (MessagesItemsControl == null)
        {
            return null;
        }

        for (int i = 0; i < MessagesItemsControl.ItemCount; i++)
        {
            var container = MessagesItemsControl.ContainerFromIndex(i) as Control;
            if (container?.DataContext is ChatMessageViewModel vm && vm.MessageId == messageId)
            {
                if (MessagesScrollViewer == null) continue;
                return container;
            }
        }

        return null;
    }

    private void Message_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        if (!properties.IsRightButtonPressed)
            return;

        if (sender is StackPanel panel && panel.DataContext is ChatMessageViewModel message)
        {
            if (DataContext is ChatViewModel viewModel)
            {
                viewModel.ToggleContextMenu(message);
            }
        }

        e.Handled = true;
    }

    private void MessageTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is ChatViewModel viewModel)
            {
                if (viewModel.SendCommand.CanExecute(null))
                {
                    viewModel.SendCommand.Execute(null);
                }
            }

            e.Handled = true;
        }
    }

    private async void FileUploadButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select File to Send",
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            var file = files[0];
            var filePath = file.Path.LocalPath;

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MaxFileUploadSize)
            {
                return;
            }

            Console.WriteLine($"File selected: {file.Name} ({fileInfo.Length} bytes)");
        }
    }

    private void HeaderMenuButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ChatViewModel vm || sender is not Control target)
            return;

        var menu = new ContextMenu();

        if (vm.CanRename)
        {
            menu.Items.Add(new MenuItem
            {
                Header = "Rename",
                Command = vm.StartRenameCommand
            });
        }

        menu.Items.Add(new MenuItem
        {
            Header = "Leave",
            Command = vm.LeaveRoomCommand
        });

        menu.PlacementTarget = target;
        menu.Placement = PlacementMode.Bottom;
        menu.Open(target);
    }
}
