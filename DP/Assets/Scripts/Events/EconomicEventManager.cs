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
    /// ��������� �������������� ���������: ���������� �������������� ��� � ���������� ��������� �������.
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
        /// ��������� ������ ����������� ���������� � ��������� ��������� ������������� �������.
        /// ���������� �� GameFlow ����� ������������� ���������.
        /// </summary>
        public void Start()
        {
            // �������� ���������� � �������� (����� ������� � GameConfig)
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
            // ����������� 30% (����� �������� �� �������� �� �������)
            if (Random.value < 0.3f)
            {
                var eventData = _eventPool[Random.Range(0, _eventPool.Length)];
                _economyEngine.ApplyEconomicEvent(eventData);
            }
        }

        public void Stop()
        {
            _disposables.Clear();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}