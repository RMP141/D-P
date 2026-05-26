using System;
using UniRx;

namespace ConvoyManager.Core
{
    /// <summary>
    /// Центральная шина событий. Позволяет публиковать сообщения и подписываться на них без прямых зависимостей.
    /// Реализует IDisposable для корректной очистки при выгрузке сцены.
    /// </summary>
    public sealed class EventBus : IDisposable
    {
        private readonly Subject<object> _subject = new Subject<object>();

        /// <summary>
        /// Публикует событие типа T. Все подписчики этого типа получат уведомление.
        /// </summary>
        /// <typeparam name="T">Тип события (может быть классом или структурой).</typeparam>
        /// <param name="message">Экземпляр события.</param>
        public void Publish<T>(T message)
        {
            _subject.OnNext(message);
        }

        /// <summary>
        /// Подписывается на события типа T.
        /// Возвращает <see cref="IObservable{T}"/> – для отписки используйте Dispose() на полученной подписке.
        /// </summary>
        public IObservable<T> Subscribe<T>()
        {
            return Observable.OfType<object, T>(_subject);
        }

        /// <summary>
        /// Освобождает ресурсы шины. Вызывается автоматически при Dispose контейнера.
        /// </summary>
        public void Dispose()
        {
            _subject.Dispose();
        }
    }
}