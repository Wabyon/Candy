using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Candy.Client.Models
{
    /// <summary>
    /// 通知可能モデルの基底クラス。
    /// </summary>
    public abstract class NotificationObject : INotifyPropertyChanged, IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        /// <summary>
        /// <see cref="NotificationObject.Dispose"/> メソッドによって破棄されるオブジェクトの一覧を取得します。
        /// </summary>
        protected ICollection<IDisposable> CompositeDisposable
        {
            get { return _disposable; }
        }

        /// <summary>
        /// プロパティ値が変更されたときに発生します。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// <see cref="PropertyChanged"/> イベントを通知する <see cref="IObservable{T}"/> オブジェクトを生成します。
        /// </summary>
        /// <returns></returns>
        protected IObservable<EventPattern<PropertyChangedEventArgs>> ObservePropertyChanged()
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                h => PropertyChanged += h,
                h => PropertyChanged -= h);
        }
        /// <summary>
        /// 指定されたフィールドに指定された値を設定し、値が変更された場合に <see cref="PropertyChanged"/> イベントを発生させます。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        /// <param name="comparer"></param>
        protected void SetValue<T>(ref T field, T value, [CallerMemberName] string propertyName = null, IEqualityComparer<T> comparer = null)
        {
            var equalityComparer = (comparer ?? EqualityComparer<T>.Default);
            if (equalityComparer.Equals(field, value)) return;
            field = value;
            RaisePropertyChanged(propertyName);
        }
        /// <summary>
        /// <see cref="PropertyChanged"/> イベントを発生させます。
        /// </summary>
        /// <param name="propertyName">変更されたプロパティの名前。</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(e);
        }
        /// <summary>
        /// <see cref="PropertyChanged"/> イベントを発生させます。
        /// </summary>
        /// <param name="e">イベント データを格納している <see cref="PropertyChangedEventArgs"/>。</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// <see cref="NotificationObject"/> で使用されている全てのリソースを破棄します。
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// このオブジェクトで使用されている全てのリソースを破棄します。
        /// </summary>
        /// <param name="disposing">マネージ リソースとアンマネージ リソースの両方を解放する場合は <c>true</c>。アンマネージ リソースだけを解放する場合は <c>false</c>。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposable.IsDisposed) return;

            if (disposing)
            {
                _disposable.Dispose();
            }
        }
        ~NotificationObject()
        {
            Dispose(false);
        }
    }
}