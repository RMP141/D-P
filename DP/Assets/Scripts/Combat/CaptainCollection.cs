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

        public int[] GetAllCaptainIds()
        {
            var ids = new int[_captains.Count];
            for (int i = 0; i < _captains.Count; i++)
                ids[i] = _captains[i].ID;
            return ids;
        }

        public int GetActiveCaptainId()
        {
            return _activeCaptain != null ? _activeCaptain.ID : -1;
        }

        public void Clear()
        {
            _captains.Clear();
            _activeCaptain = null;
        }
    }
}