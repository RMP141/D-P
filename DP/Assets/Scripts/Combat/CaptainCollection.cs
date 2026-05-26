using System.Collections.Generic;
using ConvoyManager.Data;

namespace ConvoyManager.Combat
{
    public class CaptainCollection : ICaptainCollection
    {
        private readonly List<CaptainDataSO> _captains = new List<CaptainDataSO>();
        private CaptainDataSO _activeCaptain;
        private const int MaxCaptains = 20;

        public IReadOnlyList<CaptainDataSO> Captains => _captains;
        public CaptainDataSO ActiveCaptain => _activeCaptain;

        public bool AddCaptain(CaptainDataSO captain)
        {
            if (_captains.Count >= MaxCaptains || _captains.Contains(captain))
                return false;

            _captains.Add(captain);
            if (_activeCaptain == null)
                _activeCaptain = captain;
            return true;
        }

        public bool SetActiveCaptain(int id)
        {
            var captain = _captains.Find(c => c.ID == id);
            if (captain == null) return false;
            _activeCaptain = captain;
            return true;
        }
    }
}