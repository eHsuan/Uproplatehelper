using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace percentagToAmp
{
    public static class ListBoxAutoScrollBehavior
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached(
                "AutoScroll",
                typeof(bool),
                typeof(ListBoxAutoScrollBehavior),
                new PropertyMetadata(false, OnAutoScrollChanged));

        public static bool GetAutoScroll(DependencyObject d) => (bool)d.GetValue(AutoScrollProperty);
        public static void SetAutoScroll(DependencyObject d, bool value) => d.SetValue(AutoScrollProperty, value);

        private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox lb) return;

            void Attach()
            {
                // 監聽 ItemsSource 變更（被綁定後才有集合）
                var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl));
                dpd.AddValueChanged(lb, (_, __) => HookCollection(lb));

                // 初次（Loaded 後）掛上
                lb.Loaded += (_, __) => {
                    HookCollection(lb);
                    // 也找一次 ScrollViewer
                    _ = GetScrollViewer(lb);
                };
            }

            if (e.NewValue is true) Attach();
        }

        private static void HookCollection(ListBox lb)
        {
            if (lb.ItemsSource is INotifyCollectionChanged incc)
            {
                // 先移除舊的（避免重複訂閱）
                incc.CollectionChanged -= (NotifyCollectionChangedEventHandler)lb.GetValue(_handlerStoreProperty);
                NotifyCollectionChangedEventHandler h = (_, args) =>
                {
                    if (args.Action is NotifyCollectionChangedAction.Add || args.Action is NotifyCollectionChangedAction.Reset)
                        ScrollToEndSafely(lb);
                };
                // 存起來以便下次移除
                lb.SetValue(_handlerStoreProperty, h);
                incc.CollectionChanged += h;
            }
        }

        private static readonly DependencyProperty _handlerStoreProperty =
            DependencyProperty.RegisterAttached("_handler", typeof(NotifyCollectionChangedEventHandler),
                typeof(ListBoxAutoScrollBehavior), new PropertyMetadata(null));

        private static void ScrollToEndSafely(ListBox lb)
        {
            if (lb.Items.Count == 0) return;

            // 用較低優先序，等容器生成/排版後再捲
            lb.Dispatcher.BeginInvoke(new Action(() =>
            {
                // 先試 ScrollViewer.ScrollToEnd（最直接）
                var sv = GetScrollViewer(lb);
                if (sv != null)
                {
                    sv.ScrollToEnd();
                }
                else
                {
                    // 沒抓到 ScrollViewer 就用 ScrollIntoView
                    lb.ScrollIntoView(lb.Items[lb.Items.Count - 1]);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private static ScrollViewer? GetScrollViewer(DependencyObject root)
        {
            if (root is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var found = GetScrollViewer(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
