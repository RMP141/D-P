using System;
using System.Linq;
using ConvoyManager.Core;
using ConvoyManager.Data;
using ConvoyManager.Economy;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ConvoyManager.Events
{
    /// <summary>
    /// Управляет экономическими событиями: ежедневное восстановление цен и применение случайных событий.
    /// </summary>
    public class EconomicEventManager : IDisposable
    {
        private readonly IEconomyEngine _economyEngine;
        private readonly EventBus _eventBus;
        private readonly GameConfig _config;
        private readonly EconomicEventSO[] _eventPool;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public EconomicEventManager(IEconomyEngine economyEngine, EventBus eventBus, GameConfig config)
        {
            _economyEngine = economyEngine;
            _eventBus = eventBus;
            _config = config;
            _eventPool = Resources.LoadAll<EconomicEventSO>("Events/EconomicEvents");
        }

        /// <summary>
        /// Запускает таймер ежедневного обновления и активации случайных экономических событий.
        /// Вызывается из GameFlow после инициализации экономики.
        /// </summary>
        public void Start()
        {
            // Интервал обновления в секундах (можно вынести в GameConfig)
            float updateInterval = 10f;
            Observable.Interval(TimeSpan.FromSeconds(updateInterval))
                .Subscribe(_ =>
                {
                    _economyEngine.DailyUpdate();
                    TryActivateRandomEvent();
                })
                .AddTo(_disposables);
        }

        private void TryActivateRandomEvent()
        {
            if (_eventPool.Length == 0) return;
            // Вероятность 30% (можно заменить на значение из конфига)
            if (Random.value < 0.3f)
            {
                var eventData = _eventPool[Random.Range(0, _eventPool.Length)];
                _economyEngine.ApplyEconomicEvent(eventData);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}